
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Player.Turret
{
  public class TurretSpawnerAuthoring : MonoBehaviour
  {
    public GameObject TurretTopPrefab, TurretBasePrefab;
  }

  public class TurretSpawnerBaker : Baker<TurretSpawnerAuthoring>
  {
    public override void Bake(TurretSpawnerAuthoring authoring)
    {
      var entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new TurretSpawner
      {
        TurretTopPrefab = GetEntity(authoring.TurretTopPrefab, TransformUsageFlags.Dynamic),
        TurretBasePrefab = GetEntity(authoring.TurretBasePrefab, TransformUsageFlags.Dynamic)
      });
    }
  }

  public struct TurretSpawner : IComponentData
  {
    public Entity TurretTopPrefab;
    public Entity TurretBasePrefab;
  }
}