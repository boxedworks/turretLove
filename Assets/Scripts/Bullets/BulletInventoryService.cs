using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Entities.Loot;
using Assets.Scripts.Entities.Player.Turret;
using Unity.Collections;
using UnityEngine;

namespace Assets.Scripts.Bullets
{
  /// <summary>
  /// Owns the selected slot's durable player bullet data. ECS only receives derived, unmanaged magazine payloads.
  /// </summary>
  public sealed partial class BulletInventoryService : MonoBehaviour
  {
    public const int CurrentSaveVersion = 1;
    public const int EquippedSlotCount = 4;
    public const int SlotCount = 3;

    private const string SlotSaveFileNameFormat = "bullet_inventory_slot_{0}.json";
    private const string SettingsFileName = "bullet_inventory_settings.json";
    private static readonly IReadOnlyList<CraftedBulletSave> EmptyCraftedBullets = Array.Empty<CraftedBulletSave>();

    private static BulletInventoryService instance;
    private SaveData data;
    private int activeSlotIndex = -1;
    private int loadoutRevision = 1;

    public static BulletInventoryService Instance
    {
      get
      {
        EnsureInstance();
        return instance;
      }
    }

    public IReadOnlyList<CraftedBulletSave> CraftedBullets => data == null ? EmptyCraftedBullets : data.CraftedBullets;
    public int ActiveSlotIndex => activeSlotIndex;
    public bool HasActiveSave => activeSlotIndex >= 0;
    public int LoadoutRevision => loadoutRevision;
    public string SavePath => HasActiveSave ? GetSaveSlotPath(activeSlotIndex) : null;

    public event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
      EnsureInstance();
    }

    private static void EnsureInstance()
    {
      if (instance != null)
        return;

      instance = FindAnyObjectByType<BulletInventoryService>();
      if (instance != null)
        return;

      var host = new GameObject(nameof(BulletInventoryService));
      instance = host.AddComponent<BulletInventoryService>();
      DontDestroyOnLoad(host);
    }

    private void Awake()
    {
      if (instance != null && instance != this)
      {
        Destroy(gameObject);
        return;
      }

      instance = this;
      DontDestroyOnLoad(gameObject);
      RestoreLastActiveSave();
    }

    public int GetResourceCount(LootType type)
    {
      var resource = data == null ? null : FindResource(type);
      return resource == null ? 0 : resource.Amount;
    }

    public string GetEquippedBulletId(int slotIndex)
    {
      return data == null || slotIndex < 0 || slotIndex >= EquippedSlotCount
        ? null
        : data.EquippedBulletIds[slotIndex];
    }

    public CraftedBulletSave FindCraftedBullet(string bulletId)
    {
      if (data == null || string.IsNullOrWhiteSpace(bulletId))
        return null;

      for (var index = 0; index < data.CraftedBullets.Count; index++)
        if (data.CraftedBullets[index].Id == bulletId)
          return data.CraftedBullets[index];
      return null;
    }

    public bool TryConvertLoot(string recipeId, out string error)
    {
      if (!EnsureActiveSave(out error))
        return false;

      var recipe = BulletCatalog.FindConversionRecipe(recipeId);
      if (recipe == null || !HasMaterials(recipe.Input))
      {
        error = recipe == null ? "Unknown conversion recipe." : "Not enough level loot.";
        return false;
      }

      SpendMaterials(recipe.Input);
      AddMaterials(recipe.Output);
      return PersistAndNotify(false, out error);
    }

    public bool TryApplyCraftingMaterial(string craftedBulletId, LootType resource, out string error)
    {
      if (!EnsureActiveSave(out error))
        return false;

      var crafted = FindCraftedBullet(craftedBulletId);
      var material = BulletCatalog.FindCraftingMaterial(resource);
      if (crafted == null || material == null)
      {
        error = crafted == null ? "Select a valid crafted bullet." : "That inventory item is not a crafting material.";
        return false;
      }

      if (GetResourceCount(resource) < 1)
      {
        error = $"Not enough {material.DisplayName}.";
        return false;
      }
      if (!CanApplyCraftingMaterial(crafted, material, out error))
        return false;

      FindResource(resource).Amount--;
      AddSpentItem(crafted.SpentItems, resource, 1);
      foreach (var effect in material.Effects)
        ApplyCraftingMaterialEffect(crafted, effect);
      return PersistAndNotify(true, out error);
    }

