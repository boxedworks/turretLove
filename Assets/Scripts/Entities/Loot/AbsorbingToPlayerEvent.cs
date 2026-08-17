using Unity.Entities;

namespace Assets.Scripts.Entities.Loot
{
  public struct AbsorbingToPlayerEvent : IComponentData
  {
    public float StartTime;
  }
}
