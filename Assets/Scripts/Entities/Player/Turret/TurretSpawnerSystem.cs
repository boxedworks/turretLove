using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Physics;

namespace Assets.Scripts.Entities.Player.Turret
{

  public partial struct TurretSpawnerSystem : ISystem
  {

    bool _hasSpawnedTurret;

    public readonly void OnCreate(ref SystemState state)
    {
      // Require the singleton TurretSpawner component to exist for this system to run
      state.RequireForUpdate<TurretSpawner>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      if (_hasSpawnedTurret)
        return;
      _hasSpawnedTurret = true;

      // For now, just spawn one turret at start of game
      var turretSpawnerEntity = SystemAPI.GetSingletonEntity<TurretSpawner>();
      var turretSpawner = SystemAPI.GetComponent<TurretSpawner>(turretSpawnerEntity);

      var turretTopInstance = state.EntityManager.Instantiate(turretSpawner.TurretTopPrefab);
      var turretBaseInstance = state.EntityManager.Instantiate(turretSpawner.TurretBasePrefab);

      state.EntityManager.AddComponentData(turretTopInstance, new TurretTop()
      {
        RotationSpeed = 1f,
        FireRate = 0.5f,
      });
      state.EntityManager.AddComponent<TurretBase>(turretBaseInstance);

      // Set initial position of turret (for now, just place it at origin)
      var turretPosition = float3.zero;
      state.EntityManager.SetComponentData(turretTopInstance, LocalTransform.FromPosition(turretPosition + new float3(0, 0, -0.1f)));
      state.EntityManager.SetComponentData(turretBaseInstance, LocalTransform.FromPosition(turretPosition));

      // Change collision layer to avoid colliding with the bullet
      var physicsCollider = SystemAPI.GetComponentRW<PhysicsCollider>(turretBaseInstance);
      var collider = physicsCollider.ValueRO.Value;
      var filter = collider.Value.GetCollisionFilter();
      filter.BelongsTo = 1 << 1;
      filter.CollidesWith = (1 << 0) | (1 << 4);
      collider.Value.SetCollisionFilter(filter);
      physicsCollider.ValueRW.Value = collider;
    }
  }

  public partial struct TurretTop : IComponentData
  {
    public float RotationSpeed;
    public double LastShotTime;
    public float FireRate;
  }
  public partial struct TurretBase : IComponentData
  {
  }
}