
using UnityEngine;

public class ShooterInventory : MonoBehaviour
{
    [SerializeField]
    private ItemDatabase database;

    public InventoryCore Core { get; private set; }

    public ItemDatabase Database => database;

    private void Awake()
    {
        Core = new InventoryCore(database);
    }

    public bool TryAdd(int itemId)
    {
        return Core != null && Core.TryAdd(itemId);
    }

    public int GetStacks(int itemId)
    {
        return Core?.GetStacks(itemId) ?? 0;
    }

    // 测试：自动获得无人机核心
    [ContextMenu("Test / Add Drone")]
    private void TestAddDrone()
    {
        Debug.Log($"添加 Drone: {TryAdd(2001)}");
    }
    [ContextMenu("Test / Add sworde")]
    private void TestAddSword()
    {
        Debug.Log($"添加 Drone: {TryAdd(2002)}");
    }


    [ContextMenu("Test / Add Lightning")]
    private void TestAddLightning()
    {
        Debug.Log($"添加 Lightning: {TryAdd(2006)}");
    }

    [ContextMenu("Test / Add Bullet")]
    private void TestAddBullet()
    {
        Debug.Log($"添加 Bullet: {TryAdd(2005)}");
    }

    [ContextMenu("Test / Add Kill Explosion")]
    private void TestAddKillExplosion()
    {
        Debug.Log($"添加爆裂核心：{TryAdd(2007)}");
    }
    [ContextMenu("Test / Add Hit Explosion")]
    private void TestAddHitExplosion()
    {
        Debug.Log($"添加命中爆炸：{TryAdd(2008)}");
    }
}
