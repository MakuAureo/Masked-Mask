using HarmonyLib;

namespace MaskedMask.Patches;

[HarmonyPatch(typeof(RoundManager))]
internal class RoundManagerPatches
{
    [HarmonyPatch(nameof(RoundManager.DespawnPropsAtEndOfRound))]
    [HarmonyPostfix]
    private static void PostDespawnPropsAtEndOfRound(RoundManager __instance)
    {
        MaskedPlayerEnemyHelper.masks.Clear();
    }
}
