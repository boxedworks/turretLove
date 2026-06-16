
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Enemy
{
  public class EnemySpawnerAuthoring : MonoBehaviour
  {
    public float SpawnInterval = 1f;

    public GameObject GoblinPrefab;
  }

  public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
  {
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
      var entity = GetEntity(TransformUsageFlags.Dynamic);
      AddComponent(entity, new EnemySpawner
      {
        SpawnInterval = authoring.SpawnInterval,
        GoblinPrefab = GetEntity(authoring.GoblinPrefab, TransformUsageFlags.Dynamic)
      });
    }
  }

  public struct EnemySpawner : IComponentData
  {
    public float SpawnInterval;
    public Entity GoblinPrefab;
  }
}