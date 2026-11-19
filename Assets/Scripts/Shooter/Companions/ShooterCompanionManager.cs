using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ShooterPlayerHealth))]
[RequireComponent(typeof(ShooterCombatStats))]
[RequireComponent(typeof(ShooterCombatPipeline))]
public class ShooterCompanionManager : MonoBehaviour
{
    [SerializeField] private BulletPool bulletPool;

    [Header("编队")]
    [SerializeField, Min(1)] private int maxTotal = 12;
    [SerializeField, Min(0.1f)] private float orbitRadius = 2f;
    [SerializeField, Min(0f)] private float hoverHeight = 1.8f;
    [SerializeField] private float orbitSpeed = 35f;
    [SerializeField, Min(1f)] private float leashRadius = 12f;

    private sealed class Group
    {
        public CompanionAgent Prefab;
        public int Count;
        public int Wanted;
        public readonly List<CompanionAgent> Pets = new();
    }

    private readonly Dictionary<object, Group> groups = new();

    private bool destroying;
    private bool lastCanAct;

    public ShooterPlayerHealth Owner { get; private set; }
    public ShooterCombatStats Stats { get; private set; }
    public ShooterCombatPipeline Pipeline { get; private set; }

    public BulletPool Bullets => bulletPool;
    public float LeashRadius => Mathf.Max(1f, leashRadius);

    public bool CanAct =>
        isActiveAndEnabled &&
        Owner != null &&
        Owner.isActiveAndEnabled &&
        !Owner.IsDead;

    private void Awake()
    {
        Owner = GetComponent<ShooterPlayerHealth>();
        Stats = GetComponent<ShooterCombatStats>();
        Pipeline = GetComponent<ShooterCombatPipeline>();
    }

    private void OnEnable()
    {
        lastCanAct = CanAct;
        Rebuild();
    }

    private void Update()
    {
        if (lastCanAct == CanAct)
            return;

        lastCanAct = CanAct;
        Rebuild();
    }

    public void SetSummons(
        object source,
        CompanionAgent prefab,
        int count)
    {
        if (destroying || source == null)
            return;

        if (prefab == null || count <= 0)
        {
            RemoveSummons(source);
            return;
        }

        if (!groups.TryGetValue(source, out Group group))
        {
            group = new Group();
            groups.Add(source, group);
        }

        if (group.Prefab != prefab)
            ClearGroup(group);

        group.Prefab = prefab;
        group.Count = count;

        Rebuild();
    }

    public void RemoveSummons(object source)
    {
        if (destroying || source == null ||
            !groups.TryGetValue(source, out Group group))
            return;

        ClearGroup(group);
        groups.Remove(source);
        Rebuild();
    }

    private void Rebuild()
    {
        if (destroying)
            return;

        int budget = CanAct ? Mathf.Max(1, maxTotal) : 0;
        int total = 0;

        foreach (Group group in groups.Values)
        {
            group.Wanted = Mathf.Min(group.Count, budget);
            budget -= group.Wanted;
            total += group.Wanted;
        }

        int slot = 0;

        foreach (Group group in groups.Values)
        {
            while (group.Pets.Count > group.Wanted)
            {
                int last = group.Pets.Count - 1;
                DestroyPet(group.Pets[last]);
                group.Pets.RemoveAt(last);
            }

            while (group.Pets.Count < group.Wanted)
            {
                var pet = Instantiate(
                    group.Prefab,
                    GetFollowPosition(
                        slot + group.Pets.Count,
                        total
                    ),
                    Quaternion.identity
                );

                pet.Initialize(this);
                group.Pets.Add(pet);
            }

            foreach (CompanionAgent pet in group.Pets)
                pet.SetSlot(slot++, total);
        }
    }

    public Vector3 GetFollowPosition(int slot, int count)
    {
        float angle = (
            Time.time * orbitSpeed +
            360f * slot / Mathf.Max(1, count)
        ) * Mathf.Deg2Rad;

        float radius = Mathf.Min(orbitRadius, LeashRadius);

        return transform.position + new Vector3(
            Mathf.Cos(angle) * radius,
            hoverHeight,
            Mathf.Sin(angle) * radius
        );
    }

    public Vector3 ClampPosition(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;
        offset = Vector3.ClampMagnitude(offset, LeashRadius);

        return transform.position + new Vector3(
            offset.x,
            hoverHeight,
            offset.z
        );
    }

    private void DestroyPet(CompanionAgent pet)
    {
        if (pet == null)
            return;

        pet.gameObject.SetActive(false);
        Destroy(pet.gameObject);
    }

    private void ClearGroup(Group group)
    {
        foreach (CompanionAgent pet in group.Pets)
            DestroyPet(pet);

        group.Pets.Clear();
    }

    private void OnDisable()
    {
        foreach (Group group in groups.Values)
            ClearGroup(group);
    }

    private void OnDestroy()
    {
        destroying = true;

        foreach (Group group in groups.Values)
            ClearGroup(group);

        groups.Clear();
    }
}