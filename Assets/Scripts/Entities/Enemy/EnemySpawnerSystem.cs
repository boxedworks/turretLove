
using Assets.Scripts.Entities.Player.Turret;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Assets.Scripts.Entities.Game;
using Unity.Burst;

namespace Assets.Scripts.Entities.Enemy
{
  public partial struct EnemySpawnerSystem : ISystem
  {
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
      var time = SystemAPI.Time.ElapsedTime;
      if (time - _lastSpawnTime < 0.25f)
        return;
      _lastSpawnTime = time;

      var enemySpawner = SystemAPI.GetSingleton<EnemySpawner>();
      var enemy = state.EntityManager.Instantiate(enemySpawner.GoblinPrefab);
      state.EntityManager.AddComponentData(enemy, new SimpleEnemy { Health = 1f, Speed = 1f });
      state.EntityManager.AddBuffer<DamageEvent>(enemy);

      // Spawn enemies arounnd the center of the map using 4 borders
      var random = new Random((uint)System.DateTime.Now.Ticks);
      var spawnSide = random.NextInt(0, 4);
      var spawnXRaidus = 10f;
      var spawnYRadius = 6f;
      var spawnPosition = float3.zero;
      switch (spawnSide)
      {
        case 0: // Top
          spawnPosition = new float3(random.NextFloat(-spawnXRaidus, spawnXRaidus), spawnYRadius, 0);
          break;
        case 1: // Right
          spawnPosition = new float3(spawnXRaidus, random.NextFloat(-spawnYRadius, spawnYRadius), 0);
          break;
        case 2: // Bottom
          spawnPosition = new float3(random.NextFloat(-spawnXRaidus, spawnXRaidus), -spawnYRadius, 0);
          break;
        case 3: // Left
          spawnPosition = new float3(-spawnXRaidus, random.NextFloat(-spawnYRadius, spawnYRadius), 0);
          break;
      }
      state.EntityManager.SetComponentData(enemy, LocalTransform.FromPosition(spawnPosition));

      // Change collision layer to avoid colliding with the map
      var physicsCollider = SystemAPI.GetComponentRW<Unity.Physics.PhysicsCollider>(enemy);
      var collider = physicsCollider.ValueRO.Value;
      var filter = collider.Value.GetCollisionFilter();
      filter.BelongsTo = 1 << 4;
      filter.CollidesWith = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 4);
      collider.Value.SetCollisionFilter(filter);
      physicsCollider.ValueRW.Value = collider;

      // Add material override
      state.EntityManager.AddComponentData(enemy, new ColorOverride { Value = new float4(1f, 1f, 1f, 1f) });
    }

    //
    [MaterialProperty("_Color")]
    public struct ColorOverride : IComponentData
    {
      public float4 Value;
    }
  }
}