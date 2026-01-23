using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyParent))]
    internal static class EnemyParentPatch
    {
        private static bool savedSpawnValuable;
        
        [HarmonyPrefix, HarmonyPatch(nameof(EnemyParent.Despawn))]
        public static void DespawnPrefix([SuppressMessage("ReSharper", "InconsistentNaming")] EnemyParent __instance)
        {
            if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
            if (!__instance.Enemy.HasHealth || __instance.Enemy.Health.healthCurrent > 0
                || !__instance.Enemy.Health.spawnValuable
                || __instance.Enemy.Health.spawnValuableCurrent >= __instance.Enemy.Health.spawnValuableMax)
            {
                EnemyValuableTweaks.LogDebugGeneral("Valuable should not spawn, skipping probability logic", __instance);
                return;
            }
            
            EnemyValuableTweaks.LogDebugGeneral("Rolling valuable spawn probability...", __instance);
            
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
                    EnemyValuableTweaks.LogDebugGeneral("Probability is 1, spawned valuable", __instance);
                    return;
                case <= 0f:
                    __instance.Enemy.Health.spawnValuable = false;
                    savedSpawnValuable = true;
                    EnemyValuableTweaks.LogDebugGeneral("Probability is 0, didn't spawn valuable", __instance);
                    return;
            }
            
            var value = Random.value;
            if (value < probability)
            {
                EnemyValuableTweaks.LogDebugGeneral($"Rolled probability {value} < {probability}: True; spawned valuable", __instance);
            }
            else
            {
                __instance.Enemy.Health.spawnValuable = false;
                savedSpawnValuable = true;
                EnemyValuableTweaks.LogDebugGeneral($"Rolled probability {value} < {probability}: False; didn't spawn valuable", __instance);
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyParent.Despawn))]
        public static void DespawnPostfix([SuppressMessage("ReSharper", "InconsistentNaming")] EnemyParent __instance)
        {
            if (!savedSpawnValuable || !SemiFunc.IsMasterClientOrSingleplayer()) return;
            __instance.Enemy.Health.spawnValuable = true;
            savedSpawnValuable = false;
        }
    }
}
