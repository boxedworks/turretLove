using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Loot
{
  public struct LootSpawnData : IComponentData
  {
    public float3 SpawnPosition;
    public float SpawnTime, SpawnArcHeight, SpawnArcDuration, SpawnArcDirection, SpawnArcDirectionY;
  }
}
