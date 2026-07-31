using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

public static class ConfigManager
{
    private const float DebounceSeconds = 0.25f;

    private static ConfigFile config;
    private static ManualLogSource logger;
    private static FileSystemWatcher configWatcher;
    private static volatile bool pendingConstraintRefresh;
    private static volatile bool reloadPending;
    private static float lastReloadTime;
    public static ConfigEntry<bool> EnableCanFireWhileSprinting { get; private set; }
    public static ConfigEntry<bool> EnableCanFireWhileSliding { get; private set; }
    public static ConfigEntry<bool> EnableSprintToFireFix { get; private set; }

    public static void Initialize(ConfigFile configFile, ManualLogSource log)
    {
        config = configFile;
        logger = log;

        EnableCanFireWhileSprinting = config.Bind(
            "General",
            "Can Fire While Sprinting",
            true,
            "Allows firing weapons while sprinting.");

        EnableCanFireWhileSliding = config.Bind(
            "General",
            "Can Fire While Sliding",
            true,
            "Allows firing weapons while sliding.");

        EnableSprintToFireFix = config.Bind(
            "General",
            "Sprint To Fire Fix",
            false,
            "Enables the Sprint-to-Fire fix that allows immediate firing while sprinting and proper sprint resume behavior.");

        EnableCanFireWhileSprinting.SettingChanged += OnSettingChanged;
        EnableCanFireWhileSliding.SettingChanged += OnSettingChanged;
        EnableSprintToFireFix.SettingChanged += OnSettingChanged;

        try
        {
            SetupFileWatcher();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error setting up config file watcher: {ex.Message}");
        }
    }


    public static void Tick()
    {
        if (!reloadPending)
            return;

        if (Time.unscaledTime - lastReloadTime < DebounceSeconds)
            return;

        reloadPending = false;
        lastReloadTime = Time.unscaledTime;

        try
        {
            config.Reload();
            pendingConstraintRefresh = true;
            logger.LogInfo("Config reloaded from disk.");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error reloading config: {ex.Message}");
        }
    }

    public static bool ConsumePendingRefresh()
    {
        if (!pendingConstraintRefresh)
            return false;

        pendingConstraintRefresh = false;
        return true;
    }

    public static void Dispose()
    {
        if (EnableCanFireWhileSprinting != null)
            EnableCanFireWhileSprinting.SettingChanged -= OnSettingChanged;
        if (EnableCanFireWhileSliding != null)
            EnableCanFireWhileSliding.SettingChanged -= OnSettingChanged;
        if (EnableSprintToFireFix != null)
            EnableSprintToFireFix.SettingChanged -= OnSettingChanged;

        if (configWatcher != null)
        {
            configWatcher.EnableRaisingEvents = false;
            configWatcher.Changed -= OnConfigFileChanged;
            configWatcher.Created -= OnConfigFileChanged;
            configWatcher.Renamed -= OnConfigFileChanged;
            configWatcher.Dispose();
            configWatcher = null;
        }
    }

    private static void SetupFileWatcher()
    {
        configWatcher = new FileSystemWatcher(Paths.ConfigPath, $"{SparrohPlugin.PluginGUID}.cfg");
        configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
        configWatcher.Changed += OnConfigFileChanged;
        configWatcher.Created += OnConfigFileChanged;
        configWatcher.Renamed += OnConfigFileChanged;
        configWatcher.EnableRaisingEvents = true;
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        reloadPending = true;
    }

    private static void OnSettingChanged(object sender, EventArgs e)
    {
        pendingConstraintRefresh = true;
    }
}