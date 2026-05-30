
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct Turret : IComponentData
  {
    public float RotationSpeed;
    public float FireRate;
    public float TimeSinceLastShot;
  }
}