
using UnityEngine;
using Unity.Netcode;
using Cinemachine; // Cinemachine 2.x

public class PlayerCameraTarget : NetworkBehaviour
{
    [SerializeField]
    private Transform followTarget;

    private CinemachineVirtualCamera vcam;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        // 找到场景中的 Virtual Camera
        vcam = FindObjectOfType<CinemachineVirtualCamera>();

        if (vcam == null)
        {
            Debug.LogError(
                "找不到 CinemachineVirtualCamera！"
            );
            return;
        }

        Transform target = followTarget != null
            ? followTarget
            : transform;

        vcam.Follow = target;

        Debug.Log(
            $"Camera绑定成功，跟随玩家: {OwnerClientId}"
        );
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner || vcam == null)
            return;

        Transform target = followTarget != null
            ? followTarget
            : transform;

        if (vcam.Follow == target)
            vcam.Follow = null;
    }
}
