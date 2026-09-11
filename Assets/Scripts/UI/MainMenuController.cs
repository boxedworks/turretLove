using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using System.Collections.Generic;
using Assets.Scripts.Entities.Game;
using Assets.Scripts.Entities.Player.Character;
using Assets.Scripts.Entities.Player.Turret;
using Assets.Scripts.Entities.Skills;
using Assets.Scripts.Input;
using Unity.Transforms;

namespace Assets.Scripts.UI
{
  public class MainMenuController : MonoBehaviour
  {
    private const float DashDirectionIndicatorRadius = 64f;
    private const float HeldDirectionIndicatorRadius = 112f;
    private const float DashDirectionIndicatorSize = 48f;

    [SerializeField] private PanelRenderer panelRenderer;
    private VisualElement menuScreen;
    private Label statusLabel;
    private Button playButton;
    private Button optionsButton;
    private Button exitButton;
    private VisualElement gameHud;
    private VisualElement turretHealthFill;
    private Label turretHealthLabel;
    private VisualElement skillBar;
    private Label dashDirectionIndicator;
    private Label heldDirectionIndicator;
    private VisualElement uiRoot;
    private bool isGameRunning;
    private readonly Dictionary<SkillType, Texture2D> skillIcons = new();
    private Texture2D rechargeOverlayTexture;

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
      playButton = root.Q<Button>("play-button");
      optionsButton = root.Q<Button>("options-button");
      exitButton = root.Q<Button>("exit-button");
      gameHud = root.Q<VisualElement>("game-hud");
      turretHealthFill = root.Q<VisualElement>("turret-health-fill");
      turretHealthLabel = root.Q<Label>("turret-health-label");
      skillBar = root.Q<VisualElement>("skill-bar");
      dashDirectionIndicator = root.Q<Label>("dash-direction-indicator");
      heldDirectionIndicator = root.Q<Label>("held-direction-indicator");

      playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
      optionsButton?.RegisterCallback<ClickEvent>(OnOptionsClicked);
      exitButton?.RegisterCallback<ClickEvent>(OnExitClicked);
      SetGameUiVisibility();
    }

    private void UnregisterButtonCallbacks()
    {
      playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
      optionsButton?.UnregisterCallback<ClickEvent>(OnOptionsClicked);
      exitButton?.UnregisterCallback<ClickEvent>(OnExitClicked);
    }

    private void OnPlayClicked(ClickEvent clickEvent)
    {
      isGameRunning = true;
      SetGameUiVisibility();
      StartGame();
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

    private void StartGame()
    {
      // Get the default world and send LevelStart event
      var world = World.DefaultGameObjectInjectionWorld;
      if (world == null || !world.IsCreated)
        return;

      // Get the existing LevelState singleton created by LevelLifecycleSystem
      var levelStateEntity = world.EntityManager.CreateEntityQuery(typeof(LevelState)).GetSingletonEntity();
      var eventBuffer = world.EntityManager.GetBuffer<LevelEvent>(levelStateEntity);
      eventBuffer.Add(new LevelEvent { Type = LevelEvent.EventType.LevelStart });
    }

    private void Update()
    {
      if (!isGameRunning)
        return;

      var world = World.DefaultGameObjectInjectionWorld;
      if (world == null || !world.IsCreated)
        return;

      UpdateTurretHealth(world.EntityManager);
      UpdateSkills(world.EntityManager);
    }

    private void SetGameUiVisibility()
    {
      if (menuScreen != null)
        menuScreen.style.display = isGameRunning ? DisplayStyle.None : DisplayStyle.Flex;
      if (gameHud != null)
        gameHud.style.display = isGameRunning ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateTurretHealth(EntityManager entityManager)
    {
      if (turretHealthFill == null || turretHealthLabel == null)
        return;

      var turretQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TurretHealth>());
      if (turretQuery.IsEmptyIgnoreFilter)
      {
        turretHealthLabel.text = "-- / --";
        turretHealthFill.style.width = Length.Percent(0f);
      }
      else
      {
        var health = turretQuery.GetSingleton<TurretHealth>();
        turretHealthLabel.text = $"{health.CurrentHealth:0} / {health.MaxHealth:0}";
        var healthPercentage = health.MaxHealth > 0f
          ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) * 100f
          : 0f;
        turretHealthFill.style.width = Length.Percent(healthPercentage);
      }

      turretQuery.Dispose();
    }

