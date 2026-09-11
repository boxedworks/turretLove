using Assets.Scripts.Entities.Game;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct TurretDamageSystem : ISystem
  {
    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<TurretAttributes>();
    }

    [BurstCompile]
    [WithNone(typeof(TurretDefeated))]
    partial struct ApplyDamageJob : IJobEntity
    {
      public EntityCommandBuffer.ParallelWriter Ecb;

      public void Execute([EntityIndexInQuery] int entityIndex, Entity entity, ref TurretHealth health, in TurretAttributes turretAttributes, ref DynamicBuffer<DamageEvent> damageEvents)
      {
        if (damageEvents.Length == 0)
          return;

        var totalDamage = 0f;
        foreach (var damageEvent in damageEvents)
          totalDamage += damageEvent.DamageAmount;
        damageEvents.Clear();

        Ecb.AddComponent(entityIndex, entity, new BlinkEffect
        {
          Rate = 0.1f,
          BlinkColor = new float4(1f, 0f, 0f, 1f),
          BlinkCount = 6,
        });
        Ecb.AddComponent(entityIndex, turretAttributes.TurretTopEntity, new BlinkEffect
        {
          Rate = 0.1f,
          BlinkColor = new float4(1f, 0f, 0f, 1f),
          BlinkCount = 6,
        });

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
      state.Dependency = new ApplyDamageJob
      {
        Ecb = ecb
      }.ScheduleParallel(state.Dependency);
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
