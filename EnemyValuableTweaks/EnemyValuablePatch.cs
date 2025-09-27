using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyValuable))]
    internal static class EnemyValuablePatch
    {
        // Dictionaries used as timers across multiple instances
        private static Dictionary<object, float> _orbAdditionalCheckDelay = [];
        private static Dictionary<object, float> _orbGrabTimers = [];
        private static Dictionary<object, float> _orbCartTimers = [];
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Start))]
        public static void StartPostfix(EnemyValuable __instance)
        {
            EnemyValuableTweaks.Logger.LogInfo($"New EnemyValuable has spawned ({__instance.GetInstanceID()})");
            __instance.indestructibleTimer = EnemyValuableTweaks.configTimerLength.Value;
        }
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Update))]
        public static void UpdatePostfix(EnemyValuable __instance)
        {
            // Skip all following logic if the orb is already supposed to be destructible
            if (__instance.indestructibleTimer < 0f)
            {
                MakeDestructible(__instance);
            }
            if (!__instance.impactDetector.destroyDisable)
            {
                return;
            }
            
            // Primary timer
            if (!EnemyValuableTweaks.configEnableTimer.Value)
            {
                __instance.indestructibleTimer = 60f; // The edge case of absurdly long lag spikes scares me
            }
            else if (__instance.indestructibleTimer > 0f && EnemyValuableTweaks.configEnableDebugLogs.Value)
            {
                EnemyValuableTweaks.Logger.LogDebug($"Time remaining: {__instance.indestructibleTimer}");
            }
            
            // Delay additional checks, if configured
            if (EnemyValuableTweaks.configAdditionalChecksDelay.Value > 0f)
            {
                if (!_orbAdditionalCheckDelay.ContainsKey(__instance))
                {
                    _orbAdditionalCheckDelay[__instance] = EnemyValuableTweaks.configAdditionalChecksDelay.Value;
                }
                if (_orbAdditionalCheckDelay[__instance] > 0f)
                {
                    _orbAdditionalCheckDelay[__instance] -= Time.deltaTime;
                    if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                    {
                        EnemyValuableTweaks.Logger.LogDebug($"Delaying additional checks for: {_orbAdditionalCheckDelay[__instance]} ({__instance.GetInstanceID()})");
                    }
                    return;
                }
            }
            
            // Velocity threshold check
            if (EnemyValuableTweaks.configEnableVelocityCheck.Value)
            {
                // Find the max of current AND previous velocity
                var curVel = __instance.impactDetector.rb.velocity;
                var prevVel = __instance.impactDetector.previousVelocity;
                var vel = Mathf.Max(
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
                    if (!_orbGrabTimers.ContainsKey(__instance))
                    {
                        _orbGrabTimers[__instance] = EnemyValuableTweaks.configPlayerHoldTime.Value;
                        if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                        {
                            EnemyValuableTweaks.Logger.LogDebug($"Player grabbed ({__instance.GetInstanceID()})");
                        }
                    }
                    
                    HandleTimerDictionaries(_orbGrabTimers, __instance);
                }
                else if (_orbGrabTimers.Remove(__instance))
                {
                    if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                    {
                        EnemyValuableTweaks.Logger.LogDebug($"Player let go ({__instance.GetInstanceID()})");
                    }
                }
            }
            
            // Safe area check (C.A.R.T. and extraction point); functionally identical to player hold check.
            if (EnemyValuableTweaks.configEnableSafeAreaCheck.Value)
            {
                if (__instance.impactDetector.inCart)
                {
                    if (EnemyValuableTweaks.configSafeAreaTime.Value == 0f)
                    {
                        MakeDestructible(__instance);
                        return;
                    }
                    if (!(_orbCartTimers.ContainsKey(__instance)))
                    {
                        _orbCartTimers[__instance] = EnemyValuableTweaks.configSafeAreaTime.Value;
                        if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                        {
                            EnemyValuableTweaks.Logger.LogDebug($"Placed in safe area ({__instance.GetInstanceID()})");
                        }
                    }
                    
                    HandleTimerDictionaries(_orbCartTimers, __instance);
                }
                else if (_orbCartTimers.Remove(__instance))
                {
                    if (EnemyValuableTweaks.configEnableDebugLogs.Value)
                    {
                        EnemyValuableTweaks.Logger.LogDebug($"Removed from safe area ({__instance.GetInstanceID()})");
                    }
                }
            }
        }

        private static void HandleTimerDictionaries(Dictionary<object, float> d, EnemyValuable i)
        {
            if (EnemyValuableTweaks.configEnableDebugLogs.Value)
            {
                EnemyValuableTweaks.Logger.LogDebug($"Dictionary time remaining: {d[i]} {i.GetInstanceID()}");
            }
            d[i] -= Time.deltaTime;
            
            if (!(d[i] <= 0f)) return;
            
            MakeDestructible(i);
            d.Remove(i);
        }

        private static void MakeDestructible(EnemyValuable i)
        {
            i.indestructibleTimer = 0f;
            i.impactDetector.destroyDisable = false;
            _orbAdditionalCheckDelay.Remove(i);
            EnemyValuableTweaks.Logger.LogInfo($"Made destructible ({i.GetInstanceID()})");
        }
    }
}

/*
if (EnemyValuableTweaks.configEnableDebugLogs.Value)
{
    EnemyValuableTweaks.Logger.LogDebug($"");
}
*/
