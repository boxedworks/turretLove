
using Assets.Scripts.Entities.Enemy;
using Assets.Scripts.Entities.Game;
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
      [ReadOnly] public ComponentLookup<SimpleEnemy> EnemyLookup;
      [ReadOnly] public ComponentLookup<TurretTop> TurretTopLookup;
      public BufferLookup<DamageEvent> DamageEventLookup;
      public BufferLookup<KnockbackEvent> KnockbackEventLookup;

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

        // var isEnemyCollision = EnemyLookup.HasComponent(entityA) || EnemyLookup.HasComponent(entityB);
        // if (!isEnemyCollision)
        //   return;

        var isTurretCollision = TurretTopLookup.HasComponent(entityA) || TurretTopLookup.HasComponent(entityB);
        if (isTurretCollision)
          return;

        // Destroy any bullet that collides with something
        if (isEntityABullet)
          HandleCollisionAsBullet(triggerEvent.BodyIndexA, entityA);
        else
          HandleCollisionAsNonBullet(triggerEvent.BodyIndexA, entityA, LocalTransformLookup[entityB].Position, LocalTransformLookup[entityA]);

        if (isEntityBBullet)
          HandleCollisionAsBullet(triggerEvent.BodyIndexB, entityB);
        else
          HandleCollisionAsNonBullet(triggerEvent.BodyIndexB, entityB, LocalTransformLookup[entityA].Position, LocalTransformLookup[entityB]);
      }

      // If a bullet collides with something, destroy the bullet
      void HandleCollisionAsBullet(int entityIndex, Entity bulletEntity)
      {
        Ecb.DestroyEntity(entityIndex, bulletEntity);
      }

      // If a bullet collides with a non-bullet entity, add a collision event to that entity's buffer
      void HandleCollisionAsNonBullet(int entityIndex, Entity nonBulletEntity, float3 bulletPosition, LocalTransform transform)
      {
        if (!DamageEventLookup.HasBuffer(nonBulletEntity))
          return;
        var damageEventBuffer = DamageEventLookup[nonBulletEntity];
        damageEventBuffer.Add(new DamageEvent()
        {
          BulletPosition = bulletPosition
        });

        // Apply a knockback force to the entity based on the direction from the bullet to the entity
        var knockbackEventBuffer = KnockbackEventLookup[nonBulletEntity];
        knockbackEventBuffer.Add(new KnockbackEvent()
        {
          Direction = math.normalize(transform.Position - bulletPosition),
          Force = 5f
        });
      }
    }

    // Gather trigger events and schedule the job
    [BurstCompile]
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
          EnemyLookup = SystemAPI.GetComponentLookup<SimpleEnemy>(true),
          TurretTopLookup = SystemAPI.GetComponentLookup<TurretTop>(true),

          DamageEventLookup = SystemAPI.GetBufferLookup<DamageEvent>(),
        }
        .Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
  }
}