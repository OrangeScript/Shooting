using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]

public class ItemPickUp : NetworkBehaviour
{
    [SerializeField] private ItemDefinition definition;
    [SerializeField] private float maxPickDistance = 2.5f;

    private void Reset()
    {
        var trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.2f;

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
        if (!IsServer || !IsSpawned || definition == null)
            return;

        // 不信任客户端传来的玩家身份
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClients.TryGetValue(
            senderId, out var client))
            return;

        var player = client.PlayerObject;
        if (player == null) return;

        // 服务器验证距离
        float distance = Vector3.Distance(
            player.transform.position,
            transform.position
        );

        if (distance > maxPickDistance)
            return;

        PlayerHealth health =
            player.GetComponent<PlayerHealth>();

        if (health != null && health.IsDead)
            return;

        PlayerInventory inventory =
            player.GetComponent<PlayerInventory>();

        if (inventory == null)
            return;

        // 成功获得道具，才让道具消失
        if (inventory.ServerTryAdd(definition))
        {
            Debug.Log(
                $"Player {senderId} 获得 {definition.itemName}"
            );

            NetworkObject.Despawn(true);
        }
    }
}
