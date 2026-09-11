
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct TurretAttributes : IComponentData
  {
    public Entity TurretTopEntity;

    public float RotationSpeed;
    public float FireRate;
    public double TimeSinceLastShot;
  }
}