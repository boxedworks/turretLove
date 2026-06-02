using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

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

      state.EntityManager.AddComponent<TurretTop>(turretTopInstance);
      state.EntityManager.AddComponent<TurretBase>(turretBaseInstance);

      // Set initial position of turret (for now, just place it at origin)
      var turretPosition = float3.zero;
      state.EntityManager.SetComponentData(turretTopInstance, LocalTransform.FromPosition(turretPosition + new float3(0, 0, -0.1f)));
      state.EntityManager.SetComponentData(turretBaseInstance, LocalTransform.FromPosition(turretPosition));
    }
  }

  public partial struct TurretTop : IComponentData
  {
  }
  public partial struct TurretBase : IComponentData
  {
  }
}