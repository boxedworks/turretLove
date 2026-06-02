using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Game
{
  public class LevelSpawnerAuthoring : MonoBehaviour
  {
    public GameObject Border0Prefab, Border1Prefab;
  }

  public class LevelSpawnerBaker : Baker<LevelSpawnerAuthoring>
  {
    public override void Bake(LevelSpawnerAuthoring authoring)
    {
      var entity = GetEntity(TransformUsageFlags.None);
      var levelSpawner = new LevelSpawner
      {
        Border0Prefab = GetEntity(authoring.Border0Prefab, TransformUsageFlags.None),
        Border1Prefab = GetEntity(authoring.Border1Prefab, TransformUsageFlags.None)
      };

      AddComponent(entity, levelSpawner);
    }

  }

  public struct LevelSpawner : IComponentData
  {
    public Entity Border0Prefab;
    public Entity Border1Prefab;
  }
}
