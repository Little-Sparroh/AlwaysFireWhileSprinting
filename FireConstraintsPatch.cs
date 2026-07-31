using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Pigeon.Movement;
using Object = UnityEngine.Object;

public static class FireConstraintsPatches
{
    private static readonly FieldInfo gunDataField = AccessTools.Field(typeof(Gun), "gunData");
    private static readonly Dictionary<int, OriginalFireConstraints> originalConstraints = new();

    [HarmonyPatch(typeof(Gun), "Setup", typeof(Player), typeof(PlayerAnimation), typeof(IGear))]
    [HarmonyPrefix]
    private static void ModifyWeaponPrefix(Gun __instance, IGear prefab)
    {
        if (prefab is not Gun gunPrefab)
            return;

        ApplyFireConstraints(gunPrefab);
    }

    internal static void ApplyFireConstraintsToAllGuns()
    {
        try
        {
            var guns = Object.FindObjectsOfType<Gun>();
            for (var i = 0; i < guns.Length; i++)
            {
                var gun = guns[i];
                if (gun != null)
                    ApplyFireConstraints(gun);
            }

            SparrohPlugin.Logger.LogInfo($"Re-applied fire constraints to {guns.Length} gun(s).");
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error re-applying fire constraints: {ex.Message}");
        }
    }

    internal static void ApplyFireConstraints(Gun gun)
    {
        if (gun == null)
            return;

        try
        {
            var gunDataObj = gunDataField?.GetValue(gun);
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
            SparrohPlugin.Logger.LogError($"Error applying fire constraints: {ex.Message}");
        }
    }

    private static void ApplyFireConstraintsToGunData(Gun gun, ref GunData gunData)
    {
        var key = gun.GetInstanceID();
        if (!originalConstraints.TryGetValue(key, out var original))
        {
            original = new OriginalFireConstraints
            {
                CanFireWhileSprinting = gunData.fireConstraints.canFireWhileSprinting,
                CanFireWhileSliding = gunData.fireConstraints.canFireWhileSliding
            };
            originalConstraints[key] = original;
        }

        gunData.fireConstraints.canFireWhileSprinting = ConfigManager.EnableCanFireWhileSprinting.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSprinting;

        gunData.fireConstraints.canFireWhileSliding = ConfigManager.EnableCanFireWhileSliding.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSliding;
    }

    private static void ApplyFireConstraintsToGunDataObject(Gun gun, object gunDataObj)
    {
        var fireConstraintsField = gunDataObj.GetType().GetField("fireConstraints");
        if (fireConstraintsField == null)
            return;

        var fireConstraints = fireConstraintsField.GetValue(gunDataObj);
        if (fireConstraints == null)
            return;

        var constraintsType = fireConstraints.GetType();
        var sprintField = constraintsType.GetField("canFireWhileSprinting");
        var slideField = constraintsType.GetField("canFireWhileSliding");
        if (sprintField == null || slideField == null)
            return;

        var key = gun.GetInstanceID();
        if (!originalConstraints.TryGetValue(key, out var original))
        {
            original = new OriginalFireConstraints
            {
                CanFireWhileSprinting = (FireConstraints.ActionFireMode)sprintField.GetValue(fireConstraints),
                CanFireWhileSliding = (FireConstraints.ActionFireMode)slideField.GetValue(fireConstraints)
            };
            originalConstraints[key] = original;
        }

        object sprintValue = ConfigManager.EnableCanFireWhileSprinting.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSprinting;
        object slideValue = ConfigManager.EnableCanFireWhileSliding.Value
            ? FireConstraints.ActionFireMode.CanPerformDuring
            : original.CanFireWhileSliding;

        sprintField.SetValue(fireConstraints, sprintValue);
        slideField.SetValue(fireConstraints, slideValue);
        fireConstraintsField.SetValue(gunDataObj, fireConstraints);

        if (gunDataField != null && gunDataField.FieldType.IsValueType)
            gunDataField.SetValue(gun, gunDataObj);
    }

    private struct OriginalFireConstraints
    {
        public FireConstraints.ActionFireMode CanFireWhileSprinting;
        public FireConstraints.ActionFireMode CanFireWhileSliding;
    }
}