    private static bool CanApplyCraftingMaterial(CraftedBulletSave crafted, CraftingMaterialDefinition material, out string error)
    {
      error = null;
      if (material.Effects == null || material.Effects.Length == 0)
      {
        error = "That crafting material has no effect.";
        return false;
      }

      var installedModifierIds = new List<string>();
      foreach (var installedModifier in crafted.Modifiers)
        installedModifierIds.Add(installedModifier.ModifierId);

      foreach (var effect in material.Effects)
      {
        if (effect == null)
        {
          error = "That crafting material has an invalid effect.";
          return false;
        }
        var count = effect.Count;
        if (count < 1)
        {
          error = "That crafting material has an invalid effect.";
          return false;
        }

        switch (effect.Kind)
        {
          case CraftingMaterialEffectKind.AddRandomModifier:
            var availableCount = 0;
            foreach (var modifier_ in BulletCatalog.Modifiers)
              if (!installedModifierIds.Contains(modifier_.Id))
                availableCount++;
            if (availableCount < count)
            {
              error = "There are not enough uninstalled modifiers for that crafting material.";
              return false;
            }
            for (var index = 0; index < count; index++)
            {
              foreach (var modifier_ in BulletCatalog.Modifiers)
              {
                if (!installedModifierIds.Contains(modifier_.Id))
                {
                  installedModifierIds.Add(modifier_.Id);
                  break;
                }
              }
            }
            break;

          case CraftingMaterialEffectKind.AddSpecificModifier:
            var modifier = BulletCatalog.FindModifier(effect.ModifierId);
            if (modifier == null)
            {
              error = "That crafting material references an unknown modifier.";
              return false;
            }
            if (count != 1 || installedModifierIds.Contains(modifier.Id))
            {
              error = count != 1 ? "A specific modifier effect must add exactly one modifier." : "That modifier is already installed.";
              return false;
            }
            installedModifierIds.Add(modifier.Id);
            break;

          case CraftingMaterialEffectKind.RerollRandomModifier:
            var rerollableCount = 0;
            foreach (var modifierId in installedModifierIds)
            {
              var installedModifier = BulletCatalog.FindModifier(modifierId);
              if (installedModifier != null && installedModifier.RollTiming == ModifierRollTiming.OnCrafting)
                rerollableCount++;
            }
            if (rerollableCount < count)
            {
              error = "That bullet has no permanent modifier value to reroll.";
              return false;
            }
            break;

          case CraftingMaterialEffectKind.RemoveRandomModifier:
            if (installedModifierIds.Count < count)
            {
              error = "That bullet has no modifier to remove.";
              return false;
            }
            installedModifierIds.RemoveRange(0, count);
            break;

          default:
            error = "That crafting material has an unknown effect.";
            return false;
        }
      }
      return true;
    }

    private static void ApplyCraftingMaterialEffect(CraftedBulletSave crafted, CraftingMaterialEffectDefinition effect)
    {
      switch (effect.Kind)
      {
        case CraftingMaterialEffectKind.AddRandomModifier:
          for (var index = 0; index < effect.Count; index++)
          {
            var availableModifiers = GetUninstalledModifiers(crafted);
            AddModifier(crafted, availableModifiers[UnityEngine.Random.Range(0, availableModifiers.Count)]);
          }
          break;

        case CraftingMaterialEffectKind.AddSpecificModifier:
          AddModifier(crafted, BulletCatalog.FindModifier(effect.ModifierId));
          break;

        case CraftingMaterialEffectKind.RerollRandomModifier:
          for (var index = 0; index < effect.Count; index++)
          {
            var rerollableModifiers = GetRerollableModifiers(crafted);
            var modifier = rerollableModifiers[UnityEngine.Random.Range(0, rerollableModifiers.Count)];
            foreach (var savedModifier in crafted.Modifiers)
            {
              if (savedModifier.ModifierId == modifier.Id)
              {
                savedModifier.RolledValue = UnityEngine.Random.Range(modifier.MinimumValue, modifier.MaximumValue);
                break;
              }
            }
          }
          break;

        case CraftingMaterialEffectKind.RemoveRandomModifier:
          for (var index = 0; index < effect.Count; index++)
            crafted.Modifiers.RemoveAt(UnityEngine.Random.Range(0, crafted.Modifiers.Count));
          break;
      }
    }

