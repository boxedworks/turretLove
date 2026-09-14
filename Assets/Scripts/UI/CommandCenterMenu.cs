using System;
using System.Collections.Generic;
using Assets.Scripts.Bullets;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
  internal sealed class CommandCenterMenu
  {
    private readonly VisualElement overlay;
    private readonly Label saveLabel;
    private readonly Label selectionLabel;
    private readonly Label messageLabel;
    private readonly Button backButton;
    private readonly Button workshopButton;
    private readonly Button startButton;
    private readonly List<Button> levelButtons;
    private readonly Action backRequested;
    private readonly Action workshopRequested;
    private readonly Action<int, int> startRequested;
    private int selectedAreaIndex = -1;
    private int selectedLevelIndex = -1;

    public bool IsVisible => overlay != null && overlay.resolvedStyle.display != DisplayStyle.None;

    public CommandCenterMenu(
      VisualElement overlay,
      Label saveLabel,
      Label selectionLabel,
      Label messageLabel,
      Button backButton,
      Button workshopButton,
      Button startButton,
      List<Button> levelButtons,
      Action backRequested,
      Action workshopRequested,
      Action<int, int> startRequested)
    {
      this.overlay = overlay;
      this.saveLabel = saveLabel;
      this.selectionLabel = selectionLabel;
      this.messageLabel = messageLabel;
      this.backButton = backButton;
      this.workshopButton = workshopButton;
      this.startButton = startButton;
      this.levelButtons = levelButtons;
      this.backRequested = backRequested;
      this.workshopRequested = workshopRequested;
      this.startRequested = startRequested;
    }

    public void RegisterCallbacks()
    {
      backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
      workshopButton?.RegisterCallback<ClickEvent>(OnWorkshopClicked);
      startButton?.RegisterCallback<ClickEvent>(OnStartClicked);
      foreach (var levelButton in levelButtons)
        levelButton.RegisterCallback<ClickEvent>(OnLevelClicked);
    }

    public void UnregisterCallbacks()
    {
      backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
      workshopButton?.UnregisterCallback<ClickEvent>(OnWorkshopClicked);
      startButton?.UnregisterCallback<ClickEvent>(OnStartClicked);
      foreach (var levelButton in levelButtons)
        levelButton.UnregisterCallback<ClickEvent>(OnLevelClicked);
    }

    public void Show()
    {
      SetVisible(true);
      Refresh();
    }

    public void Hide()
    {
      SetVisible(false);
    }

    public void Refresh()
    {
      var inventory = BulletInventoryService.Instance;
      if (saveLabel != null)
        saveLabel.text = inventory.HasActiveSave ? $"ACTIVE SAVE: SLOT {inventory.ActiveSlotIndex + 1}" : "NO ACTIVE SAVE";
      if (selectionLabel != null)
        selectionLabel.text = HasLevelSelection
          ? $"SELECTED: AREA {selectedAreaIndex + 1} / LEVEL {selectedLevelIndex + 1}"
          : "SELECT A LEVEL TO DEPLOY";

      for (var buttonIndex = 0; buttonIndex < levelButtons.Count; buttonIndex++)
      {
        var isSelected = buttonIndex / 3 == selectedAreaIndex && buttonIndex % 3 == selectedLevelIndex;
        levelButtons[buttonIndex].EnableInClassList("command-level-selected", isSelected);
      }

      if (startButton != null)
        startButton.SetEnabled(HasLevelSelection && inventory.HasActiveSave);
      if (messageLabel != null)
        messageLabel.text = HasLevelSelection ? "Ready to deploy." : "Select a level to deploy.";
    }

    public void ShowMessage(string message)
    {
      if (messageLabel != null)
        messageLabel.text = message;
    }

    private void SetVisible(bool visible)
    {
      if (overlay != null)
        overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnBackClicked(ClickEvent clickEvent)
    {
      backRequested?.Invoke();
    }

    private void OnWorkshopClicked(ClickEvent clickEvent)
    {
      workshopRequested?.Invoke();
    }

    private void OnStartClicked(ClickEvent clickEvent)
    {
      if (HasLevelSelection)
        startRequested?.Invoke(selectedAreaIndex, selectedLevelIndex);
    }

    private void OnLevelClicked(ClickEvent clickEvent)
    {
      var buttonIndex = levelButtons.IndexOf(clickEvent.currentTarget as Button);
      if (buttonIndex < 0)
        return;

      selectedAreaIndex = buttonIndex / 3;
      selectedLevelIndex = buttonIndex % 3;
      Refresh();
    }

    private bool HasLevelSelection => selectedAreaIndex >= 0 && selectedLevelIndex >= 0;
  }
}
