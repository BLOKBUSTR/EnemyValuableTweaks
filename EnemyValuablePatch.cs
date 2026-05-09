using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

#pragma warning disable CS8618
namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyValuable))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class EnemyValuablePatch
    {
        // Dictionaries used as timers across multiple orb instances
        private static readonly Dictionary<EnemyValuable, float> OrbAdditionalCheckDelay = new();
        private static readonly Dictionary<EnemyValuable, float> OrbGrabTimers = new();
        private static readonly Dictionary<EnemyValuable, float> OrbSafeAreaTimers = new();
        
        private static readonly List<EnemyValuable> TrackedOrbs = new();
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Start))]
        internal static void StartPostfix(EnemyValuable __instance)
        {
            EnemyValuableTweaks.Debug("New EnemyValuable spawned)", __instance);
            
            // Check if HostOnlyMode is enabled, otherwise track orb
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                if (EnemyValuableTweaks.configHostOnlyMode.Value)
                {
                    EnemyValuableTweaks.Debug("HostOnlyMode is enabled, skipping all setup and logic");
                    return;
                }
                TrackedOrbs.Add(__instance);
                EnemyValuableTweaks.Debug($"Tracked orbs: {TrackedOrbs.Count} (added one)");
            }
            
            // Initial indestructibleTimer value
            __instance.indestructibleTimer = EnemyValuableTweaks.configTimerLength.Value;
            
            // EnemyValuableUtil component
            if (SemiFunc.IsMultiplayer())
            {
                var component = __instance.AddComponent<EnemyValuableUtil>();
                component.enemyValuable = __instance;
                component.photonView = __instance.impactDetector.photonView;
                EnemyValuableTweaks.Debug("In Multiplayer, added EnemyValuableUtil component", __instance);
            }
            else
            {
                EnemyValuableTweaks.Debug("In Singleplayer, didn't add EnemyValuableUtil component", __instance);
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Update))]
        [SuppressMessage("ReSharper", "InvertIf")]
        internal static void UpdatePostfix(EnemyValuable __instance)
        {
            if (!TrackedOrbs.Contains(__instance) || EnemyValuableTweaks.configHostOnlyMode.Value) return;
            if (__instance.indestructibleTimer < 0f)
                MakeDestructible(__instance);
            if (!__instance.impactDetector.destroyDisable)
                return;
            
            // Primary timer
            if (!EnemyValuableTweaks.configEnableTimer.Value)
            {
                __instance.indestructibleTimer = int.MaxValue;
            }
            else
            {
                EnemyValuableTweaks.DebugTimer($"Time remaining: {__instance.indestructibleTimer}", __instance);
            }
            
            // Delay additional checks, if configured
            if (EnemyValuableTweaks.configAdditionalChecksDelay.Value > 0f)
            {
                if (!OrbAdditionalCheckDelay.ContainsKey(__instance))
                {
                    OrbAdditionalCheckDelay[__instance] = EnemyValuableTweaks.configAdditionalChecksDelay.Value;
                }
                if (OrbAdditionalCheckDelay[__instance] > 0f)
                {
                    OrbAdditionalCheckDelay[__instance] -= Time.deltaTime;
                    EnemyValuableTweaks.DebugTimer($"Delaying additional checks for: {OrbAdditionalCheckDelay[__instance]}", __instance);
                    return;
                }
            }
            
            // Velocity threshold check
            if (EnemyValuableTweaks.configEnableVelocityCheck.Value)
            {
                var sqrMagnitude = Mathf.Max(
                    __instance.impactDetector.rb.velocity.sqrMagnitude,
                    __instance.impactDetector.previousVelocity.sqrMagnitude
                );
                EnemyValuableTweaks.DebugTimer($"sqrMagnitude: {sqrMagnitude}", __instance);
                var threshold = EnemyValuableTweaks.configVelocityThreshold.Value;
                if (sqrMagnitude < threshold * threshold)
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
                    if (!OrbGrabTimers.ContainsKey(__instance))
                    {
                        OrbGrabTimers[__instance] = EnemyValuableTweaks.configPlayerHoldTime.Value;
                        EnemyValuableTweaks.Debug("Player grabbed", __instance);
                    }
                    
                    TimerLogic(OrbGrabTimers, __instance);
                }
                else if (OrbGrabTimers.Remove(__instance))
                {
                    EnemyValuableTweaks.Debug("Player released", __instance);
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
                    if (!OrbSafeAreaTimers.ContainsKey(__instance))
                    {
                        OrbSafeAreaTimers[__instance] = EnemyValuableTweaks.configSafeAreaTime.Value;
                        EnemyValuableTweaks.Debug("Placed in safe area", __instance);
                    }
                    
                    TimerLogic(OrbSafeAreaTimers, __instance);
                }
                else if (OrbSafeAreaTimers.Remove(__instance))
                {
                    EnemyValuableTweaks.Debug("Removed from safe area", __instance);
                }
            }
        }
        
        private static void TimerLogic(Dictionary<EnemyValuable, float> dictionary, EnemyValuable instance)
        {
            if (!TrackedOrbs.Contains(instance)) return;
            
            EnemyValuableTweaks.DebugTimer($"Dictionary time remaining: {dictionary[instance]}", instance);
            dictionary[instance] -= Time.deltaTime;
            
            if (!(dictionary[instance] <= 0f)) return;
            
            MakeDestructible(instance);
            dictionary.Remove(instance);
        }
        
        private static void MakeDestructible(EnemyValuable instance)
        {
            if (!TrackedOrbs.Contains(instance)) return;
            
            instance.indestructibleTimer = 0f;
            instance.impactDetector.destroyDisable = false;
            TrackedOrbs.Remove(instance);
            OrbAdditionalCheckDelay.Remove(instance);
            EnemyValuableTweaks.Debug("Made EnemyValuable destructible\nTracked orbs: {TrackedOrbs.Count} (removed one)", instance);
            
            if (SemiFunc.IsNotMasterClient())
            {
                EnemyValuableTweaks.Debug("Is client, skipping explosion probability calculations", instance);
                return;
            }
            EnemyValuableTweaks.Debug("Calculating explosion probability", instance);
            
            var explosion = false;
            
            float probability;
            if (SemiFunc.MoonLevel() > 3)
            {
                probability = EnemyValuableTweaks.configSuperMoonExplosionProbability.Value;
            }
            else if (SemiFunc.MoonLevel() > 2)
            {
                probability = EnemyValuableTweaks.configFullMoonExplosionProbability.Value;
            }
            else if (SemiFunc.MoonLevel() > 1)
            {
                probability = EnemyValuableTweaks.configHalfMoonExplosionProbability.Value;
            }
            else if (SemiFunc.MoonLevel() > 0)
            {
                probability = EnemyValuableTweaks.configCrescentMoonExplosionProbability.Value;
            }
            else
            {
                probability = EnemyValuableTweaks.configInitialExplosionProbability.Value;
            }
            
            switch (probability)
            {
                case <= 0f:
                    EnemyValuableTweaks.Debug("Probability is 0, EnemyValuable will never explode", instance);
                    break;
                case >= 1f:
                    explosion = true;
                    EnemyValuableTweaks.Debug("Probability is 1, EnemyValuable will always explode", instance);
                    break;
                default:
                    explosion = Random.value < probability;
                    break;
            }
            
            if (SemiFunc.IsMultiplayer())
            {
                var component = instance.GetComponent<EnemyValuableUtil>();
                if (!component)
                {
                    EnemyValuableTweaks.Logger.LogError($"{instance}: EnemyValuableUtil component not found!");
                    return;
                }
                component.SetExplosion(explosion);
            }
            else
            {
                instance.hasExplosion = explosion;
            }
            
            EnemyValuableTweaks.Debug($"MoonLevel: {SemiFunc.MoonLevel()} | probability: {probability} | hasExplosion: {explosion}", instance);
        }
        
        internal static void ClearTrackedOrbs()
        {
            TrackedOrbs.Clear();
            EnemyValuableTweaks.Logger.LogInfo("Changed level, cleared tracked orbs");
        }
    }
}
