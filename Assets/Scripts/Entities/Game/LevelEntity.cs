using Unity.Entities;

namespace Assets.Scripts.Entities.Game
{
  /// <summary>
  /// Tag component to mark entities that should be cleaned up at the end of a level.
  /// Applied to: player, turret, enemies, loot, projectiles.
  /// </summary>
  public struct LevelEntity : IComponentData
  {
  }
}
