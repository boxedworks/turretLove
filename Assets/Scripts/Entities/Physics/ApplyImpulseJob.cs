using Assets.Scripts.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
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
        .WithAll<CubeTest>()
        .Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.CompleteDependency();

      // Gather input state
      var inputState = SystemAPI.GetSingleton<InputState>();

      // Iterate through all entities with physics components
      var entities = _physicsQuery.ToEntityArray(Allocator.Temp);
      foreach (var entity in entities)
      {
        var velocity = SystemAPI.GetComponentRW<PhysicsVelocity>(entity);
        var mass = SystemAPI.GetComponentRW<PhysicsMass>(entity);
        var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);

        // Apply an impulse to the entity's velocity
        if (inputState.Mouse1Down)
        {
          var position = transform.ValueRO.Position;
          var dir = inputState.MouseWorldPosition - position;
          dir.z = 0f;
          var force = math.clamp(math.length(dir), 0f, 1f) * 0.25f;
          var impulse = math.normalize(dir) * force;
          velocity.ValueRW.Linear += impulse / mass.ValueRO.InverseMass;
        }

        transform.ValueRW.Position.z = 0f;
        velocity.ValueRW.Linear.z = 0f;
        mass.ValueRW.InverseInertia.x = 0f;
        mass.ValueRW.InverseInertia.y = 0f;
      }
      entities.Dispose();
    }
  }


}