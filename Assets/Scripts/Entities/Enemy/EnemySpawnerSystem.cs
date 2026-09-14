using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Assets.Scripts.Entities.Game;
using Assets.Scripts.Entities.Game.Scroll;
using Assets.Scripts.Entities.Player.Turret;
using Unity.Burst;

namespace Assets.Scripts.Entities.Enemy
{
  public partial struct EnemySpawnerSystem : ISystem
  {
    private const double SpawnRampDuration = 60.0;
    private const double InitialSpawnInterval = 3.0;
    private const double MinimumSpawnInterval = 0.3;

    private double _lastSpawnTime;
    private Random _random;
    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<EnemySpawner>();

      _lastSpawnTime = 0.0;
      _random = new Random((uint)System.DateTime.Now.Ticks);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      // Check level state - only spawn if level is running
      if (!SystemAPI.TryGetSingleton<LevelState>(out var levelState))
        return;

      if (levelState.CurrentState != LevelState.State.Running)
        return;

      var time = SystemAPI.Time.ElapsedTime;
      var rampProgress = math.saturate((float)(time / SpawnRampDuration));
      var spawnInterval = math.lerp((float)InitialSpawnInterval, (float)MinimumSpawnInterval, rampProgress);
      if (time - _lastSpawnTime < spawnInterval)
        return;
      _lastSpawnTime = time;
      var enemySpawner = SystemAPI.GetSingleton<EnemySpawner>();

      var enemyType = _random.NextFloat(0f, 1f) < 0.8f ? EnemyType.Goblin : EnemyType.Tree;
      GetEnemyStats(enemyType, out var health, out var speed, out var mass, out var scale);
      var prefab = enemyType switch
      {
        EnemyType.Goblin => enemySpawner.GoblinPrefab,
        EnemyType.Ghost => enemySpawner.GhostPrefab,
        EnemyType.Tree => enemySpawner.TreePrefab,
        _ => enemySpawner.GoblinPrefab
      };

      var enemy = state.EntityManager.Instantiate(prefab);
      state.EntityManager.AddComponentData(enemy, new SimpleEnemy { Type = enemyType, Health = health });
      state.EntityManager.AddComponentData(enemy, new ScrollComponent { Direction = new float2(-1f, 0f), Speed = 0.5f });
      state.EntityManager.AddComponentData(enemy, new ColorOverride { Value = new float4(1f, 1f, 1f, 1f) });
      state.EntityManager.AddComponentData(enemy, new ContactCooldown { NextAllowedContactTime = 0 });
      state.EntityManager.AddComponentData(enemy, new LevelEntity { Type = LevelEntityType.Enemy });
      if (speed > 0f)
      {
        state.EntityManager.AddComponentData(enemy, new AttractToPlayer { Speed = speed * 5f });
        state.EntityManager.AddComponent<TurretTargetable>(enemy);
      }
      state.EntityManager.AddBuffer<DamageEvent>(enemy);
      state.EntityManager.AddBuffer<KnockbackEvent>(enemy);
      state.EntityManager.AddBuffer<DamageOverTimeEffect>(enemy);

      try
      {
        var physicsMass = state.EntityManager.GetComponentData<Unity.Physics.PhysicsMass>(enemy);
        physicsMass.InverseMass = 1f / mass;
        state.EntityManager.SetComponentData(enemy, physicsMass);
      }
      catch (System.ArgumentException) { }

      // Spawn enemies arounnd the center of the map using 4 borders
      var spawnSide = 1;//_random.NextInt(0, 4);
      var spawnXRaidus = 10f;
      var spawnYRadius = 5f;
      var spawnPosition = float3.zero;
      switch (spawnSide)
      {
        case 0: // Top
          spawnPosition = new float3(_random.NextFloat(-spawnXRaidus, spawnXRaidus), spawnYRadius, 0);
          break;
        case 1: // Right
          spawnPosition = new float3(spawnXRaidus, _random.NextFloat(-spawnYRadius, spawnYRadius), 0);
          break;
        case 2: // Bottom
          spawnPosition = new float3(_random.NextFloat(-spawnXRaidus, spawnXRaidus), -spawnYRadius, 0);
          break;
        case 3: // Left
          spawnPosition = new float3(-spawnXRaidus, _random.NextFloat(-spawnYRadius, spawnYRadius), 0);
          break;
      }

      // Set position and scale
      var localTransform = new LocalTransform
      {
        Position = spawnPosition,
        Scale = scale,
        Rotation = quaternion.identity
      };
      state.EntityManager.SetComponentData(enemy, localTransform);

      // Change collision layer to avoid colliding with the map
      var physicsCollider = SystemAPI.GetComponentRW<Unity.Physics.PhysicsCollider>(enemy);
      var collider = physicsCollider.ValueRO.Value;
      var filter = collider.Value.GetCollisionFilter();
      filter.BelongsTo = 1 << 4;
      filter.CollidesWith = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 4) | (1 << 6);
      collider.Value.SetCollisionFilter(filter);
      physicsCollider.ValueRW.Value = collider;
    }

    private static void GetEnemyStats(EnemyType enemyType, out float health, out float speed, out float mass, out float scale)
    {
      switch (enemyType)
      {
        case EnemyType.Goblin:
          health = 2f;
          speed = 0.3f;
          mass = 1f;
          scale = 0.5f;
          break;
        case EnemyType.Ghost:
          health = 2f;
          speed = 0.3f;
          mass = 0.1f;
          scale = 0.5f;
          break;
        case EnemyType.Tree:
          health = 2f;
          speed = 0f;
          mass = 50f;
          scale = 1f;
          break;
        default:
          health = 2f;
          speed = 0.3f;
          mass = 1f;
          scale = 0.5f;
          break;
      }
    }
  }
}