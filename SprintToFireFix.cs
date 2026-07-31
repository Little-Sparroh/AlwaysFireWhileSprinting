using System;
using System.Reflection;
using HarmonyLib;
using Pigeon.Movement;
using UnityEngine;

public class SprintToFireData : MonoBehaviour
{
    public bool SprintingLockedBySprintToFire { get; set; }
    public bool PreviousFireInputHeld { get; set; }
}

public static class SprintToFireFixPatches
{
    private static readonly FieldInfo playerField = AccessTools.Field(typeof(Gun), "player");
    private static readonly FieldInfo gunDataField = AccessTools.Field(typeof(Gun), "gunData");
    private static readonly FieldInfo isFireInputHeldField = AccessTools.Field(typeof(Gun), "isFireInputHeld");
    private static readonly MethodInfo tryFireMethod = AccessTools.Method(typeof(Gun), "TryFire");

    private static readonly PropertyInfo canFireWithoutAmmoProperty =
        AccessTools.Property(typeof(Gun), "CanFireWithoutAmmo");

    private static bool IsEnabled =>
        ConfigManager.EnableSprintToFireFix != null && ConfigManager.EnableSprintToFireFix.Value;

    [HarmonyPatch(typeof(Gun), "CanFireDuringAnimationState")]
    [HarmonyPrefix]
    private static bool CanFireDuringAnimationStatePrefix(Gun __instance, ref bool __result)
    {
        if (!IsEnabled)
            return true;

        try
        {
            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);
            if (isFireInputHeld)
            {
                __result = true;
                return false;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in CanFireDuringAnimationState patch: {ex.Message}");
        }

        return true;
    }

    [HarmonyPatch(typeof(Gun), "MinWalkingWeightToFire")]
    [HarmonyPrefix]
    private static bool MinWalkingWeightToFirePrefix(Gun __instance, ref float __result)
    {
        if (!IsEnabled)
            return true;

        try
        {
            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);
            if (isFireInputHeld)
            {
                __result = 0f;
                return false;
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in MinWalkingWeightToFire patch: {ex.Message}");
        }

        return true;
    }

    [HarmonyPatch(typeof(Gun), "Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(Gun __instance)
    {
        if (!IsEnabled)
            return;

        try
        {
            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);

            var sprintData = __instance.gameObject.GetComponent<SprintToFireData>();
            if (sprintData == null) sprintData = __instance.gameObject.AddComponent<SprintToFireData>();

            if (sprintData.PreviousFireInputHeld && !isFireInputHeld)
                if (sprintData.SprintingLockedBySprintToFire)
                {
                    var player = playerField.GetValue(__instance) as Player;
                    if (player != null)
                    {
                        player.SprintLocks = 0;
                        sprintData.SprintingLockedBySprintToFire = false;

                        if (player.AutoSprint)
                        {
                            var wantsToSprintField = typeof(Player).GetField("wantsToSprint",
                                BindingFlags.NonPublic | BindingFlags.Instance);
                            if (wantsToSprintField != null) wantsToSprintField.SetValue(player, true);
                        }
                    }
                }

            sprintData.PreviousFireInputHeld = isFireInputHeld;

            if (isFireInputHeld)
            {
                var wantsToFireProperty = AccessTools.Property(typeof(Gun), "WantsToFire");
                wantsToFireProperty.SetValue(__instance, true);
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in Update postfix: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(Gun), "HandleFiring")]
    [HarmonyPrefix]
    private static bool HandleFiringPrefix(Gun __instance)
    {
        if (!IsEnabled)
            return true;

        try
        {
            var player = playerField.GetValue(__instance) as Player;
            if (player == null) return true;

            var gunData = gunDataField.GetValue(__instance);
            if (gunData == null) return true;

            var isFireInputHeld = (bool)isFireInputHeldField.GetValue(__instance);

            if (player.IsSprinting && isFireInputHeld)
            {
                var chargeData = gunData.GetType().GetField("chargeData").GetValue(gunData);
                var canChargeFire = (bool)chargeData.GetType().GetProperty("CanFire").GetValue(chargeData);

                var canFireWithoutAmmo = (bool)canFireWithoutAmmoProperty.GetValue(__instance);

                if (canChargeFire && (__instance.RemainingAmmo >= 1.0 || canFireWithoutAmmo))
                {
                    var fireConstraints = gunData.GetType().GetField("fireConstraints").GetValue(gunData);
                    var canFireWhileSprinting = (int)fireConstraints.GetType().GetField("canFireWhileSprinting")
                        .GetValue(fireConstraints);

                    if (canFireWhileSprinting != 1)
                    {
                        player.SprintLocks = 1;

                        var sprintData = __instance.gameObject.GetComponent<SprintToFireData>();
                        if (sprintData != null) sprintData.SprintingLockedBySprintToFire = true;
                    }

                    tryFireMethod.Invoke(__instance, null);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in SprintToFireFix patch: {ex.Message}");
            SparrohPlugin.Logger.LogError($"Stack trace: {ex.StackTrace}");
        }

        return true;
    }
}