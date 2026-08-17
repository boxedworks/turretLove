using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Game.Scroll
{
  public struct ScrollComponent : IComponentData
  {
    public float2 Direction;
    public float Speed;
  }
}
