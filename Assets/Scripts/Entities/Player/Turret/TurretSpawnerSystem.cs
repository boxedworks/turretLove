using Assets.Scripts.Entities.Game;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Physics;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct TurretSpawnEvent : IBufferElementData
  {
    public float3 SpawnPosition;
  }

  public partial struct TurretTop : IComponentData
  {
  }
  public partial struct TurretBase : IComponentData
  {
  }
}