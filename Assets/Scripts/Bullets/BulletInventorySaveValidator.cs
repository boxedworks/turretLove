using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Entities.Loot;
using Assets.Scripts.Entities.Player.Turret;
using UnityEngine;

namespace Assets.Scripts.Bullets
{
  internal static class BulletInventorySaveValidator
  {
    public static void Validate(SaveData save)
    {
      save.Resources ??= new List<ResourceAmountSave>();
      save.CraftedBullets ??= new List<CraftedBulletSave>();
      save.EquippedBulletIds ??= new List<string>();
      if (save.Version != BulletInventoryService.CurrentSaveVersion)
        throw new InvalidDataException($"Save version {save.Version} is unsupported.");

      ValidateResources(save.Resources);
      ValidateCraftedBullets(save.CraftedBullets);
      ValidateEquippedBullets(save.EquippedBulletIds, save.CraftedBullets);
    }

    private static void ValidateResources(List<ResourceAmountSave> resources)
    {
      for (var index = resources.Count - 1; index >= 0; index--)
      {
        var resource = resources[index];
        if (resource == null || resource.Type == LootType.None)
          resources.RemoveAt(index);
        else
          resource.Amount = Math.Max(0, resource.Amount);
      }

      foreach (LootType type in Enum.GetValues(typeof(LootType)))
      {
        if (type != LootType.None && FindResource(resources, type) == null)
          resources.Add(new ResourceAmountSave { Type = type, Amount = 0 });
      }
    }

    private static void ValidateCraftedBullets(List<CraftedBulletSave> craftedBullets)
    {
      var uniqueIds = new HashSet<string>();
      for (var index = craftedBullets.Count - 1; index >= 0; index--)
      {
        var crafted = craftedBullets[index];
        if (crafted == null || string.IsNullOrWhiteSpace(crafted.Id) || !uniqueIds.Add(crafted.Id)
          || BulletCatalog.FindDefinition(crafted.DefinitionId) == null)
        {
          craftedBullets.RemoveAt(index);
          continue;
        }

        crafted.Modifiers ??= new List<CraftedBulletModifierSave>();
        crafted.SpentItems ??= new List<ItemSpendSave>();
        var installed = new HashSet<string>();
        for (var modifierIndex = crafted.Modifiers.Count - 1; modifierIndex >= 0; modifierIndex--)
        {
          var modifier = crafted.Modifiers[modifierIndex];
          var definition = modifier == null ? null : BulletCatalog.FindModifier(modifier.ModifierId);
          if (definition == null || !installed.Add(definition.Id))
          {
            crafted.Modifiers.RemoveAt(modifierIndex);
            continue;
          }

          modifier.RollTiming = definition.RollTiming;
          modifier.RolledValue = definition.RollTiming == ModifierRollTiming.OnCrafting
            ? Mathf.Clamp(modifier.RolledValue, definition.MinimumValue, definition.MaximumValue)
            : 0f;
        }
      }
    }

    private static void ValidateEquippedBullets(List<string> equippedBulletIds, List<CraftedBulletSave> craftedBullets)
    {
      while (equippedBulletIds.Count < BulletInventoryService.EquippedSlotCount)
        equippedBulletIds.Add(string.Empty);
      if (equippedBulletIds.Count > BulletInventoryService.EquippedSlotCount)
        equippedBulletIds.RemoveRange(
          BulletInventoryService.EquippedSlotCount,
          equippedBulletIds.Count - BulletInventoryService.EquippedSlotCount);

      var equipped = new HashSet<string>();
      for (var index = 0; index < equippedBulletIds.Count; index++)
      {
        var bulletId = equippedBulletIds[index];
        if (FindCraftedBullet(craftedBullets, bulletId) == null || !equipped.Add(bulletId))
          equippedBulletIds[index] = string.Empty;
      }
    }

    private static ResourceAmountSave FindResource(List<ResourceAmountSave> resources, LootType type)
    {
      foreach (var resource in resources)
        if (resource.Type == type)
          return resource;
      return null;
    }

    private static CraftedBulletSave FindCraftedBullet(List<CraftedBulletSave> craftedBullets, string bulletId)
    {
      if (string.IsNullOrEmpty(bulletId))
        return null;
      foreach (var crafted in craftedBullets)
        if (crafted.Id == bulletId)
          return crafted;
      return null;
    }
  }
}
