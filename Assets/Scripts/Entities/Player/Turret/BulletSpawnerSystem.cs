
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{

  public partial struct BulletSpawnerSystem : ISystem
  {
    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<BulletSpawner>();

      var spawnerEntity = state.EntityManager.CreateEntity();
      state.EntityManager.AddBuffer<BulletSpawnEvent>(spawnerEntity);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var bulletSpawner = SystemAPI.GetSingleton<BulletSpawner>();

      var bulletSpawnEventBuffer = SystemAPI.GetSingletonBuffer<BulletSpawnEvent>();
      if (bulletSpawnEventBuffer.Length == 0)
        return;

      var events = bulletSpawnEventBuffer.ToNativeArray(Unity.Collections.Allocator.Temp);
      bulletSpawnEventBuffer.Clear();

      foreach (var spawnEvent in events)
      {
        var bulletEntity = state.EntityManager.Instantiate(bulletSpawner.Prefab);
        state.EntityManager.AddComponent<Bullet>(bulletEntity);
        state.EntityManager.SetComponentData(bulletEntity, LocalTransform.FromPosition(float3.zero));

        // For testing, set random rotation and velocity
        var spawnPosition = spawnEvent.SpawnPosition;
        var rotation = spawnEvent.SpawnRotation;
        var scale = 0.25f;

        // Set position, rotation and scale
        state.EntityManager.SetComponentData(bulletEntity, LocalTransform.FromPositionRotationScale(spawnPosition, rotation, scale));

        var velocity = SystemAPI.GetComponentRW<PhysicsVelocity>(bulletEntity);
        var force = 5f;
        velocity.ValueRW.Linear = math.mul(rotation, new float3(0, 1f, 0)) * force;
      }
      events.Dispose();
    }
  }

  public partial struct BulletSpawnEvent : IBufferElementData
  {
    public float3 SpawnPosition;
    public quaternion SpawnRotation;
  }

  public partial struct Bullet : IComponentData
  {
  }
}