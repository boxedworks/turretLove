using Assets.Scripts.Entities.Game;
using Assets.Scripts.Entities.Player.Turret;
using Assets.Scripts.Entities.Skills;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Assets.Scripts.Entities.Player.Character
{
  [UpdateInGroup(typeof(PhysicsSystemGroup))]
  [UpdateAfter(typeof(PhysicsSimulationGroup))]
  public partial struct PlayerTurretCollisionSystem : ISystem
  {
    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<SimulationSingleton>();
      state.RequireForUpdate<TurretAmmo>();
    }

    [BurstCompile]
    private struct StartReloadJob : ICollisionEventsJob
    {
      [ReadOnly] public ComponentLookup<LevelEntity> LevelEntityLookup;
      [ReadOnly] public ComponentLookup<TurretAmmo> TurretAmmoLookup;
      public BufferLookup<SkillTriggerEvent> SkillTriggerEventLookup;

      public void Execute(CollisionEvent collisionEvent)
      {
        TryStartReload(collisionEvent.EntityA, collisionEvent.EntityB);
        TryStartReload(collisionEvent.EntityB, collisionEvent.EntityA);
      }

      private void TryStartReload(Entity playerEntity, Entity turretEntity)
      {
        if (!LevelEntityLookup.TryGetComponent(playerEntity, out var player)
          || player.Type != LevelEntityType.Player
          || !LevelEntityLookup.TryGetComponent(turretEntity, out var turret)
          || turret.Type != LevelEntityType.Turret
          || !TurretAmmoLookup.TryGetComponent(turretEntity, out var ammo)
          || ammo.CurrentAmmo > 0
          || !SkillTriggerEventLookup.HasBuffer(playerEntity))
        {
          return;
        }

        SkillTriggerEvent.Trigger(
          SkillTriggerEventLookup[playerEntity],
          new SkillTriggerEvent
          {
            Type = SkillType.Reload,
            TargetEntity = turretEntity
          });
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.Dependency = new StartReloadJob
      {
        LevelEntityLookup = SystemAPI.GetComponentLookup<LevelEntity>(true),
        TurretAmmoLookup = SystemAPI.GetComponentLookup<TurretAmmo>(true),
        SkillTriggerEventLookup = SystemAPI.GetBufferLookup<SkillTriggerEvent>()
      }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }
  }
}
