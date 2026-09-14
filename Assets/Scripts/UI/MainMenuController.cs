using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Unity.Entities;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Bullets;
using Assets.Scripts.Entities.Game;
using Assets.Scripts.Entities.Enemy;
using Assets.Scripts.Entities.Loot;
using Assets.Scripts.Entities.Player.Character;
using Assets.Scripts.Entities.Player.Turret;
using Assets.Scripts.Entities.Skills;
using Assets.Scripts.Input;
using Unity.Transforms;

namespace Assets.Scripts.UI
{
  public class MainMenuController : MonoBehaviour
  {
    [SerializeField] private PanelRenderer panelRenderer;
    private VisualElement menuScreen;
    private Label statusLabel;
    private Button saveSelectionButton;
    private Button playButton;
    private Button optionsButton;
    private Button exitButton;
    private Button workshopCloseButton;
    private Button workshopOpenButton;
    private Button commandCenterBackButton;
    private Button commandCenterWorkshopButton;
    private Button commandCenterStartButton;
    private VisualElement gameHud;
    private VisualElement turretHealthFill;
    private Label turretHealthLabel;
    private Label turretAmmoLabel;
    private Label turretTargetLabel;
    private Label turretTargetHealthLabel;
    private VisualElement turretTargetOutline;
    private VisualElement skillBar;
    private Label dashDirectionIndicator;
    private Label heldDirectionIndicator;
    private VisualElement uiRoot;
    private VisualElement workshopOverlay;
    private VisualElement commandCenterOverlay;
    private VisualElement saveSelectionOverlay;
    private VisualElement saveSlotList;
    private Label saveSelectionMessage;
    private Button saveSelectionBackButton;
    private VisualElement workshopResourceList;
    private VisualElement workshopRecipeList;
    private VisualElement workshopBulletList;
    private VisualElement workshopModifierList;
    private VisualElement workshopEquipmentSlots;
    private Label workshopBulletDetails;
    private Label workshopStatus;
    private Label workshopTitle;
    private Label commandCenterSaveLabel;
    private Label commandCenterSelectionLabel;
    private Label commandCenterMessage;
    private bool isGameRunning;
    private BulletInventoryService subscribedInventory;
    private readonly List<Button> commandLevelButtons = new();
    private SaveSelectionMenu saveSelectionMenu;
    private CommandCenterMenu commandCenterMenu;
    private BulletWorkshopMenu workshopMenu;
    private GameHudController gameHudController;

    private void OnEnable()
    {
      if (panelRenderer != null)
      {
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
      }
    }

