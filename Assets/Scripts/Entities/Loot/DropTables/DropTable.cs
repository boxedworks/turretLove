using System;

namespace Assets.Scripts.Entities.Loot.DropTables
{
  [Serializable]
  public struct DropTable
  {
    public int Rolls;
    public DropEntry[] Entries;
  }
}
