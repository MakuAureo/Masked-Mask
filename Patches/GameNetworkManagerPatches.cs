using HarmonyLib;

namespace MaskedMask.Patches;

[HarmonyPatch(typeof(GameNetworkManager))]
internal class GameNetworkManagerPatches
{
    [HarmonyPatch(nameof(GameNetworkManager.Start))]
    [HarmonyPostfix]
    private static void PostStart(GameNetworkManager __instance)
    {
        Network.MaskedMaskNetwork.CreateAndRegisterPrefab();
    }

    [HarmonyPatch(nameof(GameNetworkManager.Disconnect))]
    [HarmonyPrefix]
    private static void PreDisconnect(GameNetworkManager __instance)
    {
        Network.MaskedMaskNetwork.DespawnNetworkHandler();
    }
}
