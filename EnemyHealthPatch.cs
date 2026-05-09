using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyHealth))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class EnemyHealthPatch
    {
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyHealth.Awake))]
        internal static void AwakePostFix(EnemyHealth __instance)
        {
            if (EnemyValuableTweaks.configMaxSpawnAmount.Value == 0)
            {
                __instance.spawnValuableMax = int.MaxValue;
                EnemyValuableTweaks.Debug("spawnValuableMax is infinite", __instance);
            }
            else
            {
                __instance.spawnValuableMax = EnemyValuableTweaks.configMaxSpawnAmount.Value;
                EnemyValuableTweaks.Debug($"Set spawnValuableMax: {__instance.spawnValuableMax}", __instance);
            }
        }
    }
}
