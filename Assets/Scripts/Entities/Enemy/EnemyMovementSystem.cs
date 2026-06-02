using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Burst;

namespace Assets.Scripts.Entities.Enemy
{


  [UpdateInGroup(typeof(SimulationSystemGroup))]
  public partial struct EnemyMovementSystem : ISystem
  {

    [BurstCompile]
    partial struct EnemyMovementJob : IJobEntity
    {
      public float3 TargetPosition;
      public float DeltaTime;

      public readonly void Execute(ref PhysicsVelocity velocity, ref PhysicsMass mass, in LocalTransform transform, in SimpleEnemy enemy)
      {
        var direction = math.normalize(TargetPosition - transform.Position);
        var speed = enemy.Speed;
        var force = direction * speed * DeltaTime;
        velocity.ApplyLinearImpulse(mass, force);
      }
    }

    [BurstCompile]
    public readonly void OnUpdate(ref SystemState state)
    {
      state.Dependency = new EnemyMovementJob
      {
        TargetPosition = new float3(),
        DeltaTime = SystemAPI.Time.DeltaTime
      }
        .ScheduleParallel(state.Dependency);
    }
  }
}