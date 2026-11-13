using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoShooter : MonoBehaviour
{
     [Header("References")]
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private Transform firePoint;

    [Header("Settings")]
    [SerializeField] private float fireInterval = 0.2f;
    [SerializeField] private ParticleSystem muzzleFlash;


    private float nextFireTime;

    private void Update()
    {
        if (Time.time < nextFireTime)
            return;

        nextFireTime = Time.time + Mathf.Max(0.01f, fireInterval);

        Fire();
    }

    private void Fire()
    {
        if (bulletPool == null || firePoint == null)
            return;

        bulletPool.Spawn(
            firePoint.position,
            firePoint.rotation
        );
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            muzzleFlash.Play(true);
        }
    }
}
