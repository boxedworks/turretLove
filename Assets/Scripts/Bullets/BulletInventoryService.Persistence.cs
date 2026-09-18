using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Bullets
{
  public sealed partial class BulletInventoryService
  {
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

    private bool PersistAndNotify(bool affectsLoadout, out string error)
    {
      if (!WriteSave(activeSlotIndex, data, out error))
        return false;

      if (affectsLoadout)
        loadoutRevision++;
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
