
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Loot
{
  public class LootSpawnerAuthoring : MonoBehaviour
  {
    public GameObject ManaPrefab;
    public GameObject WoodPrefab;
    public GameObject StonePrefab;
    public GameObject EmeraldPrefab;
    public GameObject SapphirePrefab;
    public GameObject RubyPrefab;
    public GameObject DiamondPrefab;
  }

  public class LootSpawnerBaker : Baker<LootSpawnerAuthoring>
  {
    public override void Bake(LootSpawnerAuthoring authoring)
    {
      var entity = GetEntity(TransformUsageFlags.None);
      AddComponent(entity, new LootSpawner
      {
        ManaPrefab = GetEntity(authoring.ManaPrefab, TransformUsageFlags.Dynamic),
        WoodPrefab = GetEntity(authoring.WoodPrefab, TransformUsageFlags.Dynamic),
        StonePrefab = GetEntity(authoring.StonePrefab, TransformUsageFlags.Dynamic),
        EmeraldPrefab = GetEntity(authoring.EmeraldPrefab, TransformUsageFlags.Dynamic),
        SapphirePrefab = GetEntity(authoring.SapphirePrefab, TransformUsageFlags.Dynamic),
        RubyPrefab = GetEntity(authoring.RubyPrefab, TransformUsageFlags.Dynamic),
        DiamondPrefab = GetEntity(authoring.DiamondPrefab, TransformUsageFlags.Dynamic)
      });
    }
  }

  public struct LootSpawner : IComponentData
  {
    public Entity ManaPrefab;
    public Entity WoodPrefab;
    public Entity StonePrefab;
    public Entity EmeraldPrefab;
    public Entity SapphirePrefab;
    public Entity RubyPrefab;
    public Entity DiamondPrefab;
  }
}