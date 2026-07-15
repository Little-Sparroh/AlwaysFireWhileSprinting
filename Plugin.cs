using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Pigeon.Movement;
using UnityEngine;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.alwaysfirewhilesprinting";
    public const string PluginName = "AlwaysFireWhileSprinting";
    public const string PluginVersion = "1.0.0";

    internal static new ManualLogSource Logger;
    internal static ConfigEntry<bool> enableCanFireWhileSprinting;
    internal static ConfigEntry<bool> enableCanFireWhileSliding;
    internal static ConfigEntry<bool> enableSprintToFireFix;

    private static readonly FieldInfo gunDataField = AccessTools.Field(typeof(Gun), "gunData");
    private static readonly Dictionary<int, OriginalFireConstraints> originalConstraints = new Dictionary<int, OriginalFireConstraints>();

    private Harmony harmony;
    private FileSystemWatcher configWatcher;
    private static volatile bool pendingConstraintRefresh;

    private struct OriginalFireConstraints
    {
        public FireConstraints.ActionFireMode CanFireWhileSprinting;
        public FireConstraints.ActionFireMode CanFireWhileSliding;
    }

    private void Awake()
    {
        Logger = base.Logger;

        enableCanFireWhileSprinting = Config.Bind(
            "General",
            "Can Fire While Sprinting",
            true,
            "Allows firing weapons while sprinting.");

        enableCanFireWhileSliding = Config.Bind(
            "General",
            "Can Fire While Sliding",
            true,
            "Allows firing weapons while sliding.");

        enableSprintToFireFix = Config.Bind(
            "General",
            "Sprint To Fire Fix",
            true,
            "Enables the Sprint-to-Fire fix that allows immediate firing while sprinting and proper sprint resume behavior.");

        enableCanFireWhileSprinting.SettingChanged += OnFireConstraintSettingChanged;
        enableCanFireWhileSliding.SettingChanged += OnFireConstraintSettingChanged;

        try
        {
            SetupFileWatcher();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error setting up config file watcher: {ex.Message}");
        }

        harmony = new Harmony(PluginGUID);

        try
        {
            MethodInfo setupMethod = AccessTools.Method(typeof(Gun), "Setup", new Type[] { typeof(Player), typeof(PlayerAnimation), typeof(IGear) });
            if (setupMethod == null)
            {
                Logger.LogError("Could not find Gun.Setup method for patching.");
            }
            else
            {
                HarmonyMethod prefix = new HarmonyMethod(typeof(SparrohPlugin), nameof(ModifyWeaponPrefix));
                harmony.Patch(setupMethod, prefix: prefix);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error patching Gun.Setup: {ex.Message}");
        }

        try
        {
            // Always apply patches; each patch checks enableSprintToFireFix at runtime for hot reload.
            harmony.PatchAll(typeof(SprintToFireFixPatches));
            Logger.LogInfo($"SprintToFireFix patches applied (currently {(enableSprintToFireFix.Value ? "enabled" : "disabled")})");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying SprintToFireFix patches: {ex.Message}");
        }

        Logger.LogInfo($"{PluginName} v{PluginVersion} loaded successfully.");
    }

    private void Update()
    {
        if (!pendingConstraintRefresh)
            return;

        pendingConstraintRefresh = false;
        ApplyFireConstraintsToAllGuns();
    }

    private void SetupFileWatcher()
    {
        configWatcher = new FileSystemWatcher(Paths.ConfigPath, $"{PluginGUID}.cfg");
        configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
        configWatcher.Changed += OnConfigFileChanged;
        configWatcher.Created += OnConfigFileChanged;
        configWatcher.Renamed += OnConfigFileChanged;
        configWatcher.EnableRaisingEvents = true;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            Config.Reload();
            pendingConstraintRefresh = true;
            Logger.LogInfo("Config reloaded from disk.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error reloading config: {ex.Message}");
        }
    }

    private static void OnFireConstraintSettingChanged(object sender, EventArgs e)
    {
        pendingConstraintRefresh = true;
    }

    public static void ModifyWeaponPrefix(Gun __instance, IGear prefab)
    {
        if (prefab is not Gun gunPrefab)
            return;

        ApplyFireConstraints(gunPrefab);
    }

    internal static void ApplyFireConstraintsToAllGuns()
    {
        try
        {
            Gun[] guns = UnityEngine.Object.FindObjectsOfType<Gun>();
            for (int i = 0; i < guns.Length; i++)
            {
                Gun gun = guns[i];
                if (gun != null)
                    ApplyFireConstraints(gun);
            }

            Logger.LogInfo($"Re-applied fire constraints to {guns.Length} gun(s).");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error re-applying fire constraints: {ex.Message}");
        }
    }

    internal static void ApplyFireConstraints(Gun gun)
    {
        if (gun == null)
            return;

        try
        {
            // Prefer the live instance gunData field when present; fall back to GunData property (prefabs/setup).
            object gunDataObj = gunDataField?.GetValue(gun);
            if (gunDataObj != null)
            {
                ApplyFireConstraintsToGunDataObject(gun, gunDataObj);
                return;
            }

            ref var gunData = ref gun.GunData;
            ApplyFireConstraintsToGunData(gun, ref gunData);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying fire constraints: {ex.Message}");
        }
    }

    private static void ApplyFireConstraintsToGunData(Gun gun, ref GunData gunData)
    {
        int key = gun.GetInstanceID();
        if (!originalConstraints.TryGetValue(key, out OriginalFireConstraints original))
        {
            original = new OriginalFireConstraints
            {
                CanFireWhileSprinting = gunData.fireConstraints.canFireWhileSprinting,
                CanFireWhileSliding = gunData.fireConstraints.canFireWhileSliding
            };
            originalConstraints[key] = original;
        }

        gunData.fireConstraints.canFireWhileSprinting = enableCanFireWhileSprinting.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSprinting;

        gunData.fireConstraints.canFireWhileSliding = enableCanFireWhileSliding.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSliding;
    }

    private static void ApplyFireConstraintsToGunDataObject(Gun gun, object gunDataObj)
    {
        FieldInfo fireConstraintsField = gunDataObj.GetType().GetField("fireConstraints");
        if (fireConstraintsField == null)
            return;

        object fireConstraints = fireConstraintsField.GetValue(gunDataObj);
        if (fireConstraints == null)
            return;

        Type constraintsType = fireConstraints.GetType();
        FieldInfo sprintField = constraintsType.GetField("canFireWhileSprinting");
        FieldInfo slideField = constraintsType.GetField("canFireWhileSliding");
        if (sprintField == null || slideField == null)
            return;

        int key = gun.GetInstanceID();
        if (!originalConstraints.TryGetValue(key, out OriginalFireConstraints original))
        {
            original = new OriginalFireConstraints
            {
                CanFireWhileSprinting = (FireConstraints.ActionFireMode)sprintField.GetValue(fireConstraints),
                CanFireWhileSliding = (FireConstraints.ActionFireMode)slideField.GetValue(fireConstraints)
            };
            originalConstraints[key] = original;
        }

        // fireConstraints may be a struct; write back after mutation.
        object sprintValue = enableCanFireWhileSprinting.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSprinting;
        object slideValue = enableCanFireWhileSliding.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSliding;

        sprintField.SetValue(fireConstraints, sprintValue);
        slideField.SetValue(fireConstraints, slideValue);
        fireConstraintsField.SetValue(gunDataObj, fireConstraints);

        // gunData may itself be a struct; write the boxed copy back onto the Gun.
        if (gunDataField != null && gunDataField.FieldType.IsValueType)
            gunDataField.SetValue(gun, gunDataObj);
    }


    private void OnDestroy()
    {
        if (enableCanFireWhileSprinting != null)
            enableCanFireWhileSprinting.SettingChanged -= OnFireConstraintSettingChanged;
        if (enableCanFireWhileSliding != null)
            enableCanFireWhileSliding.SettingChanged -= OnFireConstraintSettingChanged;

        if (configWatcher != null)
        {
            configWatcher.EnableRaisingEvents = false;
            configWatcher.Changed -= OnConfigFileChanged;
            configWatcher.Created -= OnConfigFileChanged;
            configWatcher.Renamed -= OnConfigFileChanged;
            configWatcher.Dispose();
            configWatcher = null;
        }

        harmony?.UnpatchSelf();
    }
}
