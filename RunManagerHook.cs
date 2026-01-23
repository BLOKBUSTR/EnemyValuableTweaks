using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(RunManager))]
    public class RunManagerHook
    {
        [HarmonyPostfix, HarmonyPatch(nameof(RunManager.ChangeLevel))]
        public static void ChangeLevelPostfix([SuppressMessage("ReSharper", "InconsistentNaming")] RunManager __instance)
        {
            EnemyValuablePatch.ClearTrackedOrbs();
        }
    }
}
