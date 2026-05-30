
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Turret
{
  public class BulletSpawnerAuthoring : UnityEngine.MonoBehaviour
  {
    public UnityEngine.GameObject BulletPrefab;
  }

  class BulletSpawnerBaker : Baker<BulletSpawnerAuthoring>
  {
    public override void Bake(BulletSpawnerAuthoring authoring)
    {
      var entity = GetEntity(TransformUsageFlags.None);
      AddComponent(entity, new BulletSpawner
      {
        Prefab = GetEntity(authoring.BulletPrefab, TransformUsageFlags.Dynamic)
      });
    }
  }

  public partial struct BulletSpawner : IComponentData
  {
    public Entity Prefab;
  }
}