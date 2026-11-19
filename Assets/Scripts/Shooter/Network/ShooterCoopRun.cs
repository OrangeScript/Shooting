using System.Collections.Generic;
using Cinemachine;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class ShooterCoopRun : NetworkBehaviour
{
    [Header("场景引用")]
    [SerializeField] private GameObject soloPlayer;
    [SerializeField] private GameObject[] soloOnlyActors = System.Array.Empty<GameObject>();
    [SerializeField] private GameDirector director;
    [SerializeField] private ShooterRunHUD runHud;
    [SerializeField] private ShooterHealthHUD healthHud;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private EnemySpawner coopSpawner;
    [SerializeField] private Transform spawnCenter;

    [Header("联机预制体")]
    [SerializeField] private ShooterCoopPlayer playerPrefab;
    [SerializeField] private ShooterCoopEnemy enemyPrefab;

    [Header("道路")]
    [SerializeField] private Transform[] roadWidthObjects;
    [SerializeField] private Transform[] sideBoundaries;
    [SerializeField] private CinemachineVirtualCamera gameplayCamera;
    [SerializeField] private RoadScroller roadScroller;
    [SerializeField, Min(1f)] private float soloRoadHalfWidth = 5f;
    [SerializeField, Min(0f)] private float extraHalfWidthPerPlayer = 1.5f;
    [SerializeField, Min(0f)] private float extraSpawnDepthPerPlayer = 1.5f;
    [SerializeField, Min(0.5f)] private float playerSpacing = 2f;

    [Header("合作难度")]
    [SerializeField, Range(1, 5)] private int combatWaves = 3;
    [SerializeField, Min(0f)] private float extraEnemyHealthPerPlayer = 0.5f;
    [SerializeField, Min(0f)] private float extraEnemySpeedPerPlayer = 0.08f;
    [SerializeField, Min(0f)] private float extraEnemyDamagePerPlayer = 0.25f;

    private enum Result : byte { Playing, Victory, Defeat }
    private readonly NetworkVariable<int> teamSize = new();
    private readonly NetworkVariable<FixedString128Bytes> status = new("等待全员加载…");
    private readonly NetworkVariable<Result> result = new();
    private readonly List<ShooterCoopPlayer> players = new();
    private readonly List<ShooterPlayerHealth> teamHealth = new();
    private readonly HashSet<ShooterCoopEnemy> enemies = new();
    private Vector3[] roadScales;
    private Vector3[] boundaryPositions;
    private LensSettings initialLens;
    private ShooterRoom room;
    private ShooterCoopPlayer localPlayer;

    public static ShooterCoopRun Active { get; private set; }
    public BulletPool Bullets => bulletPool;
    private float CenterX => spawnCenter.position.x;
    private float RoadHalfWidth => soloRoadHalfWidth + extraHalfWidthPerPlayer * (teamSize.Value - 1);

    private void Awake()
    {
        room = ShooterRoom.Instance;
        if (room == null || !room.IsBusy)
            return;

        Active = this;
        Time.timeScale = 1f;
        director?.PrepareCoop();
        if (soloPlayer != null)
            soloPlayer.SetActive(false);
        foreach (GameObject actor in soloOnlyActors)
            if (actor != null)
                actor.SetActive(false);
        healthHud?.SetCoopPlayer(null);
        roadScales = CaptureTransforms(roadWidthObjects, true);
        boundaryPositions = CaptureTransforms(sideBoundaries, false);
        if (gameplayCamera != null)
            initialLens = gameplayCamera.m_Lens;
    }

    private static Vector3[] CaptureTransforms(Transform[] objects, bool captureScale)
    {
        if (objects == null)
            return System.Array.Empty<Vector3>();
        var values = new Vector3[objects.Length];
        for (int i = 0; i < objects.Length; i++)
            if (objects[i] != null)
                values[i] = captureScale ? objects[i].localScale : objects[i].position;
        return values;
    }

    public override void OnNetworkSpawn()
    {
        if (Active != this)
            return;

        runHud?.SetCoopExit(room.LeaveMatch);
        teamSize.OnValueChanged += OnTeamSizeChanged;
        status.OnValueChanged += OnStatusChanged;
        result.OnValueChanged += OnResultChanged;
        OnTeamSizeChanged(0, teamSize.Value);
        OnStatusChanged(default, status.Value);
        OnResultChanged(Result.Playing, result.Value);
    }

    public bool Begin(IReadOnlyList<ulong> completedClients)
    {
        if (!IsServer || !IsSpawned || teamSize.Value != 0 || soloPlayer == null ||
            director == null || runHud == null || healthHud == null || bulletPool == null ||
            coopSpawner == null || spawnCenter == null || playerPrefab == null || enemyPrefab == null ||
            gameplayCamera == null || !System.Array.Exists(roadWidthObjects ?? System.Array.Empty<Transform>(),
                roadObject => roadObject != null) ||
            !director.isActiveAndEnabled || !coopSpawner.isActiveAndEnabled)
            return false;

        var prefabs = NetworkManager.NetworkConfig.Prefabs;
        if (!prefabs.Contains(playerPrefab.gameObject) || !prefabs.Contains(enemyPrefab.gameObject))
            return false;

        var clientIds = new List<ulong>(completedClients.Count);
        foreach (ulong clientId in completedClients)
            if (NetworkManager.ConnectedClients.ContainsKey(clientId))
                clientIds.Add(clientId);
        if (clientIds.Count == 0)
            return false;

        clientIds.Sort();
        teamSize.Value = clientIds.Count;
        float moveHalfWidth = RoadHalfWidth - 0.5f;
        float spacing = Mathf.Min(playerSpacing, 2f * moveHalfWidth / Mathf.Max(1, clientIds.Count - 1));
        for (int i = 0; i < clientIds.Count; i++)
        {
            Vector3 position = spawnCenter.position;
            position.x += (i - (clientIds.Count - 1) * 0.5f) * spacing;
            ShooterCoopPlayer member = Instantiate(playerPrefab, position, spawnCenter.rotation);
            member.ConfigureServer(bulletPool, CenterX, moveHalfWidth);
            member.NetworkObject.SpawnAsPlayerObject(clientIds[i], destroyWithScene: true);
            players.Add(member);
            teamHealth.Add(member.Health);
        }

        NetworkManager.OnClientDisconnectCallback += OnPlayerDisconnected;
        director.CoopStatusChanged += PublishStatus;
        director.CoopFinished += Finish;
        director.BeginCoop(teamHealth, Mathf.Clamp(combatWaves, 1, 5), SpawnBatch);
        return true;
    }

    public void BindLocalPlayer(ShooterCoopPlayer player)
    {
        localPlayer = player;
        healthHud?.SetCoopPlayer(player.Health);
        if (result.Value != Result.Playing)
            player.StopPlaying();
    }

    private void SpawnBatch(EnemyStats stats)
    {
        enemies.RemoveWhere(enemy => enemy == null || !enemy.IsSpawned);
        int count = teamSize.Value;
        EnemyStats scaledStats = stats.ForTeam(count, extraEnemyHealthPerPlayer,
            extraEnemySpeedPerPlayer, extraEnemyDamagePerPlayer);
        coopSpawner.SpawnTeamBatch(scaledStats, count, enemies.Count, CenterX,
            RoadHalfWidth - 1f, extraSpawnDepthPerPlayer * (count - 1), SpawnEnemy);
    }

    private void SpawnEnemy(Vector3 position, Quaternion rotation, EnemyStats stats)
    {
        ShooterCoopEnemy enemy = Instantiate(enemyPrefab, position, rotation);
        enemy.Initialize(stats, FindLivingPlayer);
        enemy.NetworkObject.Spawn(destroyWithScene: true);
        enemies.Add(enemy);
    }

    private ShooterPlayerHealth FindLivingPlayer(Vector3 position)
    {
        ShooterPlayerHealth best = null;
        float distance = float.PositiveInfinity;
        foreach (ShooterCoopPlayer member in players)
        {
            if (member == null || !member.IsSpawned || member.Health.IsDead)
                continue;

            float delta = Mathf.Abs(member.transform.position.x - position.x);
            if (delta < distance)
            {
                distance = delta;
                best = member.Health;
            }
        }
        return best;
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        if (NetworkManager.ShutdownInProgress)
            return;

        for (int i = players.Count - 1; i >= 0; i--)
        {
            ShooterCoopPlayer member = players[i];
            if (member != null && member.IsSpawned && member.OwnerClientId != clientId)
                continue;
            director.RemoveCoopPlayer(teamHealth[i]);
            teamHealth.RemoveAt(i);
            players.RemoveAt(i);
        }
        director.CheckCoopTeam();
    }

    private void PublishStatus(string message)
    {
        status.Value = new FixedString128Bytes(message);
    }

    private void OnStatusChanged(FixedString128Bytes _, FixedString128Bytes next)
    {
        runHud?.SetStatus(next.ToString());
    }

    private void Finish(bool won)
    {
        foreach (ShooterCoopPlayer member in players)
            if (member != null)
                member.StopPlaying();
        result.Value = won ? Result.Victory : Result.Defeat;
    }

    private void OnResultChanged(Result _, Result next)
    {
        if (next == Result.Playing)
            return;

        localPlayer?.StopPlaying();
        roadScroller?.SetScrolling(false);
        runHud?.ShowResult(next == Result.Victory);
    }

    private void OnTeamSizeChanged(int _, int next)
    {
        if (next < 1 || spawnCenter == null)
            return;

        float scale = RoadHalfWidth / soloRoadHalfWidth;
        for (int i = 0; i < roadScales.Length; i++)
        {
            if (roadWidthObjects[i] == null)
                continue;
            Vector3 size = roadScales[i];
            size.x *= scale;
            roadWidthObjects[i].localScale = size;
        }
        for (int i = 0; i < boundaryPositions.Length; i++)
        {
            if (sideBoundaries[i] == null)
                continue;
            Vector3 position = boundaryPositions[i];
            position.x = CenterX + (position.x - CenterX) * scale;
            sideBoundaries[i].position = position;
        }
        if (gameplayCamera != null)
        {
            LensSettings lens = initialLens;
            lens.OrthographicSize *= scale;
            float halfAngle = initialLens.FieldOfView * Mathf.Deg2Rad * 0.5f;
            lens.FieldOfView = 2f * Mathf.Atan(Mathf.Tan(halfAngle) * scale) * Mathf.Rad2Deg;
            gameplayCamera.m_Lens = lens;
        }
    }

    public override void OnNetworkDespawn()
    {
        teamSize.OnValueChanged -= OnTeamSizeChanged;
        status.OnValueChanged -= OnStatusChanged;
        result.OnValueChanged -= OnResultChanged;
        if (director != null)
        {
            director.CoopStatusChanged -= PublishStatus;
            director.CoopFinished -= Finish;
            director.StopAllCoroutines();
        }
        if (NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnPlayerDisconnected;
    }

    public override void OnDestroy()
    {
        if (Active == this)
            Active = null;
        base.OnDestroy();
    }
}
