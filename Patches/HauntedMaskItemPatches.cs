using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.Netcode;

namespace MaskedMask.Patches;

[HarmonyPatch(typeof(HauntedMaskItem))]
internal class HauntedMaskItemPatches
{
    [HarmonyPatch(nameof(HauntedMaskItem.LateUpdate))]
    [HarmonyPostfix]
    private static void PostLateUpdate(HauntedMaskItem __instance)
    {
        if (__instance.parentObject != null && __instance.parentObject.parent.name == "HeadMaskComedy")
        {
            __instance.transform.rotation = __instance.parentObject.rotation;
            __instance.transform.position = __instance.parentObject.position;
        }
    }

    [HarmonyPatch(nameof(HauntedMaskItem.CreateMimicServerRpc))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TranspileCreateMimicServerRpc(IEnumerable<CodeInstruction> codes)
    {
        CodeInstruction[] callAttachMaskWithValue = 
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_2),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HauntedMaskItemHelper), nameof(HauntedMaskItemHelper.AttachMaskWithValue)))
        };

        return new CodeMatcher(codes)
            .MatchForward(false, new CodeMatch(OpCodes.Stloc_2))
            .Advance(1)
            .Insert(callAttachMaskWithValue)
            .InstructionEnumeration();
    }
}

internal static class HauntedMaskItemHelper
{
    public static void AttachMaskWithValue(HauntedMaskItem maskInstance, NetworkObjectReference maskedRef)
    {
        if (!maskedRef.TryGet(out NetworkObject maskedPlayerEnemy))
        {
            MaskedMask.Logger.LogError("TryGet maskedPlayerEnemy from NetObjRef failed");
            return;
        }

        MaskedPlayerEnemy maskedInstance = maskedPlayerEnemy.GetComponent<MaskedPlayerEnemy>();
        if (maskedInstance == null)
        {
            MaskedMask.Logger.LogError("Masked in GrabMask function did not have MaskedPlayerEnemy component.");
            return;
        }

        MaskedPlayerEnemyHelper.SpawnMaskScrap(maskedInstance, (MaskedPlayerEnemyHelper.MaskIndex)maskedInstance.maskTypeIndex, maskInstance.scrapValue);
    }
}
