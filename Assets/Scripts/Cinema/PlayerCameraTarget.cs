
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Cinemachine; // Cinemachine 2.x

public class PlayerCameraTarget : NetworkBehaviour
{
    [SerializeField]
    private Transform followTarget;

    private CinemachineVirtualCamera vcam;
    private Transform planarFollowTarget;
    private Transform previousFollow;
    private CinemachineTransposer transposer;
    private CinemachineTransposer.BindingMode previousBindingMode;
    private Vector3 previousFollowOffset;
    private Vector3 previousDamping;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;

        // Host 的玩家可能先在菜单场景生成，游戏场景中的相机尚未加载。
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBindCamera();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (!TryBindCamera())
        {
            Debug.LogError(
                $"场景 {scene.name} 加载完成后仍找不到 CinemachineVirtualCamera，请确认相机对象已启用。", this
            );
        }
    }

    private bool TryBindCamera()
    {
        CinemachineVirtualCamera sceneCamera = FindObjectOfType<CinemachineVirtualCamera>();
        if (sceneCamera == null)
            return false;

        if (vcam == sceneCamera && planarFollowTarget != null)
            return true;

        UnbindCamera();
        vcam = sceneCamera;

        Transform target = followTarget != null
            ? followTarget
            : transform;

        Vector3 sceneCameraPosition = vcam.transform.position;
        Quaternion sceneCameraRotation = vcam.transform.rotation;
        previousFollow = vcam.Follow;

        // 独立的运行时跟随点只取玩家 XZ，不继承跳跃高度、转向或模型缩放。
        planarFollowTarget = new GameObject("Local Camera Follow Target").transform;
        SceneManager.MoveGameObjectToScene(planarFollowTarget.gameObject, vcam.gameObject.scene);
        planarFollowTarget.position = new Vector3(target.position.x, 0f, target.position.z);

        transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
        if (transposer != null)
        {
            previousBindingMode = transposer.m_BindingMode;
            previousFollowOffset = transposer.m_FollowOffset;
            previousDamping = new Vector3(transposer.m_XDamping, transposer.m_YDamping, transposer.m_ZDamping);

            Vector3 forward = sceneCameraRotation * Vector3.forward;
            Vector3 offset = sceneCameraPosition - planarFollowTarget.position;
            // 沿场景原来的视线对准地面，保留原高度和俯角。
            if (forward.y < -0.001f && offset.y > 0f)
                offset = -forward * (offset.y / -forward.y);

            transposer.m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
            transposer.m_FollowOffset = offset;
            // 斜向偏移的阻尼轴并非世界坐标，三轴相同才能保持水平跟随平面。
            transposer.m_YDamping = transposer.m_XDamping;
            transposer.m_ZDamping = transposer.m_XDamping;
            sceneCameraPosition = planarFollowTarget.position + offset;
        }

        vcam.Follow = planarFollowTarget;
        vcam.ForceCameraPosition(sceneCameraPosition, sceneCameraRotation);

        Debug.Log(
            $"Camera绑定成功，跟随玩家: {OwnerClientId}"
        );
        return true;
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || planarFollowTarget == null)
            return;

        Transform target = followTarget != null ? followTarget : transform;
        planarFollowTarget.position = new Vector3(target.position.x, 0f, target.position.z);
    }

    public override void OnNetworkDespawn()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindCamera();
    }

    public override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindCamera();
        base.OnDestroy();
    }

    private void UnbindCamera()
    {
        if (vcam != null && planarFollowTarget != null && vcam.Follow == planarFollowTarget)
        {
            vcam.Follow = previousFollow;
            if (transposer != null)
            {
                transposer.m_BindingMode = previousBindingMode;
                transposer.m_FollowOffset = previousFollowOffset;
                transposer.m_XDamping = previousDamping.x;
                transposer.m_YDamping = previousDamping.y;
                transposer.m_ZDamping = previousDamping.z;
            }
        }
        if (planarFollowTarget != null)
            Destroy(planarFollowTarget.gameObject);

        planarFollowTarget = null;
        previousFollow = null;
        transposer = null;
        vcam = null;
    }
}
