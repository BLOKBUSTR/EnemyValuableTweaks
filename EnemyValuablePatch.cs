using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Photon.Pun;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

#pragma warning disable CS8618
namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyValuable))]
    internal static class EnemyValuablePatch
    {
        // Dictionaries used as timers across multiple orb instances
        private static readonly Dictionary<EnemyValuable, float> OrbAdditionalCheckDelay = new();
        private static readonly Dictionary<EnemyValuable, float> OrbGrabTimers = new();
        private static readonly Dictionary<EnemyValuable, float> OrbSafeAreaTimers = new();
        
        private static readonly List<EnemyValuable> TrackedOrbs = new();
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Start))]
        public static void StartPostfix([SuppressMessage("ReSharper", "InconsistentNaming")] EnemyValuable __instance)
        {
            EnemyValuableTweaks.LogDebugGeneral("New EnemyValuable spawned)", __instance);
            
            if (SemiFunc.IsMasterClientOrSingleplayer())
            {
                if (EnemyValuableTweaks.configHostOnlyMode.Value)
                {
                    EnemyValuableTweaks.LogDebugGeneral("HostOnlyMode is enabled, skipping all setup and logic");
                    return;
                }
                TrackedOrbs.Add(__instance);
                EnemyValuableTweaks.LogDebugGeneral($"Tracked orbs: {TrackedOrbs.Count} (added one)");
            }
            
            __instance.indestructibleTimer = EnemyValuableTweaks.configTimerLength.Value;
            
            if (SemiFunc.IsMultiplayer())
            {
                var component = __instance.AddComponent<EnemyValuableSynchronizer>();
                component.enemyValuable = __instance;
                component.photonView = __instance.impactDetector.photonView;
                EnemyValuableTweaks.LogDebugGeneral("In Multiplayer, added EnemyValuableSynchronizer component", __instance);
            }
            else
            {
                EnemyValuableTweaks.LogDebugGeneral("In Singleplayer, didn't add EnemyValuableSynchronizer component", __instance);
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Update))]
        public static void UpdatePostfix([SuppressMessage("ReSharper", "InconsistentNaming")] EnemyValuable __instance)
        {
            if (!TrackedOrbs.Contains(__instance) || EnemyValuableTweaks.configHostOnlyMode.Value) return;
            
            // Skip all following logic if the orb is already supposed to be destructible
            if (__instance.indestructibleTimer < 0f)
                MakeDestructible(__instance);
            if (!__instance.impactDetector.destroyDisable)
                return;
            
            // Primary timer
            if (!EnemyValuableTweaks.configEnableTimer.Value)
            {
                __instance.indestructibleTimer = 60f;
            }
            else
            {
                EnemyValuableTweaks.LogDebugTimers($"Time remaining: {__instance.indestructibleTimer}", __instance);
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
                    EnemyValuableTweaks.LogDebugTimers($"Delaying additional checks for: {OrbAdditionalCheckDelay[__instance]}", __instance);
                    return;
                }
            }
            
            // Velocity threshold check
            if (EnemyValuableTweaks.configEnableVelocityCheck.Value)
            {
                // Find the max of current AND previous velocity
                Vector3 curVel = __instance.impactDetector.rb.velocity;
                Vector3 prevVel = __instance.impactDetector.previousVelocity;
                var vel = Mathf.Max(
                    Mathf.Abs(curVel.x),
                    Mathf.Abs(curVel.y),
                    Mathf.Abs(curVel.z),
                    Mathf.Abs(prevVel.x),
                    Mathf.Abs(prevVel.y),
                    Mathf.Abs(prevVel.z));
                EnemyValuableTweaks.LogDebugTimers($"curVel: {curVel} | prevVel: {prevVel} | combined: {vel}", __instance);
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
                    if (!OrbGrabTimers.ContainsKey(__instance))
                    {
                        OrbGrabTimers[__instance] = EnemyValuableTweaks.configPlayerHoldTime.Value;
                        EnemyValuableTweaks.LogDebugTimers("Player grabbed", __instance);
                    }
                    
                    TimerDictionaryLogic(OrbGrabTimers, __instance);
                }
                else if (OrbGrabTimers.Remove(__instance))
                {
                    EnemyValuableTweaks.LogDebugTimers("Player released", __instance);
                }
            }
            
            // Safe area check (C.A.R.T. and extraction point); functionally identical to player hold check.
            // ReSharper disable once InvertIf
            if (EnemyValuableTweaks.configEnableSafeAreaCheck.Value)
            {
                if (__instance.impactDetector.inCart)
                {
                    if (EnemyValuableTweaks.configSafeAreaTime.Value == 0f)
                    {
                        MakeDestructible(__instance);
                        return;
                    }
                    if (!(OrbSafeAreaTimers.ContainsKey(__instance)))
                    {
                        OrbSafeAreaTimers[__instance] = EnemyValuableTweaks.configSafeAreaTime.Value;
                        EnemyValuableTweaks.LogDebugTimers("Placed in safe area", __instance);
                    }
                    
                    TimerDictionaryLogic(OrbSafeAreaTimers, __instance);
                }
                else if (OrbSafeAreaTimers.Remove(__instance))
                {
                    EnemyValuableTweaks.LogDebugTimers("Removed from safe area", __instance);
                }
            }
        }
        
        private static void TimerDictionaryLogic(Dictionary<EnemyValuable, float> d, EnemyValuable i)
        {
            if (!TrackedOrbs.Contains(i)) return;
            
            EnemyValuableTweaks.LogDebugTimers($"Dictionary time remaining: {d[i]}", i);
            d[i] -= Time.deltaTime;
            
            if (!(d[i] <= 0f)) return;
            
            MakeDestructible(i);
            d.Remove(i);
        }
        
        private static void MakeDestructible(EnemyValuable i)
        {
            if (!TrackedOrbs.Contains(i)) return;
            
            i.indestructibleTimer = 0f;
            i.impactDetector.destroyDisable = false;
            TrackedOrbs.Remove(i);
            OrbAdditionalCheckDelay.Remove(i);
            EnemyValuableTweaks.LogDebugGeneral("Made EnemyValuable destructible", i);
            EnemyValuableTweaks.LogDebugGeneral($"Tracked orbs: {TrackedOrbs.Count} (removed one)");
            
            if (!SemiFunc.IsMasterClientOrSingleplayer() || !SemiFunc.IsMultiplayer())
            {
                EnemyValuableTweaks.LogDebugGeneral("Is client or singleplayer, skipping explosion probability calculations", i);
                return;
            }
            EnemyValuableTweaks.LogDebugGeneral("Is host, calculating explosion probability and sending to clients", i);
            
            var component = i.GetComponent<EnemyValuableSynchronizer>();
            if (component == null)
            {
                EnemyValuableTweaks.Logger.LogError($"{i}: EnemyValuableSynchronizer component not found!");
                return;
            }
            
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
                    component.SetExplosion(false);
                    EnemyValuableTweaks.LogDebugGeneral("Probability is 0, EnemyValuable will never explode", i);
                    return;
                case >= 1f:
                    component.SetExplosion(true);
                    EnemyValuableTweaks.LogDebugGeneral("Probability is 1, EnemyValuable will always explode", i);
                    return;
            }
            
            var rng = Random.value;
            component.SetExplosion(rng < probability);
            EnemyValuableTweaks.LogDebugGeneral($"MoonLevel: {SemiFunc.MoonLevel()} | rng: {rng} | probability: {probability} | hasExplosion: {i.hasExplosion}", i);
        }
        
        internal static void ClearTrackedOrbs()
        {
            if (TrackedOrbs.Count <= 0) return;
            TrackedOrbs.Clear();
            EnemyValuableTweaks.Logger.LogInfo("Changed level, cleared tracked orbs");
        }
    }
    
    public class EnemyValuableSynchronizer : MonoBehaviourPun
    {
        public EnemyValuable enemyValuable;
        public new PhotonView photonView;
        
        public void SetExplosion(bool state)
        {
            if (SemiFunc.IsNotMasterClient()) return;
            
            
            if (SemiFunc.IsMultiplayer())
            {
                photonView.RPC(nameof(SetExplosionRPC), RpcTarget.All, state);
                EnemyValuableTweaks.LogDebugGeneral($"Called SetExplosionRPC on clients (hasExplosion = {state})", enemyValuable);
            }
            else
            {
                SetExplosionRPC(state);
                EnemyValuableTweaks.LogDebugGeneral($"Set hasExplosion = {state}", enemyValuable);
            }
        }
        
        [PunRPC]
        public void SetExplosionRPC(bool state, PhotonMessageInfo info = default)
        {
            enemyValuable.hasExplosion = state;
            EnemyValuableTweaks.LogDebugGeneral($"Received SetExplosionRPC from host (hasExplosion = {state})", enemyValuable);
        }
    }
}
