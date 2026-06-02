
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{

  public partial struct BulletSpawnerSystem : ISystem
  {

    double _lastSpawnTime;

    public readonly void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
      var time = SystemAPI.Time.ElapsedTime;
      if (time - _lastSpawnTime < 0.25f)
        return;
      _lastSpawnTime = time;

      var bulletSpawner = SystemAPI.GetSingleton<BulletSpawner>();

      var bulletEntity = state.EntityManager.Instantiate(bulletSpawner.Prefab);
      state.EntityManager.AddComponent<Bullet>(bulletEntity);
      state.EntityManager.SetComponentData(bulletEntity, LocalTransform.FromPosition(float3.zero));

      // For testing, set random rotation and velocity
      var random = new Random((uint)System.DateTime.Now.Ticks);
      var spawnPosition = float3.zero;
      var rotation = quaternion.EulerXYZ(0, 0, random.NextFloat(0, math.PI * 2));
      var scale = 0.25f;

      // Set position, rotation and scale
      state.EntityManager.SetComponentData(bulletEntity, LocalTransform.FromPositionRotationScale(spawnPosition, rotation, scale));

      var velocity = SystemAPI.GetComponentRW<PhysicsVelocity>(bulletEntity);
      var force = 5f;
      velocity.ValueRW.Linear = math.mul(rotation, new float3(0, 1f, 0)) * force;
    }

  }

  public partial struct Bullet : IComponentData
  {
  }
  public partial struct BulletCollisionEvent : IBufferElementData
  {
    public float3 BulletPosition;
  }
}