
using Assets.Scripts.Entities.Enemy;
using Assets.Scripts.Entities.Game.Audio;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Game
{
  public partial struct DamageSystem : ISystem
  {
    [BurstCompile]
    partial struct DamageJob : IJobEntity
    {
      public EntityCommandBuffer Ecb;
      public DynamicBuffer<AudioEvent> AudioEventBuffer;

      public readonly void Execute(Entity entity, ref SimpleEnemy simpleEnemy, ref DynamicBuffer<DamageEvent> damageBuffer)
      {
        if (damageBuffer.Length > 0)
        {
          // Handle collision events
          foreach (var damageEvent in damageBuffer)
          {
            simpleEnemy.Health -= damageEvent.DamageAmount;
          }
          damageBuffer.Clear();

          // Destroy entity using ecb
          if (simpleEnemy.Health <= 0f)
          {
            Ecb.DestroyEntity(entity);

            // Add audio event for enemy death
            AudioEventBuffer.Add(new AudioEvent { Type = AudioEvent.EventType.EnemyDestroy });
          }
          else
          {

            // Add blink effect for enemy death
            Ecb.AddComponent(entity, new BlinkEffect
            {
              Rate = 0.1f,
              BlinkColor = new float4(1f, 0f, 0f, 1f),
              BlinkCount = 6,
            });

            // Add audio event for enemy damage
            AudioEventBuffer.Add(new AudioEvent { Type = AudioEvent.EventType.GoblinDamage });
          }
        }
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
      var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
      state.Dependency = new DamageJob()
      {
        Ecb = ecb,
        AudioEventBuffer = SystemAPI.GetBuffer<AudioEvent>(SystemAPI.GetSingletonEntity<AudioEvent>())
      }
        .Schedule(state.Dependency);
    }
  }

  public partial struct DamageEvent : IBufferElementData
  {
    public float3 DamagePosition;
    public float DamageAmount;
  }
}