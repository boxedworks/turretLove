using Assets.Scripts.Entities.Enemy;
using Assets.Scripts.Input;
using Unity.Burst;
using Unity.Collections;
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
  public partial struct ApplyImpulseSystem : ISystem
  {

    EntityQuery _physicsQuery;

    public void OnCreate(ref SystemState state)
    {
      _physicsQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<SimpleEnemy>()
        .Build(ref state);
    }

    [BurstCompile]
    partial struct ApplyImpulseJob : IJobEntity
    {
      public InputState InputState;

      public readonly void Execute(in SimpleEnemy enemy, ref PhysicsVelocity velocity, ref PhysicsMass mass, in LocalTransform transform)
      {
        if (InputState.Mouse1Down)
        {
          var position = transform.Position;
          var dir = InputState.MouseWorldPosition - position;
          dir.z = 0f;
          var force = math.clamp(math.length(dir), 0f, 1f) * 0.25f;
          var impulse = math.normalize(dir) * force;
          velocity.Linear += impulse / mass.InverseMass;
        }

        // Constrain movement to the XY plane
        velocity.Linear.z = 0f;
        mass.InverseInertia = float3.zero;
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.CompleteDependency();

      // Gather input state
      var inputState = SystemAPI.GetSingleton<InputState>();

      // Schedule the job to apply impulses based on input
      state.Dependency = new ApplyImpulseJob
      {
        InputState = inputState
      }
        .ScheduleParallel(state.Dependency);
    }
  }

}