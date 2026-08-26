
using Unity.Entities;

namespace Assets.Scripts.Entities.Enemy
{
  public enum EnemyType
  {
    None,

    Goblin,
    Ghost,

    Tree
  }

  public partial struct SimpleEnemy : IComponentData
  {
    public EnemyType Type;

    public float Health;
  }
}