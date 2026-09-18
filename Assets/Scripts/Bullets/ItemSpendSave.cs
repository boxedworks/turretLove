using System;
using Assets.Scripts.Entities.Loot;

namespace Assets.Scripts.Bullets
{
  [Serializable]
  public sealed class ItemSpendSave
  {
    public LootType Type;
    public int Amount;
  }
}
