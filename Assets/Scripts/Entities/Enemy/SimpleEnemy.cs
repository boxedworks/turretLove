
using Unity.Entities;

namespace Assets.Scripts.Entities.Enemy
{
  public partial struct SimpleEnemy : IComponentData
  {
    public float Health;

    public float Speed;
  }
}