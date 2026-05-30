
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct TurretSystem : ISystem
  {

    public void OnCreate(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
      var deltaTime = SystemAPI.Time.DeltaTime;


    }

  }
}