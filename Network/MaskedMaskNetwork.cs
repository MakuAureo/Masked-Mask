using Unity.Netcode;
using UnityEngine;

namespace MaskedMask.Network;

internal class MaskedMaskNetwork : NetworkBehaviour
{
    private static GameObject prefab = null!;
    public static MaskedMaskNetwork Instance { get; private set; } = null!;

    public static void CreateAndRegisterPrefab()
    {
        if (prefab != null)
            return;

        prefab = new GameObject(MyPluginInfo.PLUGIN_GUID + " Prefab");
        prefab.hideFlags |= HideFlags.HideAndDontSave;
        NetworkObject networkObject = prefab.AddComponent<NetworkObject>();
        networkObject.GlobalObjectIdHash = prefab.name.Hash32();
        prefab.AddComponent<MaskedMaskNetwork>();
        NetworkManager.Singleton.AddNetworkPrefab(prefab);

        MaskedMask.Logger.LogInfo("Networ prefab created and registered");
    }

    public static void SpawnNetworkHandler()
    {
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Object.Instantiate(prefab).GetComponent<NetworkObject>().Spawn();
            MaskedMask.Logger.LogInfo("Network handler spawned");
        }
    }

    public static void DespawnNetworkHandler()
    {
        if (Instance != null && Instance.NetworkObject.IsSpawned && (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost))
        {
            Instance.NetworkObject.Despawn();
            MaskedMask.Logger.LogInfo("Network handler despawned");
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    [Rpc(SendTo.Everyone)]
    public void GrabMaskEveryoneRpc(NetworkObjectReference maskedPlayerEnemyNetObjRef, NetworkObjectReference maskItemNetObjRef, int maskValue)
    {
        if (!maskedPlayerEnemyNetObjRef.TryGet(out NetworkObject maskedPlayerEnemy))
        {
            MaskedMask.Logger.LogError("TryGet maskedPlayerEnemy from NetObjRef failed");
            return;
        }
        if (!maskItemNetObjRef.TryGet(out NetworkObject maskItem))
        {
            MaskedMask.Logger.LogError("TryGet maskItem from NetObjRef failed");
            return;
        }

        HauntedMaskItem mask = maskItem.GetComponent<HauntedMaskItem>();
        if (mask == null)
        {
            MaskedMask.Logger.LogError("Mask in GrabMask function did not have HauntedMaskItem component.");
            return;
        }
        MaskedPlayerEnemy masked = maskedPlayerEnemy.GetComponent<MaskedPlayerEnemy>();
        if (masked == null)
        {
            MaskedMask.Logger.LogError("Masked in GrabMask function did not have MaskedPlayerEnemy component.");
            return;
        }

        if (Patches.MaskedPlayerEnemyHelper.masks.TryGetValue(masked, out _))
        {
            MaskedMask.Logger.LogWarning($"Duplicate Masked entry... skipping {maskValue}");
            return;
        }

        masked.maskTypes[0].SetActive(value: false);
        masked.maskTypes[1].SetActive(value: false);

        mask.transform.localScale = new Vector3(0.13f, 0.13f, 0.13f);
        mask.parentObject = masked.maskTypes[0].transform.GetChild(2).transform;
        mask.SetScrapValue(maskValue);
        mask.isHeldByEnemy = true;
        mask.grabbable = true;
        mask.grabbableToEnemies = false;

        Patches.HauntedMaskItemInfo maskInfo = new()
        {
            mask = mask,
            hasBeenHeld = false
        };
        Patches.MaskedPlayerEnemyHelper.masks[masked] = maskInfo;
    }
}
