
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

  // Tracks when an enemy is next allowed to deal contact damage, to debounce collision events spanning multiple frames.
  public struct ContactCooldown : IComponentData
  {
    public double NextAllowedContactTime;
  }
}