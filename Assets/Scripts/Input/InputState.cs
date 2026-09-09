
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Input
{
  public enum InputButtonState : byte
  {
    None,
    Pressed,
    Held,
    Released
  }

  public partial struct InputState : IComponentData
  {
    public float3 MouseWorldPosition;
    public bool Mouse1Down;
    public InputButtonState ArrowUpState;
    public InputButtonState ArrowDownState;
    public InputButtonState ArrowLeftState;
    public InputButtonState ArrowRightState;
    public float2 ArrowReleaseDirection;
  }
}