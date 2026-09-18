using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Bullets;
using Assets.Scripts.Entities.Loot;
using Assets.Scripts.Entities.Player.Turret;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
  internal sealed class CommandCenterMenu
  {
    private readonly VisualElement overlay;
    private readonly Label selectionLabel;
    private const int MaximumLogEntries = 8;
    private readonly Label logOutput;
    private readonly Button backButton;
    private readonly Button startButton;
    private readonly List<Button> areaButtons;
    private readonly List<Button> levelButtons;
    private readonly VisualElement inventoryList;
    private readonly VisualElement inventoryPreviewIcon;
    private readonly VisualElement inventoryPreviewDetails;
    private readonly VisualElement conversionList;
    private readonly VisualElement craftingBenchSlot;
    private readonly VisualElement craftingBenchDetails;
    private readonly VisualElement magazineSlots;
    private readonly Label runtimeTooltip;
    private readonly Dictionary<VisualElement, string> craftedBulletDropTargets = new();
    private readonly Dictionary<VisualElement, int> magazineDropTargets = new();
    private readonly Action backRequested;
    private readonly Action<int, int> startRequested;
    private readonly List<string> logEntries = new();
    private int selectedAreaIndex = -1;
    private int selectedLevelIndex = -1;
    private string selectedCraftedBulletId;
    private LootType? selectedInventoryItem;
    private string draggedCraftedBulletId;
    private string craftingBenchBulletId;
    private LootType? draggedInventoryItem;
    private bool showAttributeDefinitions;
    private VisualElement dragSource;
    private VisualElement dragGhost;
    private int dragPointerId = -1;

    public bool IsVisible => overlay != null && overlay.resolvedStyle.display != DisplayStyle.None;

    public CommandCenterMenu(
      VisualElement overlay,
      Label selectionLabel,
      Label logOutput,
      Button backButton,
      Button startButton,
      List<Button> areaButtons,
      List<Button> levelButtons,
      VisualElement inventoryList,
      VisualElement inventoryPreviewIcon,
      VisualElement inventoryPreviewDetails,
      VisualElement conversionList,
      VisualElement craftingBenchSlot,
      VisualElement craftingBenchDetails,
      VisualElement magazineSlots,
      Action backRequested,
      Action<int, int> startRequested)
    {
      this.overlay = overlay;
      this.selectionLabel = selectionLabel;
      this.logOutput = logOutput;
      this.backButton = backButton;
      this.startButton = startButton;
      this.areaButtons = areaButtons;
      this.levelButtons = levelButtons;
      this.inventoryList = inventoryList;
      this.inventoryPreviewIcon = inventoryPreviewIcon;
      this.inventoryPreviewDetails = inventoryPreviewDetails;
      this.conversionList = conversionList;
      this.craftingBenchSlot = craftingBenchSlot;
      this.craftingBenchDetails = craftingBenchDetails;
      this.magazineSlots = magazineSlots;
      this.backRequested = backRequested;
      this.startRequested = startRequested;
      if (overlay != null)
      {
        runtimeTooltip = new Label();
        runtimeTooltip.AddToClassList("command-runtime-tooltip");
        runtimeTooltip.pickingMode = PickingMode.Ignore;
        runtimeTooltip.style.display = DisplayStyle.None;
        overlay.Add(runtimeTooltip);
        dragGhost = new VisualElement { pickingMode = PickingMode.Ignore };
        dragGhost.AddToClassList("command-drag-ghost");
        dragGhost.style.display = DisplayStyle.None;
        overlay.Add(dragGhost);
      }
    }

    public void RegisterCallbacks()
    {
      backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
      startButton?.RegisterCallback<ClickEvent>(OnStartClicked);
      foreach (var areaButton in areaButtons)
        areaButton.RegisterCallback<ClickEvent>(OnAreaClicked);
      foreach (var levelButton in levelButtons)
        levelButton.RegisterCallback<ClickEvent>(OnLevelClicked);
      SetLevelButtonsEnabled(false);
    }

    public void UnregisterCallbacks()
    {
      backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
      startButton?.UnregisterCallback<ClickEvent>(OnStartClicked);
      foreach (var areaButton in areaButtons)
        areaButton.UnregisterCallback<ClickEvent>(OnAreaClicked);
      foreach (var levelButton in levelButtons)
        levelButton.UnregisterCallback<ClickEvent>(OnLevelClicked);
    }

    public void Show()
    {
      SetVisible(true);
      Refresh();
      if (logEntries.Count == 0)
        WriteLog("Command center online.");
    }

    public void Hide()
    {
      showAttributeDefinitions = false;
      SetVisible(false);
    }

    public void Update()
    {
      if (!IsVisible)
        return;

      var shouldShowDefinitions = Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
      if (showAttributeDefinitions == shouldShowDefinitions)
        return;

      showAttributeDefinitions = shouldShowDefinitions;
      RefreshLoadout(BulletInventoryService.Instance);
    }

    public void Refresh(string status = null)
    {
      var inventory = BulletInventoryService.Instance;
      if (selectionLabel != null)
        selectionLabel.text = HasLevelSelection
          ? $"SELECTED: AREA {selectedAreaIndex + 1} / LEVEL {selectedLevelIndex + 1}"
          : "SELECT A LEVEL TO DEPLOY";
      for (var index = 0; index < areaButtons.Count; index++)
        areaButtons[index].EnableInClassList("command-level-selected", index == selectedAreaIndex);
      for (var index = 0; index < levelButtons.Count; index++)
        levelButtons[index].EnableInClassList("command-level-selected", index == selectedLevelIndex);

      if (startButton != null)
        startButton.SetEnabled(HasLevelSelection && inventory.HasActiveSave);
      RefreshLoadout(inventory);
      if (!string.IsNullOrEmpty(status))
        WriteLog(status);
    }

    public void ShowMessage(string message)
    {
      WriteLog(message);
    }

    private void WriteLog(string message)
    {
      if (string.IsNullOrWhiteSpace(message))
        return;

      logEntries.Add($"> {message}");
      if (logEntries.Count > MaximumLogEntries)
        logEntries.RemoveAt(0);
      if (logOutput != null)
        logOutput.text = string.Join("\n", logEntries);
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

    private void OnStartClicked(ClickEvent clickEvent)
    {
      if (HasLevelSelection)
        startRequested?.Invoke(selectedAreaIndex, selectedLevelIndex);
    }

    private void OnAreaClicked(ClickEvent clickEvent)
    {
      selectedAreaIndex = areaButtons.IndexOf(clickEvent.currentTarget as Button);
      selectedLevelIndex = -1;
      SetLevelButtonsEnabled(true);
      Refresh();
    }

    private void OnLevelClicked(ClickEvent clickEvent)
    {
      selectedLevelIndex = levelButtons.IndexOf(clickEvent.currentTarget as Button);
      Refresh();
    }

    private void SetLevelButtonsEnabled(bool enabled)
    {
      foreach (var levelButton in levelButtons)
        levelButton.SetEnabled(enabled);
    }

    private bool HasLevelSelection => selectedAreaIndex >= 0 && selectedLevelIndex >= 0;

    private void RefreshLoadout(BulletInventoryService inventory)
    {
      if (inventoryList == null || conversionList == null
        || magazineSlots == null)
        return;

      inventoryList.Clear();
      ResetInventoryPreview();
      conversionList.Clear();
      magazineSlots.Clear();
      craftedBulletDropTargets.Clear();
      magazineDropTargets.Clear();

      if (!inventory.HasActiveSave)
        return;

      if (inventory.FindCraftedBullet(selectedCraftedBulletId) == null)
        selectedCraftedBulletId = null;
      if (inventory.FindCraftedBullet(craftingBenchBulletId) == null
        || GetMagazineSlotIndex(inventory, craftingBenchBulletId) >= 0)
        craftingBenchBulletId = null;
      if (selectedInventoryItem.HasValue && inventory.GetResourceCount(selectedInventoryItem.Value) <= 0)
        selectedInventoryItem = null;
      RestoreSelectedInventoryPreview(inventory);

      foreach (LootType type in Enum.GetValues(typeof(LootType)))
      {
        if (type == LootType.None)
          continue;
        var itemCount = inventory.GetResourceCount(type);
        if (itemCount <= 0)
          continue;
        var iconClass = $"command-icon-item-{type.ToString().ToLowerInvariant()}";
        var details = GetLootDetails(type, itemCount);
        var resourceCard = CreateIconTile(iconClass, details, itemCount.ToString());
        resourceCard.EnableInClassList("command-card-selected", type == selectedInventoryItem);
        RegisterDragSource(resourceCard, iconClass, () => BeginInventoryItemDrag(type));
        RegisterInventoryPreview(resourceCard, iconClass, details);
        resourceCard.RegisterCallback<ClickEvent>(_ => SelectInventoryItem(type));
        inventoryList.Add(resourceCard);
      }

      foreach (var crafted in inventory.CraftedBullets)
      {
        if (crafted.Id != craftingBenchBulletId && GetMagazineSlotIndex(inventory, crafted.Id) < 0)
          inventoryList.Add(CreateCraftedBulletCard(crafted));
      }

      foreach (var recipe in BulletCatalog.ConversionRecipes)
      {
        var recipeId = recipe.Id;
        conversionList.Add(CreateActionButton(
          "command-icon-conversion",
          $"{recipe.DisplayName}\nConverts: {FormatLootCost(recipe.Input)}\nProduces: {FormatLootCost(recipe.Output)}",
          () => ConvertLoot(recipeId)));
      }

      for (var slotIndex = 0; slotIndex < BulletInventoryService.EquippedSlotCount; slotIndex++)
        magazineSlots.Add(CreateMagazineSlot(inventory, slotIndex));

      RefreshCraftingBench(inventory);
    }

    private VisualElement CreateCraftedBulletCard(CraftedBulletSave crafted)
    {
      var definition = BulletCatalog.FindDefinition(crafted.DefinitionId);
      var iconClass = $"command-icon-bullet-{definition?.Pattern.ToString().ToLowerInvariant() ?? "unknown"}";
      var details = GetCraftedBulletDetails(crafted);
      var card = CreateIconTile(
        iconClass,
        $"{definition?.DisplayName ?? "Unknown bullet"}\n{definition?.Description}\nModifiers: {crafted.Modifiers.Count}\nDrag to a magazine slot or the crafting bench.",
        crafted.Modifiers.Count.ToString());
      card.EnableInClassList("command-card-selected", crafted.Id == selectedCraftedBulletId);
      craftedBulletDropTargets.Add(card, crafted.Id);
      RegisterDragSource(card, iconClass, () => BeginBulletDrag(crafted.Id));
      RegisterBulletInventoryPreview(card, iconClass, crafted);
      card.RegisterCallback<ClickEvent>(_ => SelectCraftedBullet(crafted.Id));
      var salvageButton = CreateActionButton(
        "command-icon-salvage",
        "Salvage this unequipped bullet.\nRefund: 50% of spent inventory items, rounded down.",
        () => SalvageBullet(crafted.Id));
      salvageButton.AddToClassList("command-salvage-button");
      card.Add(salvageButton);
      return card;
    }

    private VisualElement CreateMagazineSlot(BulletInventoryService inventory, int slotIndex)
    {
      var crafted = inventory.FindCraftedBullet(inventory.GetEquippedBulletId(slotIndex));
      var definition = crafted == null ? null : BulletCatalog.FindDefinition(crafted.DefinitionId);
      var iconClass = crafted == null
        ? "command-icon-empty-slot"
        : $"command-icon-bullet-{definition?.Pattern.ToString().ToLowerInvariant() ?? "unknown"}";
      var slot = CreateIconTile(
        iconClass,
        crafted == null
          ? $"Magazine slot {slotIndex + 1}\nEmpty. Drop a crafted bullet here."
          : $"Magazine slot {slotIndex + 1}\n{definition?.DisplayName ?? "Unknown bullet"}\nDrag to another magazine slot, the crafting bench, or Player Inventory.",
        (slotIndex + 1).ToString());
      slot.AddToClassList("command-magazine-slot");
      magazineDropTargets.Add(slot, slotIndex);
      if (crafted != null)
      {
        slot.EnableInClassList("command-card-selected", crafted.Id == selectedCraftedBulletId);
        RegisterDragSource(
          slot,
          iconClass,
          () => BeginBulletDrag(crafted.Id));
        RegisterBulletInventoryPreview(slot, iconClass, crafted);
        slot.RegisterCallback<ClickEvent>(_ => SelectCraftedBullet(crafted.Id));
      }
      return slot;
    }

    private VisualElement CreateIconTile(string iconClass, string tooltip, string badge = null)
    {
      var card = new VisualElement();
      card.AddToClassList("command-icon-tile");
      card.tooltip = tooltip;
      var icon = new VisualElement();
      icon.AddToClassList("command-item-icon");
      icon.AddToClassList(iconClass);
      card.Add(icon);
      if (!string.IsNullOrEmpty(badge))
      {
        var badgeLabel = new Label(badge);
        badgeLabel.AddToClassList("command-item-badge");
        card.Add(badgeLabel);
      }
      RegisterRuntimeTooltip(card, tooltip);
      return card;
    }

    private Button CreateActionButton(string iconClass, string tooltip, Action action)
    {
      var button = new Button(action) { tooltip = tooltip };
      button.AddToClassList("command-action-button");
      button.Add(CreateIconTile(iconClass, tooltip));
      return button;
    }

    private void RegisterDragSource(VisualElement element, string iconClass, Action beginDrag)
    {
      element.RegisterCallback<PointerDownEvent>(pointerEvent =>
      {
        if (pointerEvent.button != 0)
          return;
        beginDrag();
        StartDrag(element, iconClass, pointerEvent);
        pointerEvent.StopPropagation();
      });
      element.RegisterCallback<PointerMoveEvent>(OnDragMoved);
      element.RegisterCallback<PointerUpEvent>(OnDragReleased);
    }

    private void StartDrag(VisualElement source, string iconClass, PointerDownEvent pointerEvent)
    {
      dragSource = source;
      dragPointerId = pointerEvent.pointerId;
      dragSource.CapturePointer(dragPointerId);
      dragGhost.Clear();
      var icon = new VisualElement();
      icon.AddToClassList("command-item-icon");
      icon.AddToClassList(iconClass);
      dragGhost.Add(icon);
      dragGhost.style.display = DisplayStyle.Flex;
      UpdateDragGhost(pointerEvent.position);
      HideRuntimeTooltip();
    }

    private void OnDragMoved(PointerMoveEvent pointerEvent)
    {
      if (pointerEvent.pointerId == dragPointerId)
        UpdateDragGhost(pointerEvent.position);
    }

    private void OnDragReleased(PointerUpEvent pointerEvent)
    {
      if (pointerEvent.pointerId != dragPointerId)
        return;

      var target = overlay.panel.Pick(new Vector2(pointerEvent.position.x, pointerEvent.position.y));
      if (!TryCompleteDrop(target))
        ClearDrag();
      pointerEvent.StopPropagation();
    }

    private void UpdateDragGhost(Vector3 pointerPosition)
    {
      var localPosition = overlay.WorldToLocal(new Vector2(pointerPosition.x, pointerPosition.y));
      dragGhost.style.left = localPosition.x + 10f;
      dragGhost.style.top = localPosition.y + 10f;
      dragGhost.BringToFront();
    }

    private bool TryCompleteDrop(VisualElement target)
    {
      for (var current = target; current != null; current = current.parent)
      {
        if (draggedInventoryItem.HasValue && current == craftingBenchSlot)
        {
          ApplyDraggedCraftingMaterialToBench();
          return true;
        }
        if (!string.IsNullOrWhiteSpace(draggedCraftedBulletId) && magazineDropTargets.TryGetValue(current, out var slotIndex))
        {
          MoveDraggedBullet(slotIndex);
          return true;
        }
        if (!string.IsNullOrWhiteSpace(draggedCraftedBulletId) && current == craftingBenchSlot)
        {
          PlaceBulletInCraftingBench();
          return true;
        }
        if (!string.IsNullOrWhiteSpace(draggedCraftedBulletId) && current == inventoryList
          && ReturnDraggedBulletToInventory())
        {
          return true;
        }
      }
      return false;
    }

    private void RegisterRuntimeTooltip(VisualElement element, string tooltip)
    {
      if (runtimeTooltip == null || string.IsNullOrWhiteSpace(tooltip))
        return;

      element.RegisterCallback<PointerEnterEvent>(pointerEvent => ShowRuntimeTooltip(tooltip, pointerEvent.position));
      element.RegisterCallback<PointerMoveEvent>(pointerEvent => ShowRuntimeTooltip(tooltip, pointerEvent.position));
      element.RegisterCallback<PointerLeaveEvent>(_ => HideRuntimeTooltip());
    }

    private void RegisterInventoryPreview(VisualElement element, string iconClass, string details)
    {
      if (inventoryPreviewIcon == null || inventoryPreviewDetails == null)
        return;

      element.RegisterCallback<PointerEnterEvent>(_ => ShowInventoryPreview(iconClass, details));
      element.RegisterCallback<PointerLeaveEvent>(_ => RestoreSelectedInventoryPreview(BulletInventoryService.Instance));
    }

    private void RegisterBulletInventoryPreview(VisualElement element, string iconClass, CraftedBulletSave bullet)
    {
      if (inventoryPreviewIcon == null || inventoryPreviewDetails == null)
        return;

      element.RegisterCallback<PointerEnterEvent>(_ => ShowBulletInventoryPreview(iconClass, bullet));
      element.RegisterCallback<PointerLeaveEvent>(_ => RestoreSelectedInventoryPreview(BulletInventoryService.Instance));
    }

    private void ShowInventoryPreview(string iconClass, string details)
    {
      inventoryPreviewIcon.ClearClassList();
      inventoryPreviewIcon.AddToClassList("command-inventory-preview-icon");
      inventoryPreviewIcon.AddToClassList("command-item-icon");
      inventoryPreviewIcon.AddToClassList(iconClass);
      inventoryPreviewDetails.Clear();
      inventoryPreviewDetails.Add(new Label(details));
    }

    private void ShowBulletInventoryPreview(string iconClass, CraftedBulletSave bullet)
    {
      inventoryPreviewIcon.ClearClassList();
      inventoryPreviewIcon.AddToClassList("command-inventory-preview-icon");
      inventoryPreviewIcon.AddToClassList("command-item-icon");
      inventoryPreviewIcon.AddToClassList(iconClass);
      PopulateBulletDetails(inventoryPreviewDetails, BulletInventoryService.Instance, bullet, null);
    }

    private void ResetInventoryPreview()
    {
      if (inventoryPreviewIcon != null)
      {
        inventoryPreviewIcon.ClearClassList();
        inventoryPreviewIcon.AddToClassList("command-inventory-preview-icon");
        inventoryPreviewIcon.AddToClassList("command-item-icon");
      }
      if (inventoryPreviewDetails != null)
      {
        inventoryPreviewDetails.Clear();
        inventoryPreviewDetails.Add(new Label("Hover over an inventory item to inspect it."));
      }
    }

    private void RestoreSelectedInventoryPreview(BulletInventoryService inventory)
    {
      if (selectedInventoryItem.HasValue)
      {
        var itemCount = inventory.GetResourceCount(selectedInventoryItem.Value);
        if (itemCount > 0)
        {
          var iconClass = $"command-icon-item-{selectedInventoryItem.Value.ToString().ToLowerInvariant()}";
          ShowInventoryPreview(iconClass, GetLootDetails(selectedInventoryItem.Value, itemCount));
          return;
        }
      }

      var crafted = inventory.FindCraftedBullet(selectedCraftedBulletId);
      if (crafted != null)
      {
        var definition = BulletCatalog.FindDefinition(crafted.DefinitionId);
        var iconClass = $"command-icon-bullet-{definition?.Pattern.ToString().ToLowerInvariant() ?? "unknown"}";
        ShowBulletInventoryPreview(iconClass, crafted);
        return;
      }

      ResetInventoryPreview();
    }

    private void ShowRuntimeTooltip(string tooltip, Vector3 pointerPosition)
    {
      var localPosition = overlay.WorldToLocal(new Vector2(pointerPosition.x, pointerPosition.y));
      runtimeTooltip.text = tooltip;
      runtimeTooltip.style.left = localPosition.x + 14f;
      runtimeTooltip.style.top = localPosition.y + 14f;
      runtimeTooltip.style.display = DisplayStyle.Flex;
      runtimeTooltip.BringToFront();
    }

    private void HideRuntimeTooltip()
    {
      if (runtimeTooltip != null)
        runtimeTooltip.style.display = DisplayStyle.None;
    }

    private void BeginBulletDrag(string bulletId)
    {
      draggedCraftedBulletId = bulletId;
      draggedInventoryItem = null;
    }

    private void BeginInventoryItemDrag(LootType item)
    {
      draggedInventoryItem = item;
      draggedCraftedBulletId = null;
    }

    private void RefreshCraftingBench(BulletInventoryService inventory)
    {
      if (craftingBenchSlot == null)
        return;

      craftingBenchSlot.Clear();
      var bullet = inventory.FindCraftedBullet(craftingBenchBulletId);
      if (bullet == null)
      {
        craftingBenchSlot.Add(CreateIconTile(
          "command-icon-empty-slot",
          "Crafting Bench\nEmpty. Drag a bullet base here."));
        PopulateBulletDetails(craftingBenchDetails, null, null, "Place a bullet base in the slot to inspect its attributes.");
        return;
      }

      var definition = BulletCatalog.FindDefinition(bullet.DefinitionId);
      var benchBullet = CreateIconTile(
        $"command-icon-bullet-{definition?.Pattern.ToString().ToLowerInvariant() ?? "unknown"}",
        $"Crafting Bench\n{GetCraftedBulletDetails(bullet)}\nDrag back to Player Inventory to remove it from the bench.");
      RegisterDragSource(benchBullet, $"command-icon-bullet-{definition?.Pattern.ToString().ToLowerInvariant() ?? "unknown"}", () => BeginBulletDrag(bullet.Id));
      craftingBenchSlot.Add(benchBullet);
      PopulateBulletDetails(craftingBenchDetails, inventory, bullet, null);
    }

    private void PlaceBulletInCraftingBench()
    {
      if (string.IsNullOrWhiteSpace(draggedCraftedBulletId))
        return;

      var inventory = BulletInventoryService.Instance;
      var magazineSlotIndex = GetMagazineSlotIndex(inventory, draggedCraftedBulletId);
      if (magazineSlotIndex >= 0 && !inventory.TryUnequip(magazineSlotIndex, out var error))
      {
        Refresh(error);
        ClearDrag();
        return;
      }

      craftingBenchBulletId = draggedCraftedBulletId;
      Refresh("Bullet base placed on the crafting bench.");
      ClearDrag();
    }

    private bool ReturnDraggedBulletToInventory()
    {
      if (draggedCraftedBulletId == craftingBenchBulletId)
      {
        craftingBenchBulletId = null;
        Refresh("Bullet base returned to Player Inventory.");
        ClearDrag();
        return true;
      }

      var magazineSlotIndex = GetMagazineSlotIndex(BulletInventoryService.Instance, draggedCraftedBulletId);
      if (magazineSlotIndex < 0)
        return false;

      if (BulletInventoryService.Instance.TryUnequip(magazineSlotIndex, out var error))
        Refresh($"Bullet returned to Player Inventory from magazine slot {magazineSlotIndex + 1}.");
      else
        Refresh(error);
      ClearDrag();
      return true;
    }

    private void MoveDraggedBullet(int slotIndex)
    {
      if (string.IsNullOrWhiteSpace(draggedCraftedBulletId))
        return;
      if (BulletInventoryService.Instance.TryMoveEquippedBullet(slotIndex, draggedCraftedBulletId, out var error))
      {
        if (draggedCraftedBulletId == craftingBenchBulletId)
          craftingBenchBulletId = null;
        Refresh($"Magazine slot {slotIndex + 1} updated.");
      }
      else
        Refresh(error);
      ClearDrag();
    }

    private static int GetMagazineSlotIndex(BulletInventoryService inventory, string bulletId)
    {
      if (inventory == null || string.IsNullOrWhiteSpace(bulletId))
        return -1;

      for (var slotIndex = 0; slotIndex < BulletInventoryService.EquippedSlotCount; slotIndex++)
        if (inventory.GetEquippedBulletId(slotIndex) == bulletId)
          return slotIndex;
      return -1;
    }

    private void ApplyDraggedCraftingMaterialToBench()
    {
      if (!draggedInventoryItem.HasValue)
        return;
      if (string.IsNullOrWhiteSpace(craftingBenchBulletId))
      {
        ClearDrag();
        return;
      }

      if (BulletInventoryService.Instance.TryApplyCraftingMaterial(craftingBenchBulletId, draggedInventoryItem.Value, out var error))
      {
        selectedCraftedBulletId = craftingBenchBulletId;
        selectedInventoryItem = null;
        Refresh("Crafting material applied to the crafting bench bullet.");
      }
      else
        Refresh(error);
      ClearDrag();
    }

    private void ConvertLoot(string recipeId)
    {
      if (BulletInventoryService.Instance.TryConvertLoot(recipeId, out var error))
        Refresh("Inventory items converted.");
      else
        Refresh(error);
    }

    private void SelectCraftedBullet(string bulletId)
    {
      selectedCraftedBulletId = bulletId;
      selectedInventoryItem = null;
      Refresh("Bullet selected. Drag it to set its magazine position.");
    }

    private void SelectInventoryItem(LootType type)
    {
      selectedInventoryItem = type;
      selectedCraftedBulletId = null;
      Refresh($"{type} selected.");
    }

    private void SalvageBullet(string bulletId)
    {
      if (BulletInventoryService.Instance.TrySalvage(bulletId, out var error))
      {
        if (selectedCraftedBulletId == bulletId)
          selectedCraftedBulletId = null;
        Refresh("Salvaged bullet and recovered 50% of its spent inventory items.");
      }
      else
        Refresh(error);
    }

    private static string GetCraftedBulletDetails(CraftedBulletSave crafted)
    {
      var definition = BulletCatalog.FindDefinition(crafted.DefinitionId);
      var details = new StringBuilder(definition?.DisplayName ?? "UNKNOWN BULLET");
      details.Append("\n").Append(definition?.Description);
      details.Append("\nModifiers: ").Append(crafted.Modifiers.Count);
      foreach (var savedModifier in crafted.Modifiers)
      {
        var modifier = BulletCatalog.FindModifier(savedModifier.ModifierId);
        if (modifier != null)
          details.Append("\n• ").Append(modifier.DisplayName);
      }
      return details.ToString();
    }

    private void PopulateBulletDetails(
      VisualElement detailsContainer,
      BulletInventoryService inventory,
      CraftedBulletSave bullet,
      string emptyMessage)
    {
      if (detailsContainer == null)
        return;

      detailsContainer.Clear();
      if (bullet == null || inventory == null || !inventory.TryBuildMagazineSlot(bullet.Id, out var slot))
      {
        if (string.IsNullOrEmpty(emptyMessage))
          return;
        detailsContainer.Add(new Label(emptyMessage)
        {
          name = "command-crafting-bench-empty-details"
        });
        return;
      }

      var definition = BulletCatalog.FindDefinition(bullet.DefinitionId);
      detailsContainer.Add(CreateCraftingBenchDetail($"{definition?.DisplayName ?? "UNKNOWN BULLET"}\n{definition?.Description}", true));

      var stats = slot.CraftedStats;
      var infoTab = new VisualElement();
      infoTab.AddToClassList("command-crafting-bench-info-tab");
      infoTab.Add(CreateCraftingBenchStat("Damage", stats.Damage.ToString("0.##")));
      infoTab.Add(CreateCraftingBenchStat("Speed", stats.Speed.ToString("0.##")));
      infoTab.Add(CreateCraftingBenchStat("Size", stats.Size.ToString("0.##")));
      infoTab.Add(CreateCraftingBenchStat("Projectiles", stats.ProjectileCount.ToString()));
      if (slot.Pattern == BulletFiringPattern.Burst)
        infoTab.Add(CreateCraftingBenchStat("Burst count", stats.BurstCount.ToString()));
      infoTab.Add(CreateCraftingBenchStat("Knockback", stats.Knockback.ToString("0.##")));
      if (stats.FireDamagePerSecond > 0f)
        infoTab.Add(CreateCraftingBenchStat("Fire", $"{stats.FireDamagePerSecond:0.##} DPS / {stats.FireDuration:0.##}s"));
      if (stats.PoisonDamagePerSecond > 0f)
        infoTab.Add(CreateCraftingBenchStat("Poison", $"{stats.PoisonDamagePerSecond:0.##} DPS / {stats.PoisonDuration:0.##}s"));
      detailsContainer.Add(infoTab);

      if (bullet.Modifiers.Count > 0)
        detailsContainer.Add(CreateCraftingBenchDetail("ATTRIBUTES", true));
      foreach (var savedModifier in bullet.Modifiers)
      {
        var modifier = BulletCatalog.FindModifier(savedModifier.ModifierId);
        if (modifier != null)
          detailsContainer.Add(CreateCraftingBenchDetail(GetAttributeDetails(modifier, savedModifier)));
      }
    }

    private string GetAttributeDetails(BulletModifierDefinition modifier, CraftedBulletModifierSave savedModifier)
    {
      var appliedEffect = modifier.RollTiming == ModifierRollTiming.OnCrafting
        ? GetModifierEffectDescription(modifier, savedModifier.RolledValue)
        : GetModifierEffectDescription(modifier, modifier.MinimumValue, modifier.MaximumValue);
      if (!showAttributeDefinitions)
        return $"{modifier.DisplayName}\n{appliedEffect}";

      return $"{modifier.DisplayName}\n{appliedEffect}\n({GetModifierEffectDescription(modifier, modifier.MinimumValue, modifier.MaximumValue)})";
    }

    private static string GetModifierEffectDescription(BulletModifierDefinition modifier, float value, float maximumValue = float.NaN)
    {
      var isRange = !float.IsNaN(maximumValue);
      var amount = isRange
        ? $"+{FormatModifierValue(modifier.Kind, value)} to +{FormatModifierValue(modifier.Kind, maximumValue)}"
        : $"+{FormatModifierValue(modifier.Kind, value)}";

      return modifier.Kind switch
      {
        BulletModifierKind.Damage => $"{amount} damage.",
        BulletModifierKind.SpeedPercent => $"{amount}% speed.",
        BulletModifierKind.Size => $"{amount} projectile size.",
        BulletModifierKind.ProjectileCount => $"{amount} pellets for shotgun rounds.",
        BulletModifierKind.BurstCount => $"{amount} timed burst shots.",
        BulletModifierKind.Knockback => $"{amount} knockback.",
        BulletModifierKind.FireDamageOverTime => $"{amount} fire DPS for {modifier.EffectDuration:0.##} seconds.",
        BulletModifierKind.PoisonDamageOverTime => $"{amount} poison DPS for {modifier.EffectDuration:0.##} seconds.",
        _ => modifier.Description
      };
    }

    private static string FormatModifierValue(BulletModifierKind kind, float value)
    {
      return kind is BulletModifierKind.ProjectileCount or BulletModifierKind.BurstCount
        ? Mathf.RoundToInt(value).ToString()
        : value.ToString("0.##");
    }

    private static VisualElement CreateCraftingBenchStat(string title, string value)
    {
      var stat = new VisualElement();
      stat.AddToClassList("command-crafting-bench-stat");
      stat.Add(new Label(title));
      var valueLabel = new Label(value);
      valueLabel.AddToClassList("command-crafting-bench-stat-value");
      stat.Add(valueLabel);
      return stat;
    }

    private static Label CreateCraftingBenchDetail(string text, bool isHeader = false)
    {
      var detail = new Label(text);
      detail.AddToClassList("command-crafting-bench-detail");
      if (isHeader)
        detail.AddToClassList("command-crafting-bench-detail-header");
      return detail;
    }

    private static string GetLootDetails(LootType type, int itemCount)
    {
      var craftingMaterial = BulletCatalog.FindCraftingMaterial(type);
      return craftingMaterial == null
        ? $"{type}\nAvailable: {itemCount}\nUsed by refining recipes."
        : $"{craftingMaterial.DisplayName}\nAvailable: {itemCount}\n{craftingMaterial.Description}\nDrag onto the bullet in the crafting bench to apply its effect.";
    }

    private void ClearDrag()
    {
      if (dragSource != null && dragPointerId >= 0 && dragSource.HasPointerCapture(dragPointerId))
        dragSource.ReleasePointer(dragPointerId);
      dragSource = null;
      dragPointerId = -1;
      if (dragGhost != null)
      {
        dragGhost.Clear();
        dragGhost.style.display = DisplayStyle.None;
      }
      draggedCraftedBulletId = null;
      draggedInventoryItem = null;
    }

    private static string FormatLootCost(ResourceCost[] costs)
    {
      if (costs == null || costs.Length == 0)
        return "FREE";
      var text = new StringBuilder();
      foreach (var cost in costs)
      {
        if (text.Length > 0)
          text.Append(", ");
        text.Append(cost.Amount).Append(' ').Append(cost.Type);
      }
      return text.ToString();
    }

  }
}
