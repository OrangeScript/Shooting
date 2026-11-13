
using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [Header("对象池配置")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int initialSize = 30;

    // 存放空闲子弹
    private readonly Queue<Bullet> availableBullets = new();

    private void Awake()
    {
        // 预先创建子弹
        for (int i = 0; i < initialSize; i++)
        {
            Bullet bullet = CreateBullet();
            availableBullets.Enqueue(bullet);
        }
    }

    private Bullet CreateBullet()
    {
        Bullet bullet = Instantiate(bulletPrefab, transform);

        bullet.gameObject.SetActive(false);

        return bullet;
    }

    // 从对象池取出一颗子弹并发射
    public void Spawn(Vector3 position, Quaternion rotation)
    {
        Bullet bullet;

        if (availableBullets.Count > 0)
        {
            // 复用已有子弹
            bullet = availableBullets.Dequeue();
        }
        else
        {
            // 对象池不足时，创建新子弹
            bullet = CreateBullet();
        }

        bullet.Launch(this, position, rotation);
    }

    // 回收子弹
    public void Release(Bullet bullet)
    {
        if (bullet == null || !bullet.gameObject.activeSelf)
            return;

        bullet.gameObject.SetActive(false);
        availableBullets.Enqueue(bullet);
    }
}
