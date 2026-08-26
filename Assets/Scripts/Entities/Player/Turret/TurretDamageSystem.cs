using Assets.Scripts.Entities.Game;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct TurretDamageSystem : ISystem
  {
    [BurstCompile]
    [WithNone(typeof(TurretDefeated))]
    partial struct ApplyDamageJob : IJobEntity
    {
      public EntityCommandBuffer.ParallelWriter Ecb;

      public void Execute([EntityIndexInQuery] int entityIndex, Entity entity, ref TurretHealth health, in TurretBase turretBase, ref DynamicBuffer<DamageEvent> damageEvents)
      {
        if (damageEvents.Length == 0)
          return;

        var totalDamage = 0f;
        foreach (var damageEvent in damageEvents)
          totalDamage += damageEvent.DamageAmount;
        damageEvents.Clear();

        health.CurrentHealth = math.max(0f, health.CurrentHealth - totalDamage);
        if (health.CurrentHealth == 0f)
          Ecb.AddComponent<TurretDefeated>(entityIndex, entity);
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
        .CreateCommandBuffer(state.WorldUnmanaged)
        .AsParallelWriter();
      state.Dependency = new ApplyDamageJob { Ecb = ecb }.ScheduleParallel(state.Dependency);
    }
  }

  public struct TurretHealth : IComponentData
  {
    public float MaxHealth;
    public float CurrentHealth;
  }

  public struct TurretDefeated : IComponentData
  {
  }
}
