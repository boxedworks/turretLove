
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;

namespace Assets.Scripts.Entities.Game
{
  public partial struct PhysicsEventsSystem : ISystem
  {

    [BurstCompile]
    public partial struct KnockbackJob : IJobEntity
    {

      public readonly void Execute(Entity entity, ref DynamicBuffer<KnockbackEvent> knockbackBuffer, ref PhysicsVelocity velocity, in PhysicsMass mass)
      {
        if (knockbackBuffer.Length > 0)
        {
          foreach (var knockbackEvent in knockbackBuffer)
            velocity.ApplyLinearImpulse(mass, knockbackEvent.Direction * knockbackEvent.Force);

          knockbackBuffer.Clear();
        }
      }
    }

    [BurstCompile]
    public readonly void OnUpdate(ref SystemState state)
    {
      // Gather knockback events and schedule the job
      state.Dependency = new KnockbackJob()
        .ScheduleParallel(state.Dependency);
    }
  }

  public partial struct KnockbackEvent : IBufferElementData
  {
    public float3 Direction;
    public float Force;
  }
}