
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Input
{
  public partial struct InputState : IComponentData
  {
    public float3 MouseWorldPosition;
    public bool Mouse1Down;
    public bool ArrowUpDown;
    public bool ArrowDownDown;
    public bool ArrowLeftDown;
    public bool ArrowRightDown;
  }
}