using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSetup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // 测试阶段按客户端 ID 分散出生位置
        float x = (OwnerClientId % 8) * 3f;
        transform.position = new Vector3(x, 1f, 0f);
    }
}