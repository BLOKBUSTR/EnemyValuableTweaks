using HarmonyLib;
using Photon.Pun;
using Random = System.Random;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyValuable))]
    internal static class EnemyValuablePatch
    {
        // Dictionaries used as timers across multiple orb instances
        private static readonly Dictionary<object, float> OrbAdditionalCheckDelay = [];
        private static readonly Dictionary<object, float> OrbGrabTimers = [];
        private static readonly Dictionary<object, float> OrbSafeAreaTimers = [];
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Start))]
        public static void StartPostfix(EnemyValuable __instance)
        {
            EnemyValuableTweaks.Logger.LogInfo($"New EnemyValuable has spawned (InstanceID {__instance.GetInstanceID()} | ViewID {__instance.impactDetector.photonView.ViewID})");
            __instance.indestructibleTimer = EnemyValuableTweaks.configTimerLength.Value;
            
            var component = __instance.AddComponent<EnemyValuableSynchronizer>();
            component.enemyValuable = __instance;
            component.photonView = __instance.impactDetector.photonView;
        }
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyValuable.Update))]
        public static void UpdatePostfix(EnemyValuable __instance)
        {
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
                EnemyValuableTweaks.LogDebugTimers($"Time remaining: {__instance.indestructibleTimer}");
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
                    EnemyValuableTweaks.LogDebugTimers($"Delaying additional checks for: {OrbAdditionalCheckDelay[__instance]} ({__instance.GetInstanceID()})");
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
                EnemyValuableTweaks.LogDebugTimers($"curVel: {curVel} | prevVel: {prevVel} | combined: {vel}");
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
                        EnemyValuableTweaks.LogDebugTimers($"Player grabbed ({__instance.GetInstanceID()})");
                    }
                    
                    HandleTimerDictionaries(OrbGrabTimers, __instance);
                }
                else if (OrbGrabTimers.Remove(__instance))
                {
                    EnemyValuableTweaks.LogDebugTimers($"Player let go ({__instance.GetInstanceID()})");
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
                    if (!(OrbSafeAreaTimers.ContainsKey(__instance)))
                    {
                        OrbSafeAreaTimers[__instance] = EnemyValuableTweaks.configSafeAreaTime.Value;
                        EnemyValuableTweaks.LogDebugTimers($"Placed in safe area ({__instance.GetInstanceID()})");
                    }
                    
                    HandleTimerDictionaries(OrbSafeAreaTimers, __instance);
                }
                else if (OrbSafeAreaTimers.Remove(__instance))
                {
                    EnemyValuableTweaks.LogDebugTimers($"Removed from safe area ({__instance.GetInstanceID()})");
                }
            }
        }
        
        private static void HandleTimerDictionaries(Dictionary<object, float> d, EnemyValuable i)
        {
            EnemyValuableTweaks.LogDebugTimers($"Dictionary time remaining: {d[i]} {i.GetInstanceID()}");
            d[i] -= Time.deltaTime;
            
            if (!(d[i] <= 0f)) return;
            
            MakeDestructible(i);
            d.Remove(i);
        }
        
        private static void MakeDestructible(EnemyValuable i)
        {
            i.indestructibleTimer = 0f;
            i.impactDetector.destroyDisable = false;
            OrbAdditionalCheckDelay.Remove(i);
            EnemyValuableTweaks.Logger.LogInfo($"Made EnemyValuable destructible (InstanceID {i.GetInstanceID()} | ViewID {i.impactDetector.photonView.ViewID})");
            
            if (SemiFunc.IsNotMasterClient())
            {
                EnemyValuableTweaks.LogDebugGeneral("Is client, skipping probability calculations");
                return;
            }
            EnemyValuableTweaks.LogDebugGeneral("Is host or singleplayer, calculating probability and sending to clients if multiplayer");
            
            var component = i.GetComponent<EnemyValuableSynchronizer>();
            if (component == null)
            {
                EnemyValuableTweaks.Logger.LogError("EnemyValuableSynchronizer component not found!");
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
                    EnemyValuableTweaks.LogDebugGeneral("Probability is 0, EnemyValuable will never explode");
                    return;
                case >= 1f:
                    component.SetExplosion(true);
                    EnemyValuableTweaks.LogDebugGeneral("Probability is 1, EnemyValuable will always explode");
                    return;
            }
            
            var rng = new Random().NextDouble();
            component.SetExplosion(rng < probability);
            EnemyValuableTweaks.LogDebugGeneral($"MoonLevel: {SemiFunc.MoonLevel()} | rng: {rng} | probability: {probability} | hasExplosion: {i.hasExplosion}");
        }
    }
}

public class EnemyValuableSynchronizer : MonoBehaviourPun
{
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public EnemyValuable enemyValuable;
    public new PhotonView photonView;
    #pragma warning restore CS8618
    
    public void SetExplosion(bool state)
    {
        if (SemiFunc.IsNotMasterClient()) return;
        if (!SemiFunc.IsMultiplayer())
        {
            SetExplosionRPC(state);
            EnemyValuableTweaks.EnemyValuableTweaks.LogDebugGeneral($"Set hasExplosion = {state}");
        }
        else
        {
            photonView.RPC(nameof(SetExplosionRPC), RpcTarget.All, state);
            EnemyValuableTweaks.EnemyValuableTweaks.LogDebugGeneral($"Called SetExplosionRPC on clients (hasExplosion = {state})");
        }
    }
    
    [PunRPC]
    public void SetExplosionRPC(bool state, PhotonMessageInfo info = default)
    {
        enemyValuable.hasExplosion = state;
        EnemyValuableTweaks.EnemyValuableTweaks.LogDebugGeneral($"Received SetExplosionRPC from host (hasExplosion = {state})");
    }
}