    private static List<BulletModifierDefinition> GetUninstalledModifiers(CraftedBulletSave crafted)
    {
      var available = new List<BulletModifierDefinition>();
      foreach (var modifier in BulletCatalog.Modifiers)
      {
        var isInstalled = false;
        foreach (var savedModifier in crafted.Modifiers)
        {
          if (savedModifier.ModifierId == modifier.Id)
          {
            isInstalled = true;
            break;
          }
        }
        if (!isInstalled)
          available.Add(modifier);
      }
      return available;
    }

    private static List<BulletModifierDefinition> GetRerollableModifiers(CraftedBulletSave crafted)
    {
      var rerollable = new List<BulletModifierDefinition>();
      foreach (var savedModifier in crafted.Modifiers)
      {
        var modifier = BulletCatalog.FindModifier(savedModifier.ModifierId);
        if (modifier != null && modifier.RollTiming == ModifierRollTiming.OnCrafting)
          rerollable.Add(modifier);
      }
      return rerollable;
    }

    private static void AddModifier(CraftedBulletSave crafted, BulletModifierDefinition modifier)
    {
      crafted.Modifiers.Add(new CraftedBulletModifierSave
      {
        ModifierId = modifier.Id,
        RollTiming = modifier.RollTiming,
        RolledValue = modifier.RollTiming == ModifierRollTiming.OnCrafting
          ? UnityEngine.Random.Range(modifier.MinimumValue, modifier.MaximumValue)
          : 0f
      });
    }

    public bool TryMoveEquippedBullet(int targetSlotIndex, string craftedBulletId, out string error)
    {
      if (!EnsureActiveSave(out error))
        return false;
      if (targetSlotIndex < 0 || targetSlotIndex >= EquippedSlotCount || FindCraftedBullet(craftedBulletId) == null)
      {
        error = "Drop a crafted bullet onto a valid magazine slot.";
        return false;
      }

      var sourceSlotIndex = data.EquippedBulletIds.IndexOf(craftedBulletId);
      var targetBulletId = data.EquippedBulletIds[targetSlotIndex];
      data.EquippedBulletIds[targetSlotIndex] = craftedBulletId;
      if (sourceSlotIndex >= 0 && sourceSlotIndex != targetSlotIndex)
        data.EquippedBulletIds[sourceSlotIndex] = targetBulletId;
      return PersistAndNotify(true, out error);
    }

    public bool TrySalvage(string craftedBulletId, out string error)
    {
      if (!EnsureActiveSave(out error))
        return false;
      var crafted = FindCraftedBullet(craftedBulletId);
      if (crafted == null || data.EquippedBulletIds.Contains(craftedBulletId))
      {
        error = crafted == null ? "Select a valid crafted bullet." : "Unequip this bullet before salvaging it.";
        return false;
      }

      foreach (var spend in crafted.SpentItems)
        FindResource(spend.Type).Amount += spend.Amount / 2;
      data.CraftedBullets.Remove(crafted);
      return PersistAndNotify(true, out error);
    }

    public bool TryEquip(int slotIndex, string craftedBulletId, out string error)
    {
      if (!EnsureActiveSave(out error))
        return false;
      if (slotIndex < 0 || slotIndex >= EquippedSlotCount || FindCraftedBullet(craftedBulletId) == null)
      {
        error = "Select a crafted bullet and a valid equipment slot.";
        return false;
      }

      for (var index = 0; index < data.EquippedBulletIds.Count; index++)
      {
        if (index != slotIndex && data.EquippedBulletIds[index] == craftedBulletId)
        {
          error = "Each equipped slot needs a distinct crafted bullet.";
          return false;
        }
      }

      data.EquippedBulletIds[slotIndex] = craftedBulletId;
      return PersistAndNotify(true, out error);
    }

