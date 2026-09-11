using Assets.Scripts.Entities.Game;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Player.Character
{
  public partial struct PlayerDamageSystem : ISystem
  {
    [BurstCompile]
    [WithNone(typeof(PlayerDefeated))]
    partial struct ApplyDamageJob : IJobEntity
    {
      public EntityCommandBuffer.ParallelWriter Ecb;

      public void Execute([EntityIndexInQuery] int entityIndex, Entity entity, ref PlayerAttributes attributes, ref DynamicBuffer<DamageEvent> damageEvents)
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

        attributes.CurrentHealth = math.max(0f, attributes.CurrentHealth - totalDamage);
        if (attributes.CurrentHealth == 0f)
          Ecb.AddComponent<PlayerDefeated>(entityIndex, entity);
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

  public struct PlayerDefeated : IComponentData
  {
  }
}
