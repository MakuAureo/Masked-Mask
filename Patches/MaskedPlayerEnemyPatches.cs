using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace MaskedMask.Patches;

[HarmonyPatch(typeof(MaskedPlayerEnemy))]
internal class MaskedPlayerEnemyPatches
{
    [HarmonyPatch(nameof(MaskedPlayerEnemy.Start))]
    [HarmonyPostfix]
    private static void PostStart(MaskedPlayerEnemy __instance)
    {
        if (NetworkManager.Singleton.IsServer && !MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out _))
        {
            GameObject maskPrefab = (__instance.maskTypeIndex == MaskedPlayerEnemyHelper.comedyMaskIndex) ? (MaskedPlayerEnemyHelper.comedyPrefab) : (MaskedPlayerEnemyHelper.tragedyPrefab);
            GameObject maskObj = Object.Instantiate(maskPrefab, __instance.transform.position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
            NetworkObject maskNetObj = maskObj.GetComponent<NetworkObject>();
            maskNetObj.Spawn();

            NetworkObject maskedNetObj = __instance.GetComponent<NetworkObject>();
            Network.MaskedMaskNetwork.Instance.GrabMaskEveryoneRpc(maskedNetObj, maskNetObj, MaskedMask.Instance.ConfigOptions.convertedMaskValue.Value);
        }
    }
    
    [HarmonyPatch(nameof(MaskedPlayerEnemy.SetMaskGlow))]
    [HarmonyPostfix]
    private static void PostSetMaskGlow(MaskedPlayerEnemy __instance, bool enable)
    {
        if (!MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out HauntedMaskItemInfo maskItemInfo))
        {
            MaskedMask.Logger.LogError("Could not find mask to glow");
            return;
        }
        HauntedMaskItem maskItem = maskItemInfo.mask;

        __instance.maskTypes[__instance.maskTypeIndex].SetActive(enable);
        maskItem.EnableItemMeshes(!enable);
    }

    [HarmonyPatch(nameof(MaskedPlayerEnemy.LateUpdate))]
    [HarmonyPostfix]
    private static void PostLateUpdate(MaskedPlayerEnemy __instance)
    {
        if (MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out HauntedMaskItemInfo maskItemInfo))
        {
            HauntedMaskItem maskItem = maskItemInfo.mask;
            if (maskItem.hasBeenHeld)
            {
                if (!maskItemInfo.hasBeenHeld)
                {
                    maskItemInfo.hasBeenHeld = true;
                    maskItem.originalScale = new Vector3(0.1646f, 0.1646f, 0.1646f);
                    maskItem.transform.localScale = maskItem.originalScale;
                }

                return;
            }

            maskItem.transform.rotation = __instance.maskTypes[0].transform.GetChild(2).transform.rotation;
            maskItem.transform.position = __instance.maskTypes[0].transform.GetChild(2).transform.position;
        }
        else
            MaskedMask.Logger.LogWarning("Could not find mask to update");

    }

    [HarmonyPatch(nameof(MaskedPlayerEnemy.KillEnemy))]
    [HarmonyPostfix]
    private static void PostKillEnemy(MaskedPlayerEnemy __instance)
    {
        if (MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out HauntedMaskItemInfo maskItemInfo))
        {
            HauntedMaskItem maskItem = maskItemInfo.mask;
            maskItem.isHeldByEnemy = false;
            maskItem.grabbableToEnemies = true;
            maskItem.grabbable = true;
        }
        else
            MaskedMask.Logger.LogWarning("Could not find mask to drop");
    }

    [HarmonyPatch(nameof(MaskedPlayerEnemy.killAnimation), MethodType.Enumerator)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TranspilekillAnimation(IEnumerable<CodeInstruction> codes)
    {
        return new CodeMatcher(codes)
            .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Vector3), nameof(Vector3.zero))))
            .Advance(1)
            .SetOpcodeAndAdvance(OpCodes.Ldc_I4_1)
            .InstructionEnumeration();
    }
}

struct HauntedMaskItemInfo
{
    public HauntedMaskItem mask;
    public bool hasBeenHeld;
}

internal static class MaskedPlayerEnemyHelper
{
    public const int comedyMaskIndex = 0;
    public const int tragedyMaskIndex = 1;

    public static GameObject comedyPrefab = null!;
    public static GameObject tragedyPrefab = null!;
    public static readonly Dictionary<EnemyAI, HauntedMaskItemInfo> masks = new();

    public static void PopulateMaskedPlayerEnemyHelperInfo()
    {
        comedyPrefab = StartOfRound.Instance.allItemsList.itemsList.Find(item => { return (item.name == "ComedyMask"); }).spawnPrefab;
        tragedyPrefab = StartOfRound.Instance.allItemsList.itemsList.Find(item => { return (item.name == "TragedyMask"); }).spawnPrefab;
    }
}
