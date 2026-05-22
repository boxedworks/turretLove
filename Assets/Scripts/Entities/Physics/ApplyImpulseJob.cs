using Assets.Scripts.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

    EntityQuery m_PhysicsQuery;

    public void OnCreate(ref SystemState state)
    {
      m_PhysicsQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<CubeTest>()
        .Build(ref state);
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.CompleteDependency();

      // Iterate through all entities with physics components
      var entities = m_PhysicsQuery.ToEntityArray(Allocator.Temp);
      var mousePosition = InputController.s_MouseWorldPosition;
      var isMouse1Down = InputController.s_IsMouse1Down;
      foreach (var entity in entities)
      {
        var velocity = SystemAPI.GetComponentRW<PhysicsVelocity>(entity);
        var mass = SystemAPI.GetComponentRW<PhysicsMass>(entity);
        var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);

        // Apply an impulse to the entity's velocity
        if (isMouse1Down)
        {
          var position = transform.ValueRO.Position;
          var dir = new float3(mousePosition.x, mousePosition.y, position.z) - position;
          dir.z = 0f;
          var force = math.clamp(math.length(dir), 0f, 1f) * 0.25f;
          var impulse = math.normalize(dir) * force;
          velocity.ValueRW.ApplyLinearImpulse(mass.ValueRO, impulse);
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