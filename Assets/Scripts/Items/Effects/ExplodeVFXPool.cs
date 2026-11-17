using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public class ExplosionVfxPool : MonoBehaviour
{
    [SerializeField] private ParticleSystem prefab;

    [SerializeField, Min(0)]
    private int initialSize = 8;

    [SerializeField, Min(1)]
    private int maxCached = 32;

    [SerializeField]
    private float heightOffset = 0.2f;

    private ObjectPool<ParticleSystem> pool;

    private readonly List<ParticleSystem> active = new();

    private void Awake()
    {
        if (prefab == null)
        {
            Debug.LogError("没有绑定爆炸特效 Prefab", this);
            enabled = false;
            return;
        }

        int capacity = Mathf.Max(1, maxCached);

        pool = new ObjectPool<ParticleSystem>(
            createFunc: CreateVfx,
            actionOnGet: null,
            actionOnRelease: ReturnVfx,
            actionOnDestroy: DestroyVfx,
            collectionCheck: true,
            defaultCapacity: Mathf.Clamp(initialSize, 1, capacity),
            maxSize: capacity
        );

        // 必须先全部取出，再一起归还，才能预热多个实例。
        int count = Mathf.Clamp(initialSize, 0, capacity);

        for (int i = 0; i < count; i++)
            active.Add(pool.Get());

        foreach (ParticleSystem system in active)
            pool.Release(system);

        active.Clear();
    }

    private ParticleSystem CreateVfx()
    {
        // 不设置玩家为父物体，保持爆炸在世界原位置。
        ParticleSystem system = Instantiate(prefab);
        system.gameObject.SetActive(false);
        return system;
    }

    private void ReturnVfx(ParticleSystem system)
    {
        if (system == null)
            return;

        system.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        system.gameObject.SetActive(false);
    }

    private void DestroyVfx(ParticleSystem system)
    {
        if (system != null)
            Destroy(system.gameObject);
    }

    public void Play(Vector3 position, float radius)
    {
        if (!isActiveAndEnabled || pool == null)
            return;

        ParticleSystem system = pool.Get();

        system.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        system.transform.SetPositionAndRotation(
            position + Vector3.up * heightOffset,
            Quaternion.identity
        );

        // 约定：Prefab 按半径1制作，运行时覆盖缩放。
        system.transform.localScale =
            Vector3.one * Mathf.Max(0.1f, radius);

        system.gameObject.SetActive(true);
        system.Play(true);

        active.Add(system);
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            ParticleSystem system = active[i];

            // 等根粒子及子粒子都结束，再归还。
            if (system != null && system.IsAlive(true))
                continue;

            active.RemoveAt(i);

            if (system != null)
                pool.Release(system);
        }
    }

    private void OnDisable()
    {
        if (pool == null)
            return;

        foreach (ParticleSystem system in active)
        {
            if (system != null)
                pool.Release(system);
        }

        active.Clear();
    }

    private void OnDestroy()
    {
        foreach (ParticleSystem system in active)
            DestroyVfx(system);

        active.Clear();
        pool?.Clear();
    }
}