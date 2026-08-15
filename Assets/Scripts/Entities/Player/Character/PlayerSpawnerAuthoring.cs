using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Player.Character
{
  public class PlayerSpawnerAuthoring : MonoBehaviour
  {
    public GameObject PlayerPrefab;
  }

  public class PlayerSpawnerBaker : Baker<PlayerSpawnerAuthoring>
  {
    public override void Bake(PlayerSpawnerAuthoring authoring)
    {
      var entity = GetEntity(TransformUsageFlags.None);
      AddComponent(entity, new PlayerSpawner
      {
        PlayerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic)
      });
    }
  }

  public struct PlayerSpawner : IComponentData
  {
    public Entity PlayerPrefab;
  }
}