    private void UpdateSkills(EntityManager entityManager)
    {
      if (skillBar == null)
        return;

      var playerQuery = entityManager.CreateEntityQuery(
        ComponentType.ReadOnly<PlayerAttributes>(),
        ComponentType.ReadOnly<Skill>(),
        ComponentType.ReadOnly<LocalTransform>());
      if (playerQuery.IsEmptyIgnoreFilter)
      {
        skillBar.Clear();
        HideDirectionIndicators();
        playerQuery.Dispose();
        return;
      }

      var player = playerQuery.GetSingletonEntity();
      var skills = entityManager.GetBuffer<Skill>(player, true);
      UpdateDirectionIndicators(skills, entityManager.GetComponentData<LocalTransform>(player).Position, entityManager);
      if (skillBar.childCount != skills.Length)
        CreateSkillSlots(skills.Length);

      for (var skillIndex = 0; skillIndex < skills.Length; skillIndex++)
      {
        var skill = skills[skillIndex];
        var slot = skillBar[skillIndex];
        slot.tooltip = $"{skill.Type}: {skill.RemainingUses} / {skill.MaxUses} uses";
        slot.Q<VisualElement>("skill-icon").style.backgroundImage = GetSkillIcon(skill.Type);
        slot.Q<Label>("skill-uses").text = skill.RemainingUses.ToString();

        var rechargeOverlay = slot.Q<VisualElement>("skill-recharge-overlay");
        var hasRecharge = skill.RechargeDuration > 0f && skill.RemainingUses < skill.MaxUses;
        rechargeOverlay.style.display = hasRecharge ? DisplayStyle.Flex : DisplayStyle.None;
        if (hasRecharge)
        {
          var rechargeProgress = Mathf.Clamp01(skill.RechargeElapsed / skill.RechargeDuration);
          rechargeOverlay.style.height = Length.Percent((1f - rechargeProgress) * 100f);
        }
      }

      playerQuery.Dispose();
    }

    private void UpdateDirectionIndicators(
      DynamicBuffer<Skill> skills,
      Unity.Mathematics.float3 playerPosition,
      EntityManager entityManager)
    {
      if (uiRoot == null)
        return;

      var dashDirection = Vector2.zero;
      for (var skillIndex = 0; skillIndex < skills.Length; skillIndex++)
      {
        var skill = skills[skillIndex];
        if (skill.Type != SkillType.Dash || !skill.IsCharging)
          continue;

        dashDirection = new Vector2(skill.ChargeDirection.x, skill.ChargeDirection.y);
        break;
      }

      var heldDirection = GetHeldArrowDirection(entityManager);

      var camera = Camera.main;
      if (camera == null)
      {
        HideDirectionIndicators();
        return;
      }

      var playerScreenPosition = camera.WorldToScreenPoint(new Vector3(playerPosition.x, playerPosition.y, playerPosition.z));
      if (playerScreenPosition.z < 0f || Screen.width == 0 || Screen.height == 0)
      {
        HideDirectionIndicators();
        return;
      }

      UpdateDirectionIndicator(
        dashDirectionIndicator,
        dashDirection,
        playerScreenPosition,
        DashDirectionIndicatorRadius);
      UpdateDirectionIndicator(
        heldDirectionIndicator,
        heldDirection,
        playerScreenPosition,
        HeldDirectionIndicatorRadius);
    }

    private void UpdateDirectionIndicator(
      Label indicator,
      Vector2 direction,
      Vector3 playerScreenPosition,
      float radius)
    {
      if (indicator == null)
        return;

      if (direction.sqrMagnitude == 0f)
      {
        indicator.style.display = DisplayStyle.None;
        return;
      }

      var indicatorScreenPosition = (Vector2)playerScreenPosition + direction.normalized * radius;
      indicator.style.left = Length.Pixels(indicatorScreenPosition.x / Screen.width * uiRoot.worldBound.width - DashDirectionIndicatorSize / 2f);
      indicator.style.top = Length.Pixels((1f - indicatorScreenPosition.y / Screen.height) * uiRoot.worldBound.height - DashDirectionIndicatorSize / 2f);
      indicator.text = GetDirectionIndicatorText(direction);
      indicator.style.display = DisplayStyle.Flex;
    }

