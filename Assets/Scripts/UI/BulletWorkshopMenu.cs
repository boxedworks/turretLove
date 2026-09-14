using System;
using System.Text;
using Assets.Scripts.Bullets;
using Assets.Scripts.Entities.Loot;
using Assets.Scripts.Entities.Player.Turret;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
  internal sealed class BulletWorkshopMenu
  {
    private readonly VisualElement overlay;
    private readonly Button closeButton;
    private readonly VisualElement resourceList;
    private readonly VisualElement recipeList;
    private readonly VisualElement bulletList;
    private readonly VisualElement modifierList;
    private readonly VisualElement equipmentSlots;
    private readonly Label bulletDetails;
    private readonly Label statusLabel;
    private readonly Label titleLabel;
    private readonly Action closeRequested;
    private string selectedCraftedBulletId;
    private bool isGameRunning;

    public bool IsVisible => overlay != null && overlay.resolvedStyle.display != DisplayStyle.None;

    public BulletWorkshopMenu(
      VisualElement overlay,
      Button closeButton,
      VisualElement resourceList,
      VisualElement recipeList,
      VisualElement bulletList,
      VisualElement modifierList,
      VisualElement equipmentSlots,
      Label bulletDetails,
      Label statusLabel,
      Label titleLabel,
      Action closeRequested)
    {
      this.overlay = overlay;
      this.closeButton = closeButton;
      this.resourceList = resourceList;
      this.recipeList = recipeList;
      this.bulletList = bulletList;
      this.modifierList = modifierList;
      this.equipmentSlots = equipmentSlots;
      this.bulletDetails = bulletDetails;
      this.statusLabel = statusLabel;
      this.titleLabel = titleLabel;
      this.closeRequested = closeRequested;
    }

    public void RegisterCallbacks()
    {
      closeButton?.RegisterCallback<ClickEvent>(OnCloseClicked);
    }

    public void UnregisterCallbacks()
    {
      closeButton?.UnregisterCallback<ClickEvent>(OnCloseClicked);
    }

    public void Show(bool gameRunning)
    {
      isGameRunning = gameRunning;
      SetVisible(true);
      if (titleLabel != null)
        titleLabel.text = isGameRunning ? "FIELD WORKSHOP" : "COMMAND CENTER // BULLET WORKSHOP";
      if (closeButton != null)
        closeButton.text = isGameRunning ? "CLOSE" : "RETURN";
      Refresh();
    }

    public void Hide()
    {
      SetVisible(false);
    }

    public void Refresh(string status = null)
    {
      if (resourceList == null || recipeList == null || bulletList == null || modifierList == null || equipmentSlots == null)
        return;

      var inventory = BulletInventoryService.Instance;
      if (!inventory.HasActiveSave)
      {
        resourceList.Clear();
        recipeList.Clear();
        bulletList.Clear();
        modifierList.Clear();
        equipmentSlots.Clear();
        if (statusLabel != null)
          statusLabel.text = "Select a save slot before using the workshop.";
        return;
      }

      if (inventory.FindCraftedBullet(selectedCraftedBulletId) == null)
        selectedCraftedBulletId = null;

      resourceList.Clear();
      foreach (LootType type in Enum.GetValues(typeof(LootType)))
      {
        if (type == LootType.None)
          continue;
        var resource = new Label($"{type.ToString().ToUpperInvariant()}: {inventory.GetResourceCount(type)}") { tooltip = type.ToString() };
        resource.AddToClassList("workshop-resource");
        resourceList.Add(resource);
      }

      recipeList.Clear();
      foreach (var definition in BulletCatalog.Definitions)
      {
        var recipeId = definition.Id;
        recipeList.Add(CreateActionButton($"CRAFT {definition.DisplayName}\n{FormatCost(definition.CraftCost)}", () => CraftBullet(recipeId)));
      }

      bulletList.Clear();
      foreach (var crafted in inventory.CraftedBullets)
      {
        var craftedId = crafted.Id;
        var definition = BulletCatalog.FindDefinition(crafted.DefinitionId);
        var button = CreateActionButton(
          $"{(craftedId == selectedCraftedBulletId ? "▶ " : string.Empty)}{definition?.DisplayName ?? "Unknown Bullet"}  [{crafted.Modifiers.Count} mods]",
          () => SelectCraftedBullet(craftedId));
        if (craftedId == selectedCraftedBulletId)
          button.AddToClassList("workshop-bullet-selected");
        bulletList.Add(button);
      }

      UpdateDetails(inventory);
      modifierList.Clear();
      foreach (var modifier in BulletCatalog.Modifiers)
      {
        var modifierId = modifier.Id;
        modifierList.Add(CreateActionButton(
          $"APPLY {modifier.DisplayName}\n{modifier.Description}  {FormatCost(modifier.ApplyCost)}",
          () => ApplyModifier(modifierId)));
      }

      equipmentSlots.Clear();
      for (var slotIndex = 0; slotIndex < BulletInventoryService.EquippedSlotCount; slotIndex++)
        AddEquipmentSlot(inventory, slotIndex);

      if (status != null && statusLabel != null)
        statusLabel.text = status;
    }

    private void SetVisible(bool visible)
    {
      if (overlay != null)
        overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SelectCraftedBullet(string craftedBulletId)
    {
      selectedCraftedBulletId = craftedBulletId;
      Refresh("Bullet selected. Apply a modifier or equip it in an open slot.");
    }

    private void CraftBullet(string definitionId)
    {
      if (BulletInventoryService.Instance.TryCraft(definitionId, out var craftedBulletId, out var error))
      {
        selectedCraftedBulletId = craftedBulletId;
        Refresh("Crafted and selected.");
      }
      else
        Refresh(error);
    }

    private void ApplyModifier(string modifierId)
    {
      if (BulletInventoryService.Instance.TryApplyModifier(selectedCraftedBulletId, modifierId, out var error))
        Refresh("Modifier installed. Craft-time values are now locked in.");
      else
        Refresh(error);
    }

    private void AddEquipmentSlot(BulletInventoryService inventory, int slotIndex)
    {
      var row = new VisualElement();
      row.AddToClassList("workshop-slot-row");
      var currentId = inventory.GetEquippedBulletId(slotIndex);
      var current = inventory.FindCraftedBullet(currentId);
      var definition = current == null ? null : BulletCatalog.FindDefinition(current.DefinitionId);
      var slot = CreateActionButton($"{slotIndex + 1}. {definition?.DisplayName ?? "EMPTY"}", () => EquipSelectedBullet(slotIndex));
      slot.tooltip = "Equip the selected crafted bullet in this slot.";
      row.Add(slot);

      if (current != null)
      {
        var unequip = new Button(() => UnequipBullet(slotIndex)) { text = "UNEQUIP" };
        unequip.AddToClassList("workshop-unequip-button");
        row.Add(unequip);
      }
      equipmentSlots.Add(row);
    }

    private void EquipSelectedBullet(int slotIndex)
    {
      if (BulletInventoryService.Instance.TryEquip(slotIndex, selectedCraftedBulletId, out var error))
        Refresh($"Equipped selected bullet in slot {slotIndex + 1}. It is used after the next reload.");
      else
        Refresh(error);
    }

    private void UnequipBullet(int slotIndex)
    {
      if (BulletInventoryService.Instance.TryUnequip(slotIndex, out var error))
        Refresh($"Unequipped slot {slotIndex + 1}. Empty slots are consumed as dry shots.");
      else
        Refresh(error);
    }

    private void UpdateDetails(BulletInventoryService inventory)
    {
      if (bulletDetails == null)
        return;
      var crafted = inventory.FindCraftedBullet(selectedCraftedBulletId);
      if (crafted == null)
      {
        bulletDetails.text = "Select a crafted bullet to inspect, modify, or equip it.";
        return;
      }

      var definition = BulletCatalog.FindDefinition(crafted.DefinitionId);
      if (definition == null)
      {
        bulletDetails.text = "This crafted bullet references a missing catalog definition.";
        return;
      }

      var details = new StringBuilder();
      details.Append(definition.DisplayName).Append(": ").Append(definition.Description);
      details.Append("\nPattern: ").Append(definition.Pattern);
      details.Append(" | Base damage ").Append(definition.BaseStats.Damage.ToString("0.0"));
      details.Append(" | Speed ").Append(definition.BaseStats.Speed.ToString("0.0"));
      if (crafted.Modifiers.Count == 0)
        details.Append("\nNo modifiers installed.");
      else
      {
        details.Append("\nModifiers:");
        foreach (var savedModifier in crafted.Modifiers)
        {
          var modifier = BulletCatalog.FindModifier(savedModifier.ModifierId);
          if (modifier == null)
            continue;
          details.Append("\n• ").Append(modifier.DisplayName).Append(" — ");
          if (modifier.RollTiming == ModifierRollTiming.OnCrafting)
            details.Append("crafted roll ").Append(savedModifier.RolledValue.ToString("0.##"));
          else
            details.Append("rolls on every fire (").Append(modifier.MinimumValue.ToString("0.##")).Append("–")
              .Append(modifier.MaximumValue.ToString("0.##")).Append(")");
        }
      }
      bulletDetails.text = details.ToString();
    }

    private static Button CreateActionButton(string text, Action action)
    {
      var button = new Button(action) { text = text };
      button.AddToClassList("workshop-action-button");
      return button;
    }

    private static string FormatCost(ResourceCost[] costs)
    {
      if (costs == null || costs.Length == 0)
        return "FREE";

      var text = new StringBuilder("Cost: ");
      for (var index = 0; index < costs.Length; index++)
      {
        if (index > 0)
          text.Append(", ");
        text.Append(costs[index].Amount).Append(' ').Append(costs[index].Type);
      }
      return text.ToString();
    }

    private void OnCloseClicked(ClickEvent clickEvent)
    {
      closeRequested?.Invoke();
    }
  }
}
