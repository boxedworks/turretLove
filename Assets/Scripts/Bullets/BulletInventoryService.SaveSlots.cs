using System;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Bullets
{
  public sealed partial class BulletInventoryService
  {
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
        BulletInventorySaveValidator.Validate(loaded);
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

      var starter = BulletInventorySaveFactory.CreateStarterData();
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

        BulletInventorySaveValidator.Validate(loaded);
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

        var starter = BulletInventorySaveFactory.CreateStarterData();
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
  }
}
