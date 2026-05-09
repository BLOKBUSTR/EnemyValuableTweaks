using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace EnemyValuableTweaks
{
    [HarmonyPatch(typeof(RunManager))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal class RunManagerHook
    {
        [HarmonyPostfix, HarmonyPatch(nameof(RunManager.ChangeLevel))]
        internal static void ChangeLevelPostfix(bool _levelFailed)
        {
            // To avoid getting spammed by the Arena, or by Imperium if "Disable Game Over" is enabled
            if (_levelFailed) return;
            
            EnemyValuablePatch.ClearTrackedOrbs();
        }
    }
}
