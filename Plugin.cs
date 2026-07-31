using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.alwaysfirewhilesprinting";
    public const string PluginName = "AlwaysFireWhileSprinting";
    public const string PluginVersion = "1.0.1";

    internal new static ManualLogSource Logger;

    private Harmony harmony;

    private void Awake()
    {
        Logger = base.Logger;

        ConfigManager.Initialize(Config, Logger);

        harmony = new Harmony(PluginGUID);

        try
        {
            harmony.PatchAll(typeof(FireConstraintsPatches));
            harmony.PatchAll(typeof(SprintToFireFixPatches));
            Logger.LogInfo(
                $"Harmony patches applied (SprintToFireFix currently {(ConfigManager.EnableSprintToFireFix.Value ? "enabled" : "disabled")}).");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying patches: {ex.Message}");
        }

        Logger.LogInfo($"{PluginName} v{PluginVersion} loaded successfully.");
    }

    private void Update()
    {
        ConfigManager.Tick();

        if (ConfigManager.ConsumePendingRefresh())
            FireConstraintsPatches.ApplyFireConstraintsToAllGuns();
    }

    private void OnDestroy()
    {
        ConfigManager.Dispose();
        harmony?.UnpatchSelf();
    }
}