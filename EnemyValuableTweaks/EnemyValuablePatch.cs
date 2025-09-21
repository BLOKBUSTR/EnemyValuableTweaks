using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyValuable))]
    internal static class EnemyValuablePatch
    {
        // Dictionaries used as timers across multiple instances
        private static Dictionary<object, float> orbGrabTimers = [];
        private static Dictionary<object, float> orbCartTimers = [];
        
        [HarmonyPrefix, HarmonyPatch(nameof(EnemyValuable.Start))]
        public static void StartPrefix(EnemyValuable __instance)
        {
            EnemyValuableTweaks.Logger.LogInfo($"New EnemyValuable has spawned: {__instance.GetInstanceID()}");
            if (EnemyValuableTweaks.configEnableDebugLogs.Value)
            {
                EnemyValuableTweaks.Logger.LogDebug($"IsMasterClientOrSingleplayer: {SemiFunc.IsMasterClientOrSingleplayer()}");
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Start))]
        public static void StartPostfix(EnemyValuable __instance)
        {
            if (SemiFunc.IsMultiplayer() && !SemiFunc.IsMasterClient())
            {
                __instance.indestructibleTimer = 0f;
            }
            else
            {
                __instance.indestructibleTimer = EnemyValuableTweaks.configTimerLength.Value;
            }
        }

        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Update))]
        public static void UpdatePostfix(EnemyValuable __instance)
        {
            if (__instance.indestructibleTimer <= 0f)
            {
                MakeDestructible(__instance);
                __instance.indestructibleTimer = 0f;
            }
            if (!__instance.impactDetector.destroyDisable)
            {
                return;
            }
            
            // Primary timer
            if (!EnemyValuableTweaks.configEnableTimer.Value)
            {
                __instance.indestructibleTimer = EnemyValuableTweaks.configTimerLength.Value;
            }
            else if (__instance.indestructibleTimer > 0f)
            {
                if (EnemyValuableTweaks.configTimerLength.Value - EnemyValuableTweaks.configAdditionalChecksDelay.Value < __instance.indestructibleTimer)
                {
                    if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                    {
                        EnemyValuableTweaks.Logger.LogDebug($"Additional checks are delayed ({EnemyValuableTweaks.configTimerLength.Value - EnemyValuableTweaks.configAdditionalChecksDelay.Value} > {__instance.indestructibleTimer})");
                    }
                    return;
                }
                else
                {
                    if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                    {
                        EnemyValuableTweaks.Logger.LogDebug($"Time remaining: {__instance.indestructibleTimer}");
                    }
                }
            }

            // Velocity threshold check
            if (EnemyValuableTweaks.configEnableVelocityCheck.Value && __instance.indestructibleTimer > 0f)
            {
                if (__instance.impactDetector.indestructibleSpawnTimer > 0f)
                {
                    return;
                }

                // Compare current AND previous velocity
                Vector3 curVel = __instance.impactDetector.rb.velocity;
                Vector3 prevVel = __instance.impactDetector.previousVelocity;
                float vel = Mathf.Max(
                    Mathf.Abs(curVel.x),
                    Mathf.Abs(curVel.y),
                    Mathf.Abs(curVel.z),
                    Mathf.Abs(prevVel.x),
                    Mathf.Abs(prevVel.y),
                    Mathf.Abs(prevVel.z));
                if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                {
                    EnemyValuableTweaks.Logger.LogDebug($"curVel: {curVel} | prevVel: {prevVel} | combined: {vel}");
                }
                if (vel < EnemyValuableTweaks.configVelocityThreshold.Value)
                {
                    MakeDestructible(__instance);
                    return;
                }
            }
            
            // Player hold check
            if (EnemyValuableTweaks.configEnablePlayerHold.Value)
            {
                if (__instance.impactDetector.physGrabObject.playerGrabbing.Count > 0)
                {
                    if (EnemyValuableTweaks.configPlayerHoldTime.Value == 0f)
                    {
                        MakeDestructible(__instance);
                        return;
                    }

                    // Add entry if it doesn't aleady exist
                    if (!orbGrabTimers.ContainsKey(__instance))
                    {
                        orbGrabTimers[__instance] = EnemyValuableTweaks.configPlayerHoldTime.Value;
                        if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                        {
                            EnemyValuableTweaks.Logger.LogDebug($"Player grabbed {__instance.GetInstanceID()}");
                        }
                    }

                    HandleTimerDictionaries(orbGrabTimers, __instance);
                }
                else if (orbGrabTimers.ContainsKey(__instance))
                {
                    // Removes the entry if all players let go
                    orbGrabTimers.Remove(__instance);
                    if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                    {
                        EnemyValuableTweaks.Logger.LogDebug($"Player let go {__instance.GetInstanceID()}");
                    }
                }
            }
            
            // Safe area check (C.A.R.T. and extraction point); functionally identical to previous player hold check.
            if (EnemyValuableTweaks.configEnableSafeAreaCheck.Value)
            {
                if (__instance.impactDetector.inCart)
                {
                    if (EnemyValuableTweaks.configSafeAreaTime.Value == 0f)
                    {
                        MakeDestructible(__instance);
                        return;
                    }

                    // Add entry if it doesn't aleady exist
                    if (!(orbCartTimers.ContainsKey(__instance)))
                    {
                        orbCartTimers[__instance] = EnemyValuableTweaks.configSafeAreaTime.Value;
                        if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                        {
                            EnemyValuableTweaks.Logger.LogDebug($"Placed in safe area {__instance.GetInstanceID()}");
                        }
                    }

                    HandleTimerDictionaries(orbCartTimers, __instance);
                }
                else if (orbCartTimers.ContainsKey(__instance))
                {
                    orbCartTimers.Remove(__instance);
                    if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                    {
                        EnemyValuableTweaks.Logger.LogDebug($"Removed from safe area {__instance.GetInstanceID()}");
                    }
                }
            }
        }

        private static void HandleTimerDictionaries(Dictionary<object, float> d, EnemyValuable i)
        {
            if (d[i] > 0f)
            {
                if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                {
                    EnemyValuableTweaks.Logger.LogDebug($"Dictionary time remaining: {d[i]} {i.GetInstanceID()}");
                }
                d[i] -= Time.deltaTime;
                if (d[i] <= 0f)
                {
                    MakeDestructible(i);
                    d.Remove(i);
                }
            }
        }

        private static void MakeDestructible(EnemyValuable i)
        {
            i.indestructibleTimer = 0f;
            i.impactDetector.destroyDisable = false;
            if (EnemyValuableTweaks.configEnableDebugLogs.Value)
            {
                EnemyValuableTweaks.Logger.LogDebug($"Made destructible {i.GetInstanceID()}");
            }
        }
    }
}

/*
if (EnemyValuableTweaks.configEnableDebugLogs.Value)
{
    EnemyValuableTweaks.Logger.LogDebug($"");
}
*/
