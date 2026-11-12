using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]

public class ItemPickUp : NetworkBehaviour
{
    [SerializeField] private ItemDatabase database;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float maxPickDistance = 2.5f;

    private readonly NetworkVariable<int> itemId = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private GameObject currentVisual;
    private bool collected;

    public override void OnNetworkSpawn()
    {
        itemId.OnValueChanged += OnItemChanged;
        RefreshVisual();
    }

    public override void OnNetworkDespawn()
    {
        itemId.OnValueChanged -= OnItemChanged;
    }

    public void ServerInitialize(int id)
    {
        if (!IsServer || !IsSpawned)
            return;

        itemId.Value = id;
    }

    private void OnItemChanged(int oldId, int newId)
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (currentVisual != null)
        {
            Destroy(currentVisual);
            currentVisual = null;
        }

        if (database == null || visualRoot == null)
            return;

        ItemDefinition item = database.GetItem(itemId.Value);

        if (item == null || item.equipPrefab == null)
            return;

        currentVisual = Instantiate(
            item.equipPrefab,
            visualRoot,
            false
        );

        currentVisual.transform.localPosition = Vector3.zero;
    }

    

    private void Reset()
    {
        var trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;

        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned) return;

        PlayerInventory player =
            other.GetComponentInParent<PlayerInventory>();

        // 只允许本地玩家发起拾取请求
        if (player == null ||
            !player.IsSpawned ||
            !player.IsOwner)
            return;

        RequestPickupServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(
        ServerRpcParams rpcParams = default)
    {
        if (!IsServer || !IsSpawned || collected)
            return;

        if (database == null)
            return;

        ItemDefinition item = database.GetItem(itemId.Value);
        if (item == null) return;

        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClients.TryGetValue(
            senderId, out var client))
            return;

        NetworkObject player = client.PlayerObject;
        if (player == null) return;

        if (Vector3.Distance(
            player.transform.position,
            transform.position
        ) > maxPickDistance)
            return;

        PlayerHealth health = player.GetComponent<PlayerHealth>();

        if (health != null && health.IsDead)
            return;

        PlayerInventory inventory =
            player.GetComponent<PlayerInventory>();

        if (inventory == null)
            return;

        if (inventory.ServerTryAdd(item.id))
        {
            collected = true;
            NetworkObject.Despawn(true);
        }
    }
}
