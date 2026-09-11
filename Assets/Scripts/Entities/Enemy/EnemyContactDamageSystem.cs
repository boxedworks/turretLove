using Assets.Scripts.Entities.Game;
using Assets.Scripts.Entities.Player.Character;
using Assets.Scripts.Entities.Player.Turret;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Enemy
{
  [UpdateInGroup(typeof(PhysicsSystemGroup))]
  [UpdateAfter(typeof(PhysicsSimulationGroup))]
  public partial struct EnemyContactDamageSystem : ISystem
  {
    private const float ContactDamage = 1f;
    private const float ContactKnockbackForce = 5f;
    private const double ContactCooldownDuration = 0.02;

    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    partial struct EnemyContactDamageJob : ICollisionEventsJob
    {
      [ReadOnly] public ComponentLookup<LevelEntity> LevelEntityLookup;
      [ReadOnly] public ComponentLookup<PlayerDefeated> PlayerDefeatedLookup;
      [ReadOnly] public ComponentLookup<TurretDefeated> TurretDefeatedLookup;
      [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
      public ComponentLookup<ContactCooldown> ContactCooldownLookup;
      public BufferLookup<DamageEvent> DamageEventLookup;
      public BufferLookup<KnockbackEvent> KnockbackEventLookup;
      public double ElapsedTime;

      public void Execute(CollisionEvent collisionEvent)
      {
        HandleContact(collisionEvent.EntityA, collisionEvent.EntityB);
        HandleContact(collisionEvent.EntityB, collisionEvent.EntityA);
      }

      void HandleContact(Entity enemyEntity, Entity targetEntity)
      {
        if (!LevelEntityLookup.TryGetComponent(enemyEntity, out var enemy)
          || enemy.Type != LevelEntityType.Enemy)
        {
          return;
        }

        if (!LevelEntityLookup.TryGetComponent(targetEntity, out var target)
          || (target.Type != LevelEntityType.Player && target.Type != LevelEntityType.Turret))
        {
          return;
        }

        if ((target.Type == LevelEntityType.Player && PlayerDefeatedLookup.HasComponent(targetEntity))
          || (target.Type == LevelEntityType.Turret && TurretDefeatedLookup.HasComponent(targetEntity)))
          return;

        if (!DamageEventLookup.HasBuffer(targetEntity) || !KnockbackEventLookup.HasBuffer(enemyEntity))
          return;

        if (ContactCooldownLookup.HasComponent(enemyEntity))
        {
          var cooldown = ContactCooldownLookup[enemyEntity];
          if (ElapsedTime < cooldown.NextAllowedContactTime)
            return;

          cooldown.NextAllowedContactTime = ElapsedTime + ContactCooldownDuration;
          ContactCooldownLookup[enemyEntity] = cooldown;
        }

        var targetPosition = LocalTransformLookup[targetEntity].Position;
        var enemyPosition = LocalTransformLookup[enemyEntity].Position;
        DamageEventLookup[targetEntity].Add(new DamageEvent
        {
          DamagePosition = enemyPosition,
          DamageAmount = ContactDamage
        });
        KnockbackEventLookup[enemyEntity].Add(new KnockbackEvent
        {
          Direction = math.normalizesafe(enemyPosition - targetPosition),
          Force = ContactKnockbackForce
        });
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.Dependency = new EnemyContactDamageJob
      {
        LevelEntityLookup = SystemAPI.GetComponentLookup<LevelEntity>(true),
        PlayerDefeatedLookup = SystemAPI.GetComponentLookup<PlayerDefeated>(true),
        TurretDefeatedLookup = SystemAPI.GetComponentLookup<TurretDefeated>(true),
        LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
        ContactCooldownLookup = SystemAPI.GetComponentLookup<ContactCooldown>(),
        DamageEventLookup = SystemAPI.GetBufferLookup<DamageEvent>(),
        KnockbackEventLookup = SystemAPI.GetBufferLookup<KnockbackEvent>(),
        ElapsedTime = SystemAPI.Time.ElapsedTime
      }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
  }
}
