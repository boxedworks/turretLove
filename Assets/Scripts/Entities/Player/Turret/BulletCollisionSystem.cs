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
    private struct BulletCollisionJob : ITriggerEventsJob
    {
      public EntityCommandBuffer.ParallelWriter Ecb;
      [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
      [ReadOnly] public ComponentLookup<Bullet> BulletLookup;
      [ReadOnly] public ComponentLookup<LevelEntity> LevelEntityLookup;
      public BufferLookup<DamageEvent> DamageEventLookup;
      public BufferLookup<KnockbackEvent> KnockbackEventLookup;

      public void Execute(TriggerEvent triggerEvent)
      {
        var entityA = triggerEvent.EntityA;
        var entityB = triggerEvent.EntityB;
        var isBulletA = BulletLookup.HasComponent(entityA);
        var isBulletB = BulletLookup.HasComponent(entityB);
        if (!isBulletA && !isBulletB)
          return;
        if (isBulletA && isBulletB)
          return;

        if (isBulletA)
        {
          Ecb.DestroyEntity(triggerEvent.BodyIndexA, entityA);
          if (!isBulletB)
            DamageTarget(entityB, entityA);
        }

        if (isBulletB)
        {
          Ecb.DestroyEntity(triggerEvent.BodyIndexB, entityB);
          if (!isBulletA)
            DamageTarget(entityA, entityB);
        }
      }

      private void DamageTarget(Entity targetEntity, Entity bulletEntity)
      {
        if (!LevelEntityLookup.TryGetComponent(targetEntity, out var target)
          || target.Type != LevelEntityType.Enemy
          || !DamageEventLookup.HasBuffer(targetEntity))
          return;

        var bullet = BulletLookup[bulletEntity];
        var stats = bullet.Payload.Stats;
        var bulletPosition = LocalTransformLookup.HasComponent(bulletEntity)
          ? LocalTransformLookup[bulletEntity].Position
          : float3.zero;
        DamageEventLookup[targetEntity].Add(new DamageEvent
        {
          DamagePosition = bulletPosition,
          DamageAmount = stats.Damage,
          FireDamagePerSecond = stats.FireDamagePerSecond,
          FireDuration = stats.FireDuration,
          PoisonDamagePerSecond = stats.PoisonDamagePerSecond,
          PoisonDuration = stats.PoisonDuration
        });

        if (!KnockbackEventLookup.HasBuffer(targetEntity) || !LocalTransformLookup.HasComponent(targetEntity))
          return;

        var direction = math.normalizesafe(LocalTransformLookup[targetEntity].Position - bulletPosition);
        KnockbackEventLookup[targetEntity].Add(new KnockbackEvent
        {
          Direction = direction,
          Force = stats.Knockback
        });
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
        .CreateCommandBuffer(state.WorldUnmanaged);
      state.Dependency = new BulletCollisionJob
      {
        Ecb = ecb.AsParallelWriter(),
        LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
        BulletLookup = SystemAPI.GetComponentLookup<Bullet>(true),
        LevelEntityLookup = SystemAPI.GetComponentLookup<LevelEntity>(true),
        DamageEventLookup = SystemAPI.GetBufferLookup<DamageEvent>(),
        KnockbackEventLookup = SystemAPI.GetBufferLookup<KnockbackEvent>()
      }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
  }
}
