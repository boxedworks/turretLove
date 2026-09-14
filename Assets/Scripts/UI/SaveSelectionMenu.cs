using System;
using Assets.Scripts.Bullets;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
  internal sealed class SaveSelectionMenu
  {
    private readonly VisualElement overlay;
    private readonly VisualElement slotList;
    private readonly Label messageLabel;
    private readonly Button backButton;
    private readonly Action saveChanged;
    private int pendingDeleteSlot = -1;

    public bool IsVisible => overlay != null && overlay.resolvedStyle.display != DisplayStyle.None;

    public SaveSelectionMenu(
      VisualElement overlay,
      VisualElement slotList,
      Label messageLabel,
      Button backButton,
      Action saveChanged)
    {
      this.overlay = overlay;
      this.slotList = slotList;
      this.messageLabel = messageLabel;
      this.backButton = backButton;
      this.saveChanged = saveChanged;
    }

    public void RegisterCallbacks()
    {
      backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
    }

    public void UnregisterCallbacks()
    {
      backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
    }

    public void Show(string message = null)
    {
      pendingDeleteSlot = -1;
      SetVisible(true);
      Refresh(message);
    }

    public void Hide()
    {
      SetVisible(false);
    }

    public void Refresh(string message = null)
    {
      if (slotList == null)
        return;

      var inventory = BulletInventoryService.Instance;
      slotList.Clear();
      for (var slotIndex = 0; slotIndex < BulletInventoryService.SlotCount; slotIndex++)
        AddSlotRow(inventory, slotIndex);

      if (messageLabel == null)
        return;

      messageLabel.text = message
        ?? (inventory.HasActiveSave
          ? $"ACTIVE SAVE: SLOT {inventory.ActiveSlotIndex + 1}"
          : "Choose an occupied slot or create a new save.");
    }

    private void SetVisible(bool visible)
    {
      if (overlay != null)
        overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void AddSlotRow(BulletInventoryService inventory, int slotIndex)
    {
      var row = new VisualElement();
      row.AddToClassList("save-slot-row");
      if (inventory.ActiveSlotIndex == slotIndex)
        row.AddToClassList("save-slot-active");

      var description = new VisualElement();
      var title = new Label($"SLOT {slotIndex + 1}");
      title.AddToClassList("save-slot-label");
      description.Add(title);
      var occupied = inventory.HasSaveSlot(slotIndex);
      var state = occupied
        ? inventory.IsSaveSlotValid(slotIndex, out _) ? "OCCUPIED" : "CORRUPT SAVE — LOAD TO BACK UP AND RESET"
        : "EMPTY";
      if (inventory.ActiveSlotIndex == slotIndex)
        state += "  •  ACTIVE";
      var stateLabel = new Label(state);
      stateLabel.AddToClassList("save-slot-state");
      description.Add(stateLabel);
      row.Add(description);

      var actions = new VisualElement();
      actions.AddToClassList("save-slot-actions");
      if (!occupied)
      {
        actions.Add(CreateActionButton("CREATE", () => CreateSlot(slotIndex)));
      }
      else
      {
        actions.Add(CreateActionButton("LOAD", () => LoadSlot(slotIndex)));
        if (pendingDeleteSlot == slotIndex)
        {
          var confirm = CreateActionButton("CONFIRM DELETE", () => DeleteSlot(slotIndex));
          confirm.AddToClassList("save-slot-confirm-delete");
          actions.Add(confirm);
          actions.Add(CreateActionButton("CANCEL", CancelDeletion));
        }
        else
        {
          var delete = CreateActionButton("DELETE", () => ConfirmDeletion(slotIndex));
          delete.AddToClassList("save-slot-delete");
          actions.Add(delete);
        }
      }
      row.Add(actions);
      slotList.Add(row);
    }

    private static Button CreateActionButton(string text, Action action)
    {
      var button = new Button(action) { text = text };
      button.AddToClassList("save-slot-action");
      return button;
    }

    private void CreateSlot(int slotIndex)
    {
      if (!BulletInventoryService.Instance.CreateSaveSlot(slotIndex, out var error))
      {
        Refresh(error);
        return;
      }

      saveChanged?.Invoke();
      Hide();
    }

    private void LoadSlot(int slotIndex)
    {
      if (!BulletInventoryService.Instance.LoadSaveSlot(slotIndex, out var error))
      {
        Refresh(error);
        return;
      }

      saveChanged?.Invoke();
      Hide();
    }

    private void ConfirmDeletion(int slotIndex)
    {
      pendingDeleteSlot = slotIndex;
      Refresh($"Press CONFIRM DELETE to permanently remove slot {slotIndex + 1}.");
    }

    private void CancelDeletion()
    {
      pendingDeleteSlot = -1;
      Refresh();
    }

    private void DeleteSlot(int slotIndex)
    {
      if (!BulletInventoryService.Instance.DeleteSaveSlot(slotIndex, out var error))
      {
        Refresh(error);
        return;
      }

      pendingDeleteSlot = -1;
      saveChanged?.Invoke();
      Refresh($"Save slot {slotIndex + 1} deleted.");
    }

    private void OnBackClicked(ClickEvent clickEvent)
    {
      Hide();
    }
  }
}
