using HarmonyLib;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(EnemyHealth))]
    internal static class EnemyHealthPatch
    {
        [HarmonyPostfix, HarmonyPatch(nameof(EnemyHealth.Awake))]
        public static void AwakePostFix(EnemyHealth __instance)
        {
            if (EnemyValuableTweaks.configMaxSpawnAmount.Value == 0)
            {
                __instance.spawnValuableMax = int.MaxValue;
                EnemyValuableTweaks.LogDebugGeneral("spawnValuableMax is infinite");
            }
            else
            {
                __instance.spawnValuableMax = EnemyValuableTweaks.configMaxSpawnAmount.Value;
                EnemyValuableTweaks.LogDebugGeneral($"Set spawnValuableMax: {__instance.spawnValuableMax}");
            }
        }
    }
}
