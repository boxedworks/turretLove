using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Game
{
  public partial struct DamageEvent : IBufferElementData
  {
    public float3 DamagePosition;
    public float DamageAmount;
  }
}
