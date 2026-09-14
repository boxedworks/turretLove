using Assets.Scripts.Entities.Game;
using Assets.Scripts.Entities.Game.Scroll;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{
  [UpdateBefore(typeof(TransformSystemGroup))]
  [UpdateAfter(typeof(TurretSystem))]
  public partial struct BulletSpawnerSystem : ISystem
  {
    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<BulletSpawner>();
      var eventEntity = state.EntityManager.CreateEntity();
      state.EntityManager.AddBuffer<BulletSpawnEvent>(eventEntity);
      state.EntityManager.AddBuffer<PendingBulletShot>(eventEntity);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var bulletEvents = SystemAPI.GetSingletonBuffer<BulletSpawnEvent>();
      var delayedShots = SystemAPI.GetSingletonBuffer<PendingBulletShot>();
      var currentTime = SystemAPI.Time.ElapsedTime;
      for (var index = delayedShots.Length - 1; index >= 0; index--)
      {
        if (currentTime < delayedShots[index].FireAtTime)
          continue;

        var delayedShot = delayedShots[index];
        bulletEvents.Add(new BulletSpawnEvent
        {
          SpawnPosition = delayedShot.SpawnPosition,
          SpawnRotation = delayedShot.SpawnRotation,
          Payload = delayedShot.Payload
        });
        delayedShots.RemoveAt(index);
      }

      if (bulletEvents.Length == 0)
        return;

      var bulletSpawner = SystemAPI.GetSingleton<BulletSpawner>();
      var events = bulletEvents.ToNativeArray(Allocator.Temp);
      bulletEvents.Clear();

      foreach (var spawnEvent in events)
      {
        var bulletEntity = state.EntityManager.Instantiate(bulletSpawner.Prefab);
        state.EntityManager.AddComponentData(bulletEntity, new Bullet { Payload = spawnEvent.Payload });
        state.EntityManager.AddComponent<LevelEntity>(bulletEntity);
        state.EntityManager.AddComponent<ScrollComponent>(bulletEntity);

        var stats = spawnEvent.Payload.Stats;
        state.EntityManager.SetComponentData(
          bulletEntity,
          LocalTransform.FromPositionRotationScale(spawnEvent.SpawnPosition, spawnEvent.SpawnRotation, stats.Size));

        var velocity = state.EntityManager.GetComponentData<PhysicsVelocity>(bulletEntity);
        velocity.Linear = math.mul(spawnEvent.SpawnRotation, new float3(0, 1f, 0)) * stats.Speed;
        state.EntityManager.SetComponentData(bulletEntity, velocity);
      }
      events.Dispose();
    }
  }

  public struct BulletSpawnEvent : IBufferElementData
  {
    public float3 SpawnPosition;
    public quaternion SpawnRotation;
    public BulletProjectilePayload Payload;
  }

  public struct PendingBulletShot : IBufferElementData
  {
    public double FireAtTime;
    public float3 SpawnPosition;
    public quaternion SpawnRotation;
    public BulletProjectilePayload Payload;
  }

  public struct Bullet : IComponentData
  {
    public BulletProjectilePayload Payload;
  }
}