    private void OnDisable()
    {
      if (panelRenderer == null)
      {
        return;
      }

      panelRenderer.UnregisterUIReloadCallback(OnUIReload);
      UnregisterButtonCallbacks();
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
      uiRoot = root;
      menuScreen = root.Q<VisualElement>("menu-screen");
      statusLabel = root.Q<Label>("status-label");
      saveSelectionButton = root.Q<Button>("save-selection-button");
      playButton = root.Q<Button>("play-button");
      optionsButton = root.Q<Button>("options-button");
      exitButton = root.Q<Button>("exit-button");
      workshopCloseButton = root.Q<Button>("workshop-close-button");
      workshopOpenButton = root.Q<Button>("workshop-open-button");
      commandCenterBackButton = root.Q<Button>("command-center-back-button");
      commandCenterWorkshopButton = root.Q<Button>("command-center-workshop-button");
      commandCenterStartButton = root.Q<Button>("command-center-start-button");
      gameHud = root.Q<VisualElement>("game-hud");
      turretHealthFill = root.Q<VisualElement>("turret-health-fill");
      turretHealthLabel = root.Q<Label>("turret-health-label");
      turretAmmoLabel = root.Q<Label>("turret-ammo-label");
      turretTargetLabel = root.Q<Label>("turret-target-label");
      turretTargetHealthLabel = root.Q<Label>("turret-target-health-label");
      turretTargetOutline = root.Q<VisualElement>("turret-target-outline");
      skillBar = root.Q<VisualElement>("skill-bar");
      dashDirectionIndicator = root.Q<Label>("dash-direction-indicator");
      heldDirectionIndicator = root.Q<Label>("held-direction-indicator");
      workshopOverlay = root.Q<VisualElement>("workshop-overlay");
      commandCenterOverlay = root.Q<VisualElement>("command-center-overlay");
      saveSelectionOverlay = root.Q<VisualElement>("save-selection-overlay");
      saveSlotList = root.Q<VisualElement>("save-slot-list");
      saveSelectionMessage = root.Q<Label>("save-selection-message");
      saveSelectionBackButton = root.Q<Button>("save-selection-back-button");
      workshopResourceList = root.Q<VisualElement>("workshop-resource-list");
      workshopRecipeList = root.Q<VisualElement>("workshop-recipe-list");
      workshopBulletList = root.Q<VisualElement>("workshop-bullet-list");
      workshopModifierList = root.Q<VisualElement>("workshop-modifier-list");
      workshopEquipmentSlots = root.Q<VisualElement>("workshop-equipment-slots");
      workshopBulletDetails = root.Q<Label>("workshop-bullet-details");
      workshopStatus = root.Q<Label>("workshop-status");
      workshopTitle = root.Q<Label>(className: "workshop-title");
      commandCenterSaveLabel = root.Q<Label>("command-center-save-label");
      commandCenterSelectionLabel = root.Q<Label>("command-center-selection-label");
      commandCenterMessage = root.Q<Label>("command-center-message");
      commandLevelButtons.Clear();
      root.Query<Button>(className: "command-level-button").ToList(commandLevelButtons);

      saveSelectionButton?.RegisterCallback<ClickEvent>(OnSaveSelectionClicked);
      playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
      optionsButton?.RegisterCallback<ClickEvent>(OnOptionsClicked);
      exitButton?.RegisterCallback<ClickEvent>(OnExitClicked);
      workshopOpenButton?.RegisterCallback<ClickEvent>(OnWorkshopClicked);
      saveSelectionMenu = new SaveSelectionMenu(
        saveSelectionOverlay, saveSlotList, saveSelectionMessage, saveSelectionBackButton, UpdateMenuSaveStatus);
      commandCenterMenu = new CommandCenterMenu(
        commandCenterOverlay, commandCenterSaveLabel, commandCenterSelectionLabel, commandCenterMessage,
        commandCenterBackButton, commandCenterWorkshopButton, commandCenterStartButton, commandLevelButtons,
        CloseCommandCenter, OpenWorkshopFromCommandCenter, StartSelectedLevel);
      workshopMenu = new BulletWorkshopMenu(
        workshopOverlay, workshopCloseButton, workshopResourceList, workshopRecipeList, workshopBulletList,
        workshopModifierList, workshopEquipmentSlots, workshopBulletDetails, workshopStatus, workshopTitle,
        CloseWorkshop);
      gameHudController = new GameHudController(
        gameHud, turretHealthFill, turretHealthLabel, turretAmmoLabel, turretTargetLabel, turretTargetHealthLabel,
        turretTargetOutline, skillBar, dashDirectionIndicator, heldDirectionIndicator, uiRoot);
      saveSelectionMenu.RegisterCallbacks();
      commandCenterMenu.RegisterCallbacks();
      workshopMenu.RegisterCallbacks();
      SubscribeToInventory();
      SetGameUiVisibility();
      workshopMenu.Hide();
      commandCenterMenu.Hide();
      saveSelectionMenu.Hide();
      UpdateMenuSaveStatus();
    }

    private void UnregisterButtonCallbacks()
    {
      saveSelectionButton?.UnregisterCallback<ClickEvent>(OnSaveSelectionClicked);
      playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
      optionsButton?.UnregisterCallback<ClickEvent>(OnOptionsClicked);
      exitButton?.UnregisterCallback<ClickEvent>(OnExitClicked);
      workshopOpenButton?.UnregisterCallback<ClickEvent>(OnWorkshopClicked);
      saveSelectionMenu?.UnregisterCallbacks();
      commandCenterMenu?.UnregisterCallbacks();
      workshopMenu?.UnregisterCallbacks();
      commandLevelButtons.Clear();
      if (subscribedInventory != null)
        subscribedInventory.Changed -= OnInventoryChanged;
      subscribedInventory = null;
    }

