

using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Assets.Scripts.Entities.Game
{
  public partial struct EffectSystem : ISystem
  {

    public void OnUpdate(ref SystemState state)
    {

    }

  }

  //
  [MaterialProperty("_Color")]
  public struct ColorOverride : IComponentData
  {
    public float4 Value;
  }
}