using System;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Entities.Loot;
using Assets.Scripts.Entities.Player.Turret;
using Unity.Collections;
using UnityEngine;

namespace Assets.Scripts.Bullets
{
  public enum BulletInventoryOperationType
  {
    Crafted,
    ModifierApplied,
    Equipped,
    Unequipped,
    LootCollected
  }

  public readonly struct BulletInventoryOperation
  {
    public readonly BulletInventoryOperationType Type;
    public readonly string BulletId;
    public readonly string Detail;

    public BulletInventoryOperation(BulletInventoryOperationType type, string bulletId, string detail)
    {
      Type = type;
      BulletId = bulletId;
      Detail = detail;
    }
  }

  [Serializable]
  public sealed class ResourceAmountSave
  {
    public LootType Type;
    public int Amount;
  }

  [Serializable]
  public sealed class CraftedBulletModifierSave
  {
    public string ModifierId;
    public ModifierRollTiming RollTiming;
    public float RolledValue;
  }

  [Serializable]
  public sealed class CraftedBulletSave
  {
    public string Id;
    public string DefinitionId;
    public List<CraftedBulletModifierSave> Modifiers = new();
  }

  [Serializable]
  public sealed class SaveData
  {
    public int Version;
    public List<ResourceAmountSave> Resources = new();
    public List<CraftedBulletSave> CraftedBullets = new();
    public List<string> EquippedBulletIds = new();
  }

  [Serializable]
  public sealed class SettingsData
  {
    public int ActiveSaveSlotIndex = -1;
  }

  /// <summary>
  /// Owns the selected slot's durable player bullet data. ECS only receives derived, unmanaged magazine payloads.
  /// </summary>
  public sealed class BulletInventoryService : MonoBehaviour
  {
    public const int CurrentSaveVersion = 2;
    public const int EquippedSlotCount = 4;
    public const int SlotCount = 3;

    private const string LegacySaveFileName = "bullet_inventory.json";
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
    public event Action<BulletInventoryOperation> OperationApplied;

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
      MigrateLegacySave();
      RestoreLastActiveSave();
    }

    public bool HasSaveSlot(int slotIndex)
    {
      return IsValidSlotIndex(slotIndex) && File.Exists(GetSaveSlotPath(slotIndex));
    }

    public bool IsSaveSlotValid(int slotIndex, out string error)
    {
      error = null;
      if (!HasSaveSlot(slotIndex))
      {
        error = "This save slot is empty.";
        return false;
      }

      try
      {
        var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(GetSaveSlotPath(slotIndex)));
        if (loaded == null)
          throw new InvalidDataException("Save JSON did not contain inventory data.");
        MigrateAndValidate(loaded);
        return true;
      }
      catch (Exception exception)
      {
        error = $"Save data is invalid: {exception.Message}";
        return false;
      }
    }

    public bool CreateSaveSlot(int slotIndex, out string error)
    {
      error = null;
      if (!ValidateSlotIndex(slotIndex, out error))
        return false;
      if (HasSaveSlot(slotIndex))
      {
        error = $"Save slot {slotIndex + 1} is already occupied.";
        return false;
      }

      var starter = CreateStarterData();
      if (!WriteSave(slotIndex, starter, out error))
        return false;

      SelectLoadedSave(slotIndex, starter);
      return true;
    }

    public bool LoadSaveSlot(int slotIndex, out string error)
    {
      error = null;
      if (!ValidateSlotIndex(slotIndex, out error))
        return false;
      if (!HasSaveSlot(slotIndex))
      {
        error = $"Save slot {slotIndex + 1} is empty. Create it first.";
        return false;
      }

      try
      {
        var loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(GetSaveSlotPath(slotIndex)));
        if (loaded == null)
          throw new InvalidDataException("Save JSON did not contain inventory data.");

        MigrateAndValidate(loaded);
        if (!WriteSave(slotIndex, loaded, out error))
          return false;

        SelectLoadedSave(slotIndex, loaded);
        return true;
      }
      catch (Exception exception)
      {
        if (!BackUpInvalidSave(slotIndex, out var backupError))
        {
          error = $"Save slot {slotIndex + 1} is invalid ({exception.Message}) and could not be backed up: {backupError}";
          return false;
        }

        var starter = CreateStarterData();
        if (!WriteSave(slotIndex, starter, out var writeError))
        {
          error = $"Save slot {slotIndex + 1} is invalid ({exception.Message}) and could not be reset: {writeError}";
          return false;
        }

        Debug.LogWarning($"Bullet inventory save slot {slotIndex + 1} was invalid and has been backed up and reset: {exception.Message}");
        SelectLoadedSave(slotIndex, starter);
        return true;
      }
    }

    public bool DeleteSaveSlot(int slotIndex, out string error)
    {
      error = null;
      if (!ValidateSlotIndex(slotIndex, out error))
        return false;
      if (!HasSaveSlot(slotIndex))
      {
        error = $"Save slot {slotIndex + 1} is already empty.";
        return false;
      }

      try
      {
        File.Delete(GetSaveSlotPath(slotIndex));
        if (activeSlotIndex == slotIndex)
        {
          activeSlotIndex = -1;
          data = null;
          ClearActiveSaveSetting();
          NotifyChanged();
        }
        return true;
      }
      catch (Exception exception)
      {
        error = $"Could not delete save slot {slotIndex + 1}: {exception.Message}";
        return false;
      }
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

    public bool TryCraft(string definitionId, out string craftedBulletId, out string error)
    {
      craftedBulletId = null;
      if (!EnsureActiveSave(out error))
        return false;

      var definition = BulletCatalog.FindDefinition(definitionId);
      if (definition == null)
      {
        error = "Unknown bullet recipe.";
        return false;
      }
      if (!HasMaterials(definition.CraftCost))
      {
        error = "Not enough materials.";
        return false;
      }

      SpendMaterials(definition.CraftCost);
      var crafted = new CraftedBulletSave
      {
        Id = Guid.NewGuid().ToString("N"),
        DefinitionId = definition.Id,
        Modifiers = new List<CraftedBulletModifierSave>()
      };
      data.CraftedBullets.Add(crafted);
      craftedBulletId = crafted.Id;
      return PersistAndNotify(new BulletInventoryOperation(BulletInventoryOperationType.Crafted, crafted.Id, definition.DisplayName), true, out error);
    }

    public bool TryApplyModifier(string craftedBulletId, string modifierId, out string error)
    {
      if (!EnsureActiveSave(out error))
        return false;

      var crafted = FindCraftedBullet(craftedBulletId);
      var modifier = BulletCatalog.FindModifier(modifierId);
      if (crafted == null || modifier == null)
      {
        error = "Select a valid crafted bullet and modifier.";
        return false;
      }

      for (var index = 0; index < crafted.Modifiers.Count; index++)
      {
        if (crafted.Modifiers[index].ModifierId == modifier.Id)
        {
          error = "That modifier is already installed.";
          return false;
        }
      }
      if (!HasMaterials(modifier.ApplyCost))
      {
        error = "Not enough materials.";
        return false;
      }

      SpendMaterials(modifier.ApplyCost);
      var roll = modifier.RollTiming == ModifierRollTiming.OnCrafting
        ? UnityEngine.Random.Range(modifier.MinimumValue, modifier.MaximumValue)
        : 0f;
      crafted.Modifiers.Add(new CraftedBulletModifierSave
      {
        ModifierId = modifier.Id,
        RollTiming = modifier.RollTiming,
        RolledValue = roll
      });
      return PersistAndNotify(new BulletInventoryOperation(BulletInventoryOperationType.ModifierApplied, crafted.Id, modifier.DisplayName), true, out error);
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
      return PersistAndNotify(new BulletInventoryOperation(BulletInventoryOperationType.Equipped, craftedBulletId, $"Slot {slotIndex + 1}"), true, out error);
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
      return PersistAndNotify(new BulletInventoryOperation(BulletInventoryOperationType.Unequipped, previous, $"Slot {slotIndex + 1}"), true, out error);
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
      return PersistAndNotify(new BulletInventoryOperation(BulletInventoryOperationType.LootCollected, null, $"{amount} {type}"), false, out error);
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

    private void MigrateLegacySave()
    {
      var legacyPath = Path.Combine(Application.persistentDataPath, LegacySaveFileName);
      var slotOnePath = GetSaveSlotPath(0);
      if (File.Exists(slotOnePath) || !File.Exists(legacyPath))
        return;

      try
      {
        Directory.CreateDirectory(Application.persistentDataPath);
        File.Copy(legacyPath, slotOnePath, false);
        File.Delete(legacyPath);
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"Could not migrate legacy bullet inventory save to slot 1: {exception.Message}");
      }
    }

    private void SelectLoadedSave(int slotIndex, SaveData loaded)
    {
      activeSlotIndex = slotIndex;
      data = loaded;
      WriteActiveSaveSetting(slotIndex);
      NotifyChanged();
    }

    private void RestoreLastActiveSave()
    {
      if (!TryReadActiveSaveSetting(out var slotIndex))
        return;

      if (!IsValidSlotIndex(slotIndex) || !HasSaveSlot(slotIndex))
      {
        ClearActiveSaveSetting();
        return;
      }

      if (!LoadSaveSlot(slotIndex, out var error))
        Debug.LogWarning($"Could not restore save slot {slotIndex + 1}: {error}");
    }

    private static bool TryReadActiveSaveSetting(out int slotIndex)
    {
      slotIndex = -1;
      var settingsPath = GetSettingsPath();
      if (!File.Exists(settingsPath))
        return false;

      try
      {
        var settings = JsonUtility.FromJson<SettingsData>(File.ReadAllText(settingsPath));
        if (settings == null)
          throw new InvalidDataException("Settings JSON did not contain settings data.");

        slotIndex = settings.ActiveSaveSlotIndex;
        return true;
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"Could not read bullet inventory settings: {exception.Message}");
        return false;
      }
    }

    private static void WriteActiveSaveSetting(int slotIndex)
    {
      try
      {
        Directory.CreateDirectory(Application.persistentDataPath);
        File.WriteAllText(
          GetSettingsPath(),
          JsonUtility.ToJson(new SettingsData { ActiveSaveSlotIndex = slotIndex }, true));
      }
      catch (Exception exception)
      {
        Debug.LogError($"Could not write bullet inventory settings: {exception.Message}");
      }
    }

    private static void ClearActiveSaveSetting()
    {
      var settingsPath = GetSettingsPath();
      if (!File.Exists(settingsPath))
        return;

      try
      {
        File.Delete(settingsPath);
      }
      catch (Exception exception)
      {
        Debug.LogError($"Could not clear bullet inventory settings: {exception.Message}");
      }
    }

    private void NotifyChanged()
    {
      loadoutRevision++;
      Changed?.Invoke();
    }

    private bool PersistAndNotify(BulletInventoryOperation operation, bool affectsLoadout, out string error)
    {
      if (!WriteSave(activeSlotIndex, data, out error))
        return false;

      if (affectsLoadout)
        loadoutRevision++;
      OperationApplied?.Invoke(operation);
      Changed?.Invoke();
      return true;
    }

    private bool WriteSave(int slotIndex, SaveData save, out string error)
    {
      error = null;
      try
      {
        Directory.CreateDirectory(Application.persistentDataPath);
        File.WriteAllText(GetSaveSlotPath(slotIndex), JsonUtility.ToJson(save, true));
        return true;
      }
      catch (Exception exception)
      {
        error = $"Could not write save slot {slotIndex + 1}: {exception.Message}";
        Debug.LogError(error);
        return false;
      }
    }

    private bool BackUpInvalidSave(int slotIndex, out string error)
    {
      error = null;
      try
      {
        var savePath = GetSaveSlotPath(slotIndex);
        File.Copy(savePath, savePath + ".corrupt", true);
        return true;
      }
      catch (Exception exception)
      {
        error = exception.Message;
        return false;
      }
    }

    private static SaveData CreateStarterData()
    {
      var starter = new SaveData { Version = CurrentSaveVersion };
      foreach (LootType type in Enum.GetValues(typeof(LootType)))
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
          Modifiers = new List<CraftedBulletModifierSave>()
        });
        starter.EquippedBulletIds.Add($"starter-{index + 1}-bullet");
      }
      return starter;
    }

    private static void MigrateAndValidate(SaveData save)
    {
      save.Resources ??= new List<ResourceAmountSave>();
      save.CraftedBullets ??= new List<CraftedBulletSave>();
      save.EquippedBulletIds ??= new List<string>();

      if (save.Version < 1)
        save.Version = 1;
      if (save.Version < 2)
        save.Version = 2;
      if (save.Version > CurrentSaveVersion)
        throw new InvalidDataException("Save was created by a newer version of the game.");

      for (var index = save.Resources.Count - 1; index >= 0; index--)
      {
        var resource = save.Resources[index];
        if (resource == null || resource.Type == LootType.None)
          save.Resources.RemoveAt(index);
        else
          resource.Amount = Math.Max(0, resource.Amount);
      }

      foreach (LootType type in Enum.GetValues(typeof(LootType)))
      {
        if (type != LootType.None && FindResource(save.Resources, type) == null)
          save.Resources.Add(new ResourceAmountSave { Type = type, Amount = 0 });
      }

      var uniqueIds = new HashSet<string>();
      for (var index = save.CraftedBullets.Count - 1; index >= 0; index--)
      {
        var crafted = save.CraftedBullets[index];
        if (crafted == null || string.IsNullOrWhiteSpace(crafted.Id) || !uniqueIds.Add(crafted.Id) || BulletCatalog.FindDefinition(crafted.DefinitionId) == null)
        {
          save.CraftedBullets.RemoveAt(index);
          continue;
        }

        crafted.Modifiers ??= new List<CraftedBulletModifierSave>();
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
          if (definition.RollTiming == ModifierRollTiming.OnCrafting)
            modifier.RolledValue = Mathf.Clamp(modifier.RolledValue, definition.MinimumValue, definition.MaximumValue);
          else
            modifier.RolledValue = 0f;
        }
      }

      while (save.EquippedBulletIds.Count < EquippedSlotCount)
        save.EquippedBulletIds.Add(string.Empty);
      if (save.EquippedBulletIds.Count > EquippedSlotCount)
        save.EquippedBulletIds.RemoveRange(EquippedSlotCount, save.EquippedBulletIds.Count - EquippedSlotCount);

      var equipped = new HashSet<string>();
      for (var index = 0; index < save.EquippedBulletIds.Count; index++)
      {
        var bulletId = save.EquippedBulletIds[index];
        if (FindCrafted(save.CraftedBullets, bulletId) == null || !equipped.Add(bulletId))
          save.EquippedBulletIds[index] = string.Empty;
      }

      save.Version = CurrentSaveVersion;
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

    private static CraftedBulletSave FindCrafted(List<CraftedBulletSave> craftedBullets, string id)
    {
      if (string.IsNullOrEmpty(id))
        return null;
      for (var index = 0; index < craftedBullets.Count; index++)
        if (craftedBullets[index].Id == id)
          return craftedBullets[index];
      return null;
    }

    private static bool IsValidSlotIndex(int slotIndex)
    {
      return slotIndex >= 0 && slotIndex < SlotCount;
    }

    private static bool ValidateSlotIndex(int slotIndex, out string error)
    {
      error = null;
      if (IsValidSlotIndex(slotIndex))
        return true;

      error = $"Save slot index must be between 0 and {SlotCount - 1}.";
      return false;
    }

    private static string GetSaveSlotPath(int slotIndex)
    {
      return Path.Combine(Application.persistentDataPath, string.Format(SlotSaveFileNameFormat, slotIndex + 1));
    }

    private static string GetSettingsPath()
    {
      return Path.Combine(Application.persistentDataPath, SettingsFileName);
    }
  }
}
