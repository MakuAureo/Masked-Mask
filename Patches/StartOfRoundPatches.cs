using HarmonyLib;

namespace MaskedMask.Patches;

[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRoundPatches
{
    [HarmonyPatch(nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    private static void PostStart(StartOfRound __instance)
    {
        Network.MaskedMaskNetwork.SpawnNetworkHandler();
        MaskedPlayerEnemyHelper.PopulateMaskedPlayerEnemyHelperInfo();
    }
}
