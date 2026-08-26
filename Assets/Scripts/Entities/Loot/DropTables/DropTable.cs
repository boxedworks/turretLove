using System;

namespace Assets.Scripts.Entities.Loot.DropTables
{
  [Serializable]
  public struct DropTable
  {
    public float RollChance;
    public DropEntry[] Entries;
  }
}