    private static Vector2 GetHeldArrowDirection(EntityManager entityManager)
    {
      var inputQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<InputState>());
      if (inputQuery.IsEmptyIgnoreFilter)
      {
        inputQuery.Dispose();
        return Vector2.zero;
      }

      var input = inputQuery.GetSingleton<InputState>();
      inputQuery.Dispose();
      return new Vector2(
        (IsArrowKeyDown(input.ArrowRightState) ? 1f : 0f) - (IsArrowKeyDown(input.ArrowLeftState) ? 1f : 0f),
        (IsArrowKeyDown(input.ArrowUpState) ? 1f : 0f) - (IsArrowKeyDown(input.ArrowDownState) ? 1f : 0f));
    }

    private static bool IsArrowKeyDown(InputButtonState state)
    {
      return state == InputButtonState.Pressed || state == InputButtonState.Held;
    }

    private void HideDirectionIndicators()
    {
      if (dashDirectionIndicator != null)
        dashDirectionIndicator.style.display = DisplayStyle.None;
      if (heldDirectionIndicator != null)
        heldDirectionIndicator.style.display = DisplayStyle.None;
    }

    private static string GetDirectionIndicatorText(Unity.Mathematics.float2 direction)
    {
      if (direction.y > 0f)
        return direction.x > 0f ? "↗" : direction.x < 0f ? "↖" : "↑";
      if (direction.y < 0f)
        return direction.x > 0f ? "↘" : direction.x < 0f ? "↙" : "↓";

      return direction.x > 0f ? "→" : "←";
    }

    private void CreateSkillSlots(int skillCount)
    {
      skillBar.Clear();
      for (var skillIndex = 0; skillIndex < skillCount; skillIndex++)
      {
        var skillSlot = new VisualElement();
        skillSlot.AddToClassList("skill-slot");

        var skillIcon = new VisualElement { name = "skill-icon" };
        skillIcon.AddToClassList("skill-icon");

        var rechargeOverlay = new VisualElement { name = "skill-recharge-overlay" };
        rechargeOverlay.AddToClassList("skill-recharge-overlay");
        rechargeOverlay.style.backgroundImage = GetRechargeOverlayTexture();
        skillIcon.Add(rechargeOverlay);

        var skillUses = new Label { name = "skill-uses" };
        skillUses.AddToClassList("skill-uses");
        skillIcon.Add(skillUses);
        skillSlot.Add(skillIcon);

        skillBar.Add(skillSlot);
      }
    }

    private Texture2D GetSkillIcon(SkillType skillType)
    {
      if (!skillIcons.TryGetValue(skillType, out var icon))
      {
        icon = Resources.Load<Texture2D>($"Icons/Skills/{skillType}");
        skillIcons.Add(skillType, icon);
      }

      return icon;
    }

    private Texture2D GetRechargeOverlayTexture()
    {
      if (rechargeOverlayTexture != null)
        return rechargeOverlayTexture;

      const int gradientHeight = 64;
      rechargeOverlayTexture = new Texture2D(1, gradientHeight, TextureFormat.RGBA32, false);
      rechargeOverlayTexture.wrapMode = TextureWrapMode.Clamp;
      rechargeOverlayTexture.filterMode = FilterMode.Bilinear;

      var pixels = new Color[gradientHeight];
      for (var pixelIndex = 0; pixelIndex < gradientHeight; pixelIndex++)
      {
        var progress = pixelIndex / (float)(gradientHeight - 1);
        pixels[pixelIndex] = new Color(0.03f, 0.1f, 0.11f, Mathf.Lerp(0.88f, 0.3f, progress));
      }

      rechargeOverlayTexture.SetPixels(pixels);
      rechargeOverlayTexture.Apply();
      return rechargeOverlayTexture;
    }

    private void OnDestroy()
    {
      if (rechargeOverlayTexture != null)
        Destroy(rechargeOverlayTexture);
    }
  }
}
