using Assets.Scripts.Entities.Enemy;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Game
{
  public enum DamageOverTimeType : byte
  {
    Fire,
    Poison
  }

  public struct DamageOverTimeEffect : IBufferElementData
  {
    public DamageOverTimeType Type;
    public float DamagePerTick;
    public double NextTickTime;
    public double ExpiresAt;
  }

  [UpdateBefore(typeof(EnemyDamageSystem))]
  public partial struct DamageOverTimeSystem : ISystem
  {
    private const float TickInterval = 0.5f;

    [BurstCompile]
    private partial struct TickDamageOverTimeJob : IJobEntity
    {
      public double CurrentTime;

      public void Execute(ref DynamicBuffer<DamageOverTimeEffect> effects, ref DynamicBuffer<DamageEvent> damageEvents)
      {
        for (var index = effects.Length - 1; index >= 0; index--)
        {
          var effect = effects[index];
          if (CurrentTime >= effect.ExpiresAt)
          {
            effects.RemoveAt(index);
            continue;
          }

          if (CurrentTime < effect.NextTickTime)
            continue;

          damageEvents.Add(new DamageEvent { DamageAmount = effect.DamagePerTick });
          effect.NextTickTime = math.min(effect.NextTickTime + TickInterval, CurrentTime + TickInterval);
          effects[index] = effect;
        }
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.Dependency = new TickDamageOverTimeJob
      {
        CurrentTime = SystemAPI.Time.ElapsedTime
      }.Schedule(state.Dependency);
    }

    public static void Merge(
      DynamicBuffer<DamageOverTimeEffect> effects,
      DamageOverTimeType type,
      float damagePerSecond,
      float duration,
      double currentTime)
    {
      if (damagePerSecond <= 0f || duration <= 0f)
        return;

      var damagePerTick = damagePerSecond * TickInterval;
      for (var index = 0; index < effects.Length; index++)
      {
        var effect = effects[index];
        if (effect.Type != type)
          continue;

        effect.DamagePerTick = math.max(effect.DamagePerTick, damagePerTick);
        effect.ExpiresAt = math.max(effect.ExpiresAt, currentTime + duration);
        effects[index] = effect;
        return;
      }

      effects.Add(new DamageOverTimeEffect
      {
        Type = type,
        DamagePerTick = damagePerTick,
        NextTickTime = currentTime + TickInterval,
        ExpiresAt = currentTime + duration
      });
    }
  }
}