    private void OnPlayClicked(ClickEvent clickEvent)
    {
      if (!BulletInventoryService.Instance.HasActiveSave)
      {
        saveSelectionMenu.Show("Select or create a save slot before playing.");
        return;
      }

      commandCenterMenu.Show();
    }

    private void OnOptionsClicked(ClickEvent clickEvent)
    {
      statusLabel.text = "Options will be available soon.";
    }

    private void OnExitClicked(ClickEvent clickEvent)
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    private void OnWorkshopClicked(ClickEvent clickEvent)
    {
      if (!BulletInventoryService.Instance.HasActiveSave)
      {
        saveSelectionMenu.Show("Select or create a save slot before opening the workshop.");
        return;
      }

      workshopMenu.Show(isGameRunning);
    }

    private void StartSelectedLevel(int areaIndex, int levelIndex)
    {
      if (!BulletInventoryService.Instance.HasActiveSave)
      {
        saveSelectionMenu.Show("Select or create a save slot before deploying.");
        return;
      }

      if (!StartGame(areaIndex, levelIndex))
      {
        commandCenterMenu.ShowMessage("Unable to start the level. Please try again.");
        return;
      }

      isGameRunning = true;
      commandCenterMenu.Hide();
      workshopMenu.Hide();
      saveSelectionMenu.Hide();
      SetGameUiVisibility();
    }

    private void OnSaveSelectionClicked(ClickEvent clickEvent)
    {
      saveSelectionMenu.Show();
    }

    private bool StartGame(int areaIndex, int levelIndex)
    {
      var world = World.DefaultGameObjectInjectionWorld;
      if (world == null || !world.IsCreated)
        return false;

      var levelStateQuery = world.EntityManager.CreateEntityQuery(typeof(LevelState));
      if (levelStateQuery.IsEmptyIgnoreFilter)
      {
        levelStateQuery.Dispose();
        return false;
      }

      var levelStateEntity = levelStateQuery.GetSingletonEntity();
      levelStateQuery.Dispose();
      var eventBuffer = world.EntityManager.GetBuffer<LevelEvent>(levelStateEntity);
      eventBuffer.Add(new LevelEvent
      {
        Type = LevelEvent.EventType.LevelStart,
        AreaIndex = areaIndex,
        LevelIndex = levelIndex
      });
      return true;
    }

    private void Update()
    {
      if (isGameRunning && Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
      {
        if (workshopMenu.IsVisible)
          workshopMenu.Hide();
        else
          workshopMenu.Show(true);
      }
      if (isGameRunning && workshopMenu.IsVisible
        && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        workshopMenu.Hide();

      if (!isGameRunning)
        return;

      gameHudController.Update();
    }

    private void SetGameUiVisibility()
    {
      if (menuScreen != null)
        menuScreen.style.display = isGameRunning ? DisplayStyle.None : DisplayStyle.Flex;
      gameHudController?.SetVisible(isGameRunning);
    }

    private void CloseWorkshop()
    {
      workshopMenu.Hide();
      if (!isGameRunning)
        commandCenterMenu.Show();
    }

    private void CloseCommandCenter()
    {
      commandCenterMenu.Hide();
    }

    private void OpenWorkshopFromCommandCenter()
    {
      commandCenterMenu.Hide();
      workshopMenu.Show(false);
    }

    private void UpdateMenuSaveStatus()
    {
      if (statusLabel == null)
        return;

      var inventory = BulletInventoryService.Instance;
      statusLabel.text = inventory.HasActiveSave
        ? $"ACTIVE SAVE: SLOT {inventory.ActiveSlotIndex + 1}"
        : "SELECT A SAVE SLOT TO PLAY.";
    }

    private void SubscribeToInventory()
    {
      var inventory = BulletInventoryService.Instance;
      if (subscribedInventory == inventory)
        return;
      if (subscribedInventory != null)
        subscribedInventory.Changed -= OnInventoryChanged;
      subscribedInventory = inventory;
      subscribedInventory.Changed += OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
      UpdateMenuSaveStatus();
      if (saveSelectionMenu.IsVisible)
        saveSelectionMenu.Refresh();
      if (commandCenterMenu.IsVisible)
        commandCenterMenu.Refresh();
      if (workshopMenu.IsVisible)
        workshopMenu.Refresh();
    }

    private void OnDestroy()
    {
      gameHudController?.Dispose();
    }
  }
}
