using System;
using Assets.Scripts.Entities.Enemy;

namespace Assets.Scripts.Entities.Loot.DropTables
{
  [Serializable]
  public struct EnemyDropTable
  {
    public EnemyType EnemyType;
    public DropTable[] DropTables;
  }
}
