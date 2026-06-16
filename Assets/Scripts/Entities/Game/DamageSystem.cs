
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

      public readonly void Execute(Entity entity, ref DynamicBuffer<DamageEvent> damageBuffer, ref PhysicsVelocity velocity, ref PhysicsMass mass, in LocalTransform transform)
      {
        if (damageBuffer.Length > 0)
        {
          // Handle collision events
          foreach (var damageEvent in damageBuffer)
          {

          }
          damageBuffer.Clear();

          // Destroy entity using ecb
          Ecb.DestroyEntity(entity);

          // Add audio event for enemy death
          AudioEventBuffer.Add(new AudioEvent { Type = AudioEvent.EventType.EnemyDeath });
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
    public float3 BulletPosition;
  }
}