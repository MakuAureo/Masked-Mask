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
            MaskedPlayerEnemyHelper.SpawnMaskScrap(__instance, MaskedPlayerEnemyHelper.MaskIndex.COMEDY, MaskedMask.Instance.ConfigOptions.convertedMaskValue.Value);
    }

    [HarmonyPatch(nameof(MaskedPlayerEnemy.Update))]
    [HarmonyPrefix]
    private static void PreUpdate(MaskedPlayerEnemy __instance)
    {
        if (!MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out HauntedMaskItemInfo maskItemInfo))
        {
            MaskedMask.Logger.LogError("Could not find mask to check grab status");
            return;
        }

        if (maskItemInfo.mask.hasBeenHeld && !__instance.isEnemyDead)
        {
            __instance.staminaTimer = 15f;
            __instance.creatureAnimator.SetBool("Running", value: true);
            __instance.running = true;
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

        if (maskItemInfo.mask.hasBeenHeld)
            return;

        __instance.maskTypes[__instance.maskTypeIndex].SetActive(enable);
        maskItemInfo.mask.EnableItemMeshes(!enable);
    }

    [HarmonyPatch(nameof(MaskedPlayerEnemy.LateUpdate))]
    [HarmonyPostfix]
    private static void PostLateUpdate(MaskedPlayerEnemy __instance)
    {
        if (!MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out HauntedMaskItemInfo maskItemInfo))
        {
            MaskedMask.Logger.LogError("Could not find mask to update");
            return;
        }

        HauntedMaskItem maskItem = maskItemInfo.mask;
        if (maskItem.hasBeenHeld)
        {
            if (maskItemInfo.hasBeenHeld)
                return;

            maskItemInfo.hasBeenHeld = true;
            maskItem.originalScale = new Vector3(0.1646f, 0.1646f, 0.1646f);
            maskItem.transform.localScale = maskItem.originalScale;
        }
    }

    [HarmonyPatch(nameof(MaskedPlayerEnemy.TeleportMaskedEnemyAndSync))]
    [HarmonyPostfix]
    private static void PostTeleportMaskedEnemyAndSync(MaskedPlayerEnemy __instance)
    {
        if (!MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out HauntedMaskItemInfo maskItemInfo))
        {
            MaskedMask.Logger.LogError("Could not find mask to change isInFactory property");
            return;
        }

        if (maskItemInfo.hasBeenHeld)
            return;

        maskItemInfo.mask.isInFactory = !__instance.isOutside;
    }

    [HarmonyPatch(nameof(MaskedPlayerEnemy.KillEnemy))]
    [HarmonyPostfix]
    private static void PostKillEnemy(MaskedPlayerEnemy __instance)
    {
        if (!MaskedPlayerEnemyHelper.masks.TryGetValue(__instance, out HauntedMaskItemInfo maskItemInfo))
        {
            MaskedMask.Logger.LogWarning("Could not find mask to drop");
            return;
        }

        HauntedMaskItem maskItem = maskItemInfo.mask;
        maskItem.isHeldByEnemy = false;
        maskItem.grabbableToEnemies = true;
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

internal static class MaskedPlayerEnemyHelper
{
    public enum MaskIndex
    {
        COMEDY = 0,
        TRAGEDY
    };

    public static GameObject comedyPrefab = null!;
    public static GameObject tragedyPrefab = null!;
    public static readonly Dictionary<EnemyAI, HauntedMaskItemInfo> masks = new();

    public static void PopulateMaskedPlayerEnemyHelperInfo()
    {
        comedyPrefab = StartOfRound.Instance.allItemsList.itemsList.Find(item => { return (item.name == "ComedyMask"); }).spawnPrefab;
        tragedyPrefab = StartOfRound.Instance.allItemsList.itemsList.Find(item => { return (item.name == "TragedyMask"); }).spawnPrefab;
    }

    public static void SpawnMaskScrap(MaskedPlayerEnemy maskedInstance, MaskIndex maskType, int value)
    {
        GameObject maskPrefab = null!;
        switch (maskType)
        {
            case MaskIndex.COMEDY:
                maskPrefab = comedyPrefab;
                break;
            case MaskIndex.TRAGEDY:
                maskPrefab = tragedyPrefab;
                break;
        }
        GameObject maskObj = Object.Instantiate(maskPrefab, maskedInstance.transform.position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
        NetworkObject maskNetObj = maskObj.GetComponent<NetworkObject>();
        maskNetObj.Spawn();

        NetworkObject maskedNetObj = maskedInstance.GetComponent<NetworkObject>();
        Network.MaskedMaskNetwork.Instance.GrabMaskEveryoneRpc(maskedNetObj, maskNetObj, value);
    }
}

struct HauntedMaskItemInfo
{
    public HauntedMaskItem mask;
    public bool hasBeenHeld;
}
