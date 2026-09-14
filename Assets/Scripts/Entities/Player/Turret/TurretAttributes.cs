
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct TurretAttributes : IComponentData
  {
    public Entity TurretTopEntity;
    public Entity CurrentTarget;

    public float RotationSpeed;
    public float FireRate;
    public double TimeSinceLastShot;
  }

  public struct TurretAmmo : IComponentData
  {
    public int CurrentAmmo;
    public int MagazineSize;
    public int CurrentSlotIndex;
    public int LoadoutRevision;
  }
}