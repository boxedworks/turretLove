using System;

namespace Assets.Scripts.Entities.Loot.DropTables
{
  [Serializable]
  public struct DropEntry
  {
    public LootType Type;
    public float DropChance; // 0-100
    public int MinAmount;
    public int MaxAmount;
  }
}
