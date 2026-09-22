using System.Collections.Generic;
using Assets.Scripts.Entities.Loot;
using Assets.Scripts.Entities.Player.Turret;

namespace Assets.Scripts.Bullets
{
  internal static class BulletInventorySaveFactory
  {
    public static SaveData CreateStarterData()
    {
      var starter = new SaveData { Version = BulletInventoryService.CurrentSaveVersion };
      foreach (LootType type in System.Enum.GetValues(typeof(LootType)))
      {
        if (type != LootType.None)
          starter.Resources.Add(new ResourceAmountSave { Type = type, Amount = 0 });
      }

      for (var index = 0; index < BulletCatalog.Definitions.Length; index++)
      {
        var starterBullet = BulletInventoryService.CreateBulletBase(
          BulletInventoryService.MinimumBulletLevel,
          index);
        starterBullet.Id = $"starter-{index + 1}-bullet";
        starter.CraftedBullets.Add(starterBullet);
        starter.EquippedBulletIds.Add($"starter-{index + 1}-bullet");
      }
      return starter;
    }
  }
}