    public bool TryUnequip(int slotIndex, out string error)
    {
      if (!EnsureActiveSave(out error))
        return false;
      if (slotIndex < 0 || slotIndex >= EquippedSlotCount)
      {
        error = "Invalid equipment slot.";
        return false;
      }

      var previous = data.EquippedBulletIds[slotIndex];
      if (string.IsNullOrEmpty(previous))
      {
        error = "That slot is already empty.";
        return false;
      }

      data.EquippedBulletIds[slotIndex] = string.Empty;
      return PersistAndNotify(true, out error);
    }

    public bool AddLoot(LootType type, int amount, out string error)
    {
      error = null;
      if (!EnsureActiveSave(out error) || type == LootType.None || amount <= 0)
        return false;

      var resource = FindResource(type);
      if (resource == null)
      {
        resource = new ResourceAmountSave { Type = type };
        data.Resources.Add(resource);
      }
      resource.Amount = Math.Max(0, resource.Amount + amount);
      return PersistAndNotify(false, out error);
    }

    public bool TryBuildMagazineSlot(string craftedBulletId, out TurretMagazineSlot slot)
    {
      slot = default;
      var crafted = FindCraftedBullet(craftedBulletId);
      if (crafted == null)
        return false;

      var definition = BulletCatalog.FindDefinition(crafted.DefinitionId);
      if (definition == null)
        return false;

      slot = new TurretMagazineSlot
      {
        CraftedBulletId = new FixedString64Bytes(crafted.Id),
        Pattern = definition.Pattern,
        CraftedStats = definition.BaseStats,
        ShotgunSpreadDegrees = definition.ShotgunSpreadDegrees,
        BurstInterval = definition.BurstInterval,
        IsEquipped = 1
      };

      for (var index = 0; index < crafted.Modifiers.Count; index++)
      {
        var savedModifier = crafted.Modifiers[index];
        var modifier = BulletCatalog.FindModifier(savedModifier.ModifierId);
        if (modifier == null)
          continue;

        if (modifier.RollTiming == ModifierRollTiming.OnCrafting)
          BulletStatUtility.ApplyModifier(ref slot.CraftedStats, modifier.Kind, savedModifier.RolledValue, modifier.EffectDuration);
        else
          BulletStatUtility.AddFireRange(ref slot.FireRolls, modifier.Kind, modifier.MinimumValue, modifier.MaximumValue, modifier.EffectDuration);
      }
      return true;
    }

    private bool EnsureActiveSave(out string error)
    {
      error = null;
      if (data != null && HasActiveSave)
        return true;

      error = "Select a save slot before changing inventory.";
      return false;
    }

    private bool HasMaterials(ResourceCost[] costs)
    {
      if (costs == null)
        return true;
      for (var index = 0; index < costs.Length; index++)
        if (GetResourceCount(costs[index].Type) < costs[index].Amount)
          return false;
      return true;
    }

    private void SpendMaterials(ResourceCost[] costs)
    {
      if (costs == null)
        return;
      for (var index = 0; index < costs.Length; index++)
        FindResource(costs[index].Type).Amount -= costs[index].Amount;
    }

    private void AddMaterials(ResourceCost[] resources)
    {
      if (resources == null)
        return;
      foreach (var resource in resources)
        FindResource(resource.Type).Amount += resource.Amount;
    }

    private ResourceAmountSave FindResource(LootType type)
    {
      return FindResource(data.Resources, type);
    }

    private static ResourceAmountSave FindResource(List<ResourceAmountSave> resources, LootType type)
    {
      for (var index = 0; index < resources.Count; index++)
        if (resources[index].Type == type)
          return resources[index];
      return null;
    }

    private static void AddSpentItem(List<ItemSpendSave> spends, LootType type, int amount)
    {
      if (type == LootType.None || amount <= 0)
        return;
      var existing = spends.Find(spend => spend.Type == type);
      if (existing == null)
        spends.Add(new ItemSpendSave { Type = type, Amount = amount });
      else
        existing.Amount += amount;
    }

  }
}
