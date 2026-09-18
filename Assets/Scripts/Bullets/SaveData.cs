using System;
using System.Collections.Generic;

namespace Assets.Scripts.Bullets
{
  [Serializable]
  public sealed class SaveData
  {
    public int Version;
    public List<ResourceAmountSave> Resources = new();
    public List<CraftedBulletSave> CraftedBullets = new();
    public List<string> EquippedBulletIds = new();
  }
}
