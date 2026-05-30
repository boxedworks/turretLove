
using Assets.Scripts.Entities.Player.Turret;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Enemy
{
  public partial struct EnemyDamageSystem : ISystem
  {

    EntityQuery _enemyQuery;
    public void OnCreate(ref SystemState state)
    {
      _enemyQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<BulletCollisionEvent>()
        .Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.CompleteDependency();

      var entities = _enemyQuery.ToEntityArray(Allocator.Temp);
      foreach (var entity in entities)
      {
        var collisionBuffer = SystemAPI.GetBuffer<BulletCollisionEvent>(entity);

        if (collisionBuffer.Length > 0)
        {
          // Handle collision events
          foreach (var collisionEvent in collisionBuffer)
          {
            var velocity = SystemAPI.GetComponentRW<PhysicsVelocity>(entity);
            var physicsMass = SystemAPI.GetComponentRW<PhysicsMass>(entity);
            var position = SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRO.Position;
            var bulletPosition = collisionEvent.BulletPosition;
            var dir = position - bulletPosition;
            dir.z = 0f;
            var force = 5f;
            velocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, math.normalize(dir) * force);
          }
          collisionBuffer.Clear();
        }
      }
      entities.Dispose();
    }
  }
}