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
        starter.CraftedBullets.Add(new CraftedBulletSave
        {
          Id = $"starter-{index + 1}-bullet",
          DefinitionId = BulletCatalog.Definitions[index].Id,
          Modifiers = new List<CraftedBulletModifierSave>(),
          SpentItems = new List<ItemSpendSave>()
        });
        starter.EquippedBulletIds.Add($"starter-{index + 1}-bullet");
      }
      return starter;
    }
  }
}
