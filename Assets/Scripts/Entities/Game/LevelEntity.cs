using Unity.Entities;

namespace Assets.Scripts.Entities.Game
{
  /// <summary>
  /// Marks entities that should be cleaned up at the end of a level and identifies their gameplay role.
  /// </summary>
  public enum LevelEntityType : byte
  {
    Unknown,
    Player,
    Turret,
    Enemy,
    Loot
  }

  public struct LevelEntity : IComponentData
  {
    public LevelEntityType Type;
  }
}
