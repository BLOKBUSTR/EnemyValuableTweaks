using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyParent))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class EnemyParentPatch
    {
        [HarmonyPrefix, HarmonyPatch(nameof(EnemyParent.Despawn))]
        internal static void DespawnPrefix(EnemyParent __instance, out bool __state)
        {
            if (SemiFunc.IsNotMasterClient())
            {
                __state = false;
                return;
            }
            if (!__instance.Enemy.HasHealth || __instance.Enemy.Health.healthCurrent > 0
                || !__instance.Enemy.Health.spawnValuable
                || __instance.Enemy.Health.spawnValuableCurrent >= __instance.Enemy.Health.spawnValuableMax)
            {
                EnemyValuableTweaks.Debug("Valuable should not spawn, skipping probability logic", __instance);
                __state = false;
                return;
            }
            
            EnemyValuableTweaks.Debug("Rolling valuable spawn probability...", __instance);
            
            var probability = __instance.difficulty switch
            {
                EnemyParent.Difficulty.Difficulty1 => EnemyValuableTweaks.configSmallSpawnProbability.Value,
                EnemyParent.Difficulty.Difficulty2 => EnemyValuableTweaks.configMediumSpawnProbability.Value,
                EnemyParent.Difficulty.Difficulty3 => EnemyValuableTweaks.configLargeSpawnProbability.Value,
                _ => throw new ArgumentOutOfRangeException()
            };
            switch (probability)
            {
                case >= 1f:
                    __state = false;
                    EnemyValuableTweaks.Debug("Probability is 1, spawned valuable", __instance);
                    return;
                case <= 0f:
                    __instance.Enemy.Health.spawnValuable = false;
                    __state = true;
                    EnemyValuableTweaks.Debug("Probability is 0, didn't spawn valuable", __instance);
                    return;
            }
            
            var value = Random.value;
            if (value < probability)
            {
                EnemyValuableTweaks.Debug($"Rolled probability {value} < {probability}: True; spawned valuable", __instance);
                __state = false;
            }
            else
            {
                __instance.Enemy.Health.spawnValuable = false;
                __state = true;
                EnemyValuableTweaks.Debug($"Rolled probability {value} < {probability}: False; didn't spawn valuable", __instance);
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyParent.Despawn))]
        internal static void DespawnPostfix(EnemyParent __instance, bool __state)
        {
            if (SemiFunc.IsNotMasterClient()) return;
            
            EnemyValuableTweaks.Debug($"DespawnPostfix __state: {__state}");
            if (!__state) return;
            __instance.Enemy.Health.spawnValuable = true;
        }
    }
}
