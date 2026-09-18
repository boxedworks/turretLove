using System;
using System.Collections.Generic;

namespace Assets.Scripts.Bullets
{
  [Serializable]
  public sealed class CraftedBulletSave
  {
    public string Id;
    public string DefinitionId;
    public List<CraftedBulletModifierSave> Modifiers = new();
    public List<ItemSpendSave> SpentItems = new();
  }
}
