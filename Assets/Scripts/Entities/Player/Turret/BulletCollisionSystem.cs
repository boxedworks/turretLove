
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{

  [UpdateInGroup(typeof(PhysicsSystemGroup))]
  [UpdateAfter(typeof(PhysicsSimulationGroup))]
  public partial struct BulletCollisionSystem : ISystem
  {

    [BurstCompile]
    public partial struct BulletCollisionJob : ITriggerEventsJob
    {
      public EntityCommandBuffer.ParallelWriter Ecb;

      [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
      [ReadOnly] public ComponentLookup<Bullet> BulletLookup;
      public BufferLookup<BulletCollisionEvent> BulletCollisionEventLookup;

      // Check if either entity in the trigger event is a bullet, and if so, handle the collision accordingly
      // If bullet collides with something, destroy the bullet and add a collision event to the other entity
      public void Execute(TriggerEvent triggerEvent)
      {

        var entityA = triggerEvent.EntityA;
        var entityB = triggerEvent.EntityB;

        // Gather entity types
        var isEntityABullet = BulletLookup.HasComponent(entityA);
        var isEntityBBullet = BulletLookup.HasComponent(entityB);

        var isBulletCollision = isEntityABullet || isEntityBBullet;
        if (!isBulletCollision)
          return;

        // Destroy any bullet that collides with something
        if (isEntityABullet)
          HandleCollisionAsBullet(triggerEvent.BodyIndexA, triggerEvent.EntityA);
        else
          HandleCollisionAsNonBullet(triggerEvent.BodyIndexA, triggerEvent.EntityA, LocalTransformLookup[entityB].Position);

        if (isEntityBBullet)
          HandleCollisionAsBullet(triggerEvent.BodyIndexB, triggerEvent.EntityB);
        else
          HandleCollisionAsNonBullet(triggerEvent.BodyIndexB, triggerEvent.EntityB, LocalTransformLookup[entityA].Position);
      }

      // If a bullet collides with something, destroy the bullet
      void HandleCollisionAsBullet(int entityIndex, Entity bulletEntity)
      {
        Ecb.DestroyEntity(entityIndex, bulletEntity);
      }

      // If a bullet collides with a non-bullet entity, add a collision event to that entity's buffer
      void HandleCollisionAsNonBullet(int entityIndex, Entity nonBulletEntity, float3 bulletPosition)
      {
        if (!BulletCollisionEventLookup.HasBuffer(nonBulletEntity))
          return;
        var buffer = BulletCollisionEventLookup[nonBulletEntity];
        buffer.Add(new BulletCollisionEvent()
        {
          BulletPosition = bulletPosition
        });
      }
    }

    // Gather trigger events and schedule the job
    public void OnUpdate(ref SystemState state)
    {
      var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
      var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
      state.Dependency =
        new BulletCollisionJob()
        {
          Ecb = ecb.AsParallelWriter(),

          LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
          BulletLookup = SystemAPI.GetComponentLookup<Bullet>(true),
          BulletCollisionEventLookup = SystemAPI.GetBufferLookup<BulletCollisionEvent>(),
        }
        .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
  }
}