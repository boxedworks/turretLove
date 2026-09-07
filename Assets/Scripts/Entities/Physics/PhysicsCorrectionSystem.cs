using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Physics
{

  // After the PhysicsInitialzeGroup has finished, PhysicsWorld will be created.
  [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
  [UpdateBefore(typeof(PhysicsSystemGroup))]
  public partial struct PhysicsCorrectionSystem : ISystem
  {
    [BurstCompile]
    partial struct ConstrainPhysicsJob : IJobEntity
    {
      public readonly void Execute(ref PhysicsVelocity velocity, ref PhysicsMass mass, ref LocalTransform transform)
      {
        transform.Position.z = 0f;
        velocity.Linear.z = 0f;
        mass.InverseInertia = new float3(0f, 0f, mass.InverseInertia.z);
      }
    }

    [BurstCompile]
    public readonly void OnUpdate(ref SystemState state)
    {
      state.Dependency = new ConstrainPhysicsJob()
        .ScheduleParallel(state.Dependency);
    }
  }

}