using Assets.Scripts.Entities.Enemy;
using Assets.Scripts.Entities.Player.Character;
using Assets.Scripts.Entities.Player.Turret;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Game
{
  [UpdateInGroup(typeof(PhysicsSystemGroup))]
  [UpdateAfter(typeof(PhysicsSimulationGroup))]
  public partial struct EnemyContactDamageSystem : ISystem
  {
    private const float ContactDamage = 1f;
    private const float ContactKnockbackForce = 5f;

    [BurstCompile]
    partial struct EnemyContactDamageJob : ITriggerEventsJob
    {
      [ReadOnly] public ComponentLookup<SimpleEnemy> EnemyLookup;
      [ReadOnly] public ComponentLookup<PlayerAttributes> PlayerLookup;
      [ReadOnly] public ComponentLookup<TurretBase> TurretBaseLookup;
      [ReadOnly] public ComponentLookup<PlayerDefeated> PlayerDefeatedLookup;
      [ReadOnly] public ComponentLookup<TurretDefeated> TurretDefeatedLookup;
      [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
      public BufferLookup<DamageEvent> DamageEventLookup;
      public BufferLookup<KnockbackEvent> KnockbackEventLookup;

      public void Execute(TriggerEvent triggerEvent)
      {
        HandleContact(triggerEvent.EntityA, triggerEvent.EntityB);
        HandleContact(triggerEvent.EntityB, triggerEvent.EntityA);
      }

      void HandleContact(Entity enemyEntity, Entity targetEntity)
      {
        if (!EnemyLookup.HasComponent(enemyEntity))
          return;

        var isPlayer = PlayerLookup.HasComponent(targetEntity);
        var isTurretBase = TurretBaseLookup.HasComponent(targetEntity);
        if (!isPlayer && !isTurretBase)
          return;

        if ((isPlayer && PlayerDefeatedLookup.HasComponent(targetEntity))
          || (isTurretBase && TurretDefeatedLookup.HasComponent(targetEntity)))
          return;

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
        EnemyLookup = SystemAPI.GetComponentLookup<SimpleEnemy>(true),
        PlayerLookup = SystemAPI.GetComponentLookup<PlayerAttributes>(true),
        TurretBaseLookup = SystemAPI.GetComponentLookup<TurretBase>(true),
        PlayerDefeatedLookup = SystemAPI.GetComponentLookup<PlayerDefeated>(true),
        TurretDefeatedLookup = SystemAPI.GetComponentLookup<TurretDefeated>(true),
        LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
        DamageEventLookup = SystemAPI.GetBufferLookup<DamageEvent>(),
        KnockbackEventLookup = SystemAPI.GetBufferLookup<KnockbackEvent>()
      }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
  }
}
