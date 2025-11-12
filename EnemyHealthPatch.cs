using HarmonyLib;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyHealth))]
    internal static class EnemyHealthPatch
    {
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyHealth.Awake))]
        public static void AwakePostFix(EnemyHealth __instance)
        {
            __instance.spawnValuableMax = EnemyValuableTweaks.configMaxSpawnAmount.Value;
            EnemyValuableTweaks.LogDebugGeneral($"Set spawnValuableMax: {__instance.spawnValuableMax}");
        }
    }
}
