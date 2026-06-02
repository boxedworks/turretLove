
using Assets.Scripts.Entities.Game.Audio;
using Assets.Scripts.Entities.Player.Turret;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Enemy
{
  public partial struct EnemyDamageSystem : ISystem
  {
    [BurstCompile]
    partial struct EnemyDamageJob : IJobEntity
    {
      public EntityCommandBuffer Ecb;
      public DynamicBuffer<AudioEvent> AudioEventBuffer;

      public readonly void Execute(Entity entity, ref DynamicBuffer<BulletCollisionEvent> collisionBuffer, ref PhysicsVelocity velocity, ref PhysicsMass mass, in LocalTransform transform)
      {
        if (collisionBuffer.Length > 0)
        {
          // Handle collision events
          foreach (var collisionEvent in collisionBuffer)
          {
            var position = transform.Position;
            var bulletPosition = collisionEvent.BulletPosition;
            var dir = position - bulletPosition;
            dir.z = 0f;
            var force = 5f;
            velocity.ApplyLinearImpulse(mass, math.normalize(dir) * force);
          }
          collisionBuffer.Clear();

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
      state.Dependency = new EnemyDamageJob()
      {
        Ecb = ecb,
        AudioEventBuffer = SystemAPI.GetBuffer<AudioEvent>(SystemAPI.GetSingletonEntity<AudioEvent>())
      }
        .Schedule(state.Dependency);
    }
  }
}