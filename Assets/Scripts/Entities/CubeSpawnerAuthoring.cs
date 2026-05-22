using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities
{

  public class CubeSpawnerAuthoring : MonoBehaviour
  {
    public GameObject CubePrefab;
  }

  class CuberSpawnerBaker : Baker<CubeSpawnerAuthoring>
  {
    public override void Bake(CubeSpawnerAuthoring authoring)
    {
      var entity = GetEntity(TransformUsageFlags.None);
      AddComponent(entity, new CubeSpawner
      {
        Prefab = GetEntity(authoring.CubePrefab, TransformUsageFlags.Dynamic)
      });
    }
  }

  public struct CubeSpawner : IComponentData
  {
    public Entity Prefab;
  }

  public struct CubeTest : IComponentData
  {
  }
}