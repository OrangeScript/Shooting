using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ItemSpawner : NetworkBehaviour
{
    [SerializeField] private ItemDatabase database;

    [SerializeField] private ItemPickUp pickupPrefab;
    // Start is called before the first frame update

    [Header("Spawn Area")]
    [SerializeField] private BoxCollider spawnArea;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Spawn Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField, Min(0)] private int initialCount = 5;
    [SerializeField, Min(1)] private int maxItems = 15;
    [SerializeField, Min(0.1f)] private float spawnInterval = 5f;
    [SerializeField, Min(1)] private int maxSpawnAttempts = 30;

    [Header("Position Settings")]
    [SerializeField] private float spawnHeightOffset = 0f;
    [SerializeField, Min(0f)] private float minItemSpacing = 2f;
    [SerializeField, Min(0f)] private float minPlayerDistance = 3f;
    [SerializeField, Min(0f)] private float obstacleCheckRadius = 0.4f;

    // 当前场景中由本生成器创建的道具
    private readonly List<NetworkObject> activeItems = new();

    // 当前对局允许生成的道具
    private readonly List<ItemDefinition> itemPool = new();
    private Coroutine spawnCoroutine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        BuildItemPool();

        if (autoStart)
            StartSpawning();
    }

    public override void OnNetworkDespawn()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        activeItems.Clear();
        itemPool.Clear();
    }

    private void BuildItemPool()
    {
        itemPool.Clear();

        if (database == null)
        {
            Debug.LogError("ItemSpawner: 未设置 ItemDatabase");
            return;
        }

        HashSet<int> registeredIds = new();

        foreach (ItemDefinition item in database.GetAllItems())
        {
            if (item == null)
                continue;

            // 避免重复 ID
            if (!registeredIds.Add(item.id))
            {
                Debug.LogWarning(
                    $"ItemSpawner: 重复道具 ID {item.id}"
                );
                continue;
            }

            itemPool.Add(item);
        }

        Debug.Log(
            $"ItemSpawner: 加载了 {itemPool.Count} 种道具"
        );
    }
    
    private ItemDefinition GetRandomItem()
    {
        if (itemPool.Count == 0)
            return null;

        int index = Random.Range(0, itemPool.Count);

        return itemPool[index];
    }

    // -----------------------------
    // Spawn Control
    // -----------------------------

    public void StartSpawning()
    {
        if (!IsServer || !IsSpawned)
            return;

        if (spawnCoroutine != null)
            return;

        if (spawnArea == null || pickupPrefab == null)
        {
            Debug.LogError("ItemSpawner: 生成配置不完整");
            return;
        }

        if (groundMask.value == 0)
        {
            Debug.LogError("ItemSpawner: Ground Mask 未设置");
            return;
        }

        if (itemPool.Count == 0)
            BuildItemPool();

        if (itemPool.Count == 0)
        {
            Debug.LogError("ItemSpawner: 道具池为空");
            return;
        }

        CleanupItems();

        int count = Mathf.Min(
            initialCount,
            maxItems - activeItems.Count
        );

        for (int i = 0; i < count; i++)
            SpawnRandomItem();

        spawnCoroutine = StartCoroutine(SpawnLoop());

        Debug.Log("ItemSpawner: 开始生成道具");
    }

    private IEnumerator SpawnLoop()
    {
        WaitForSeconds wait =
            new WaitForSeconds(spawnInterval);

        while (true)
        {
            yield return wait;

            CleanupItems();

            if (activeItems.Count < maxItems)
                SpawnRandomItem();
        }
    }

    public void StopSpawning(bool clearExisting = true)
    {
        if (!IsServer)
            return;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (clearExisting)
        {
            foreach (NetworkObject item in activeItems)
            {
                if (item != null && item.IsSpawned)
                    item.Despawn(true);
            }

            activeItems.Clear();
        }

        Debug.Log("ItemSpawner: 停止生成道具");
    }

    // -----------------------------
    // Spawn Item
    // -----------------------------

    public bool SpawnRandomItem()
    {
        if (!IsServer || !IsSpawned)
            return false;

        CleanupItems();

        if (activeItems.Count >= maxItems)
            return false;

        if (pickupPrefab == null ||
            spawnArea == null ||
            itemPool.Count == 0)
            return false;

        // 1. 随机选道具
        ItemDefinition definition = GetRandomItem();

        if (definition == null)
            return false;

        // 2. 随机选位置
        if (!TryGetSpawnPosition(out Vector3 position))
        {
            Debug.LogWarning(
                "ItemSpawner: 未找到有效生成位置"
            );
            return false;
        }

        // 3. 实例化统一的 Pickup Prefab
        ItemPickUp pickup = Instantiate(
            pickupPrefab,
            position,
            Quaternion.identity
        );

        NetworkObject networkObject =
            pickup.GetComponent<NetworkObject>();

        if (networkObject == null)
        {
            Destroy(pickup.gameObject);
            return false;
        }

        // 4. 服务器生成网络对象
        networkObject.Spawn(true);

        // 5. 同步道具 ID
        pickup.ServerInitialize(definition.id);

        // 6. 记录对象
        activeItems.Add(networkObject);

        Debug.Log(
            $"Spawn: {definition.itemName} " +
            $"(ID={definition.id}), Position={position}"
        );

        return true;
    }

    // -----------------------------
    // Position Sampling
    // -----------------------------

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;

        Bounds bounds = spawnArea.bounds;
        Vector3 size = spawnArea.size;
        Vector3 center = spawnArea.center;

        for (int attempt = 0;
             attempt < maxSpawnAttempts;
             attempt++)
        {
            // 在 BoxCollider 的局部区域内随机采样
            float x = Random.Range(
                -size.x * 0.5f,
                size.x * 0.5f
            );

            float z = Random.Range(
                -size.z * 0.5f,
                size.z * 0.5f
            );

            Vector3 localPoint =
                center + new Vector3(x, 0f, z);

            Vector3 worldPoint =
                spawnArea.transform.TransformPoint(localPoint);

            // 从区域上方向下检测实际地面
            Vector3 rayOrigin = new Vector3(
                worldPoint.x,
                bounds.max.y + 5f,
                worldPoint.z
            );

            if (!Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                bounds.size.y + 20f,
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            // 防止刷到区域上下方的其他楼层
            if (hit.point.y < bounds.min.y - 0.2f ||
                hit.point.y > bounds.max.y + 0.2f)
            {
                continue;
            }

            Vector3 candidate =
                hit.point + Vector3.up * spawnHeightOffset;

            // 检查墙壁、箱子等障碍物
            if (obstacleMask.value != 0)
            {
                Vector3 bottom =
                    candidate + Vector3.up * 0.25f;

                Vector3 top =
                    candidate + Vector3.up * 1.0f;

                if (Physics.CheckCapsule(
                    bottom,
                    top,
                    obstacleCheckRadius,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
            }

            // 检查与其他道具的距离
            bool valid = true;

            foreach (NetworkObject item in activeItems)
            {
                if (item == null || !item.IsSpawned)
                    continue;

                if (Vector3.Distance(
                    candidate,
                    item.transform.position
                ) < minItemSpacing)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            // 检查与玩家的距离
            foreach (var client in
                     NetworkManager.ConnectedClients.Values)
            {
                NetworkObject player = client.PlayerObject;

                if (player == null || !player.IsSpawned)
                    continue;

                if (Vector3.Distance(
                    candidate,
                    player.transform.position
                ) < minPlayerDistance)
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
                continue;

            position = candidate;
            return true;
        }

        return false;
    }

    // -----------------------------
    // Cleanup
    // -----------------------------

    private void CleanupItems()
    {
        activeItems.RemoveAll(
            item => item == null || !item.IsSpawned
        );
    }

    // -----------------------------
    // Editor Gizmos
    // -----------------------------

    private void OnDrawGizmosSelected()
    {
        if (spawnArea == null)
            return;

        Gizmos.color = Color.green;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = spawnArea.transform.localToWorldMatrix;

        Gizmos.DrawWireCube(
            spawnArea.center,
            spawnArea.size
        );

        Gizmos.matrix = oldMatrix;
    }
}
