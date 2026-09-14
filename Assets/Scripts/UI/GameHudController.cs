using System.Collections.Generic;
using Assets.Scripts.Entities.Enemy;
using Assets.Scripts.Entities.Player.Character;
using Assets.Scripts.Entities.Player.Turret;
using Assets.Scripts.Entities.Skills;
using Assets.Scripts.Input;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
  internal sealed class GameHudController
  {
    private const float DashDirectionIndicatorRadius = 64f;
    private const float HeldDirectionIndicatorRadius = 112f;
    private const float DashDirectionIndicatorSize = 48f;

    private readonly VisualElement hud;
    private readonly VisualElement turretHealthFill;
    private readonly Label turretHealthLabel;
    private readonly Label turretAmmoLabel;
    private readonly Label turretTargetLabel;
    private readonly Label turretTargetHealthLabel;
    private readonly VisualElement turretTargetOutline;
    private readonly VisualElement skillBar;
    private readonly Label dashDirectionIndicator;
    private readonly Label heldDirectionIndicator;
    private readonly VisualElement uiRoot;
    private readonly Dictionary<SkillType, Texture2D> skillIcons = new();
    private Texture2D rechargeOverlayTexture;

    public GameHudController(
      VisualElement hud,
      VisualElement turretHealthFill,
      Label turretHealthLabel,
      Label turretAmmoLabel,
      Label turretTargetLabel,
      Label turretTargetHealthLabel,
      VisualElement turretTargetOutline,
      VisualElement skillBar,
      Label dashDirectionIndicator,
      Label heldDirectionIndicator,
      VisualElement uiRoot)
    {
      this.hud = hud;
      this.turretHealthFill = turretHealthFill;
      this.turretHealthLabel = turretHealthLabel;
      this.turretAmmoLabel = turretAmmoLabel;
      this.turretTargetLabel = turretTargetLabel;
      this.turretTargetHealthLabel = turretTargetHealthLabel;
      this.turretTargetOutline = turretTargetOutline;
      this.skillBar = skillBar;
      this.dashDirectionIndicator = dashDirectionIndicator;
      this.heldDirectionIndicator = heldDirectionIndicator;
      this.uiRoot = uiRoot;
    }

    public void SetVisible(bool visible)
    {
      if (hud != null)
        hud.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void Update()
    {
      var world = World.DefaultGameObjectInjectionWorld;
      if (world == null || !world.IsCreated)
        return;

      var entityManager = world.EntityManager;
      UpdateTurretHealth(entityManager);
      UpdateTurretAmmo(entityManager);
      UpdateTurretTarget(entityManager);
      UpdateTurretTargetHealth(entityManager);
      UpdateTurretTargetOutline(entityManager);
      UpdateSkills(entityManager);
    }

    public void Dispose()
    {
      if (rechargeOverlayTexture != null)
        Object.Destroy(rechargeOverlayTexture);
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
        turretHealthFill.style.width = Length.Percent(health.MaxHealth > 0f
          ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) * 100f
          : 0f);
      }
      turretQuery.Dispose();
    }

    private void UpdateTurretAmmo(EntityManager entityManager)
    {
      if (turretAmmoLabel == null)
        return;

      var turretQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TurretAmmo>());
      turretAmmoLabel.text = turretQuery.IsEmptyIgnoreFilter
        ? "-- / --"
        : $"{turretQuery.GetSingleton<TurretAmmo>().CurrentAmmo} / {turretQuery.GetSingleton<TurretAmmo>().MagazineSize}";
      turretQuery.Dispose();
    }

    private void UpdateTurretTarget(EntityManager entityManager)
    {
      if (turretTargetLabel == null)
        return;

      var turretQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TurretAttributes>());
      if (turretQuery.IsEmptyIgnoreFilter)
        turretTargetLabel.text = "NONE";
      else
      {
        var target = turretQuery.GetSingleton<TurretAttributes>().CurrentTarget;
        turretTargetLabel.text = entityManager.Exists(target) && entityManager.HasComponent<SimpleEnemy>(target)
          ? entityManager.GetComponentData<SimpleEnemy>(target).Type.ToString().ToUpperInvariant()
          : "NONE";
      }
      turretQuery.Dispose();
    }

    private void UpdateTurretTargetHealth(EntityManager entityManager)
    {
      if (turretTargetHealthLabel == null)
        return;

      var turretQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TurretAttributes>());
      if (turretQuery.IsEmptyIgnoreFilter)
        turretTargetHealthLabel.text = "--";
      else
      {
        var target = turretQuery.GetSingleton<TurretAttributes>().CurrentTarget;
        turretTargetHealthLabel.text = entityManager.Exists(target) && entityManager.HasComponent<SimpleEnemy>(target)
          ? entityManager.GetComponentData<SimpleEnemy>(target).Health.ToString("0")
          : "--";
      }
      turretQuery.Dispose();
    }

    private void UpdateTurretTargetOutline(EntityManager entityManager)
    {
      if (turretTargetOutline == null || uiRoot == null)
        return;

      var turretQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TurretAttributes>());
      if (turretQuery.IsEmptyIgnoreFilter)
      {
        turretTargetOutline.style.display = DisplayStyle.None;
        turretQuery.Dispose();
        return;
      }

      var target = turretQuery.GetSingleton<TurretAttributes>().CurrentTarget;
      turretQuery.Dispose();
      if (!entityManager.Exists(target) || !entityManager.HasComponent<LocalTransform>(target))
      {
        turretTargetOutline.style.display = DisplayStyle.None;
        return;
      }

      var camera = Camera.main;
      var position = entityManager.GetComponentData<LocalTransform>(target).Position;
      var screenPosition = camera == null ? Vector3.back : camera.WorldToScreenPoint(new Vector3(position.x, position.y, position.z));
      if (screenPosition.z < 0f || Screen.width == 0 || Screen.height == 0)
      {
        turretTargetOutline.style.display = DisplayStyle.None;
        return;
      }

      const float outlineSize = 56f;
      turretTargetOutline.style.left = Length.Pixels(screenPosition.x / Screen.width * uiRoot.worldBound.width - outlineSize / 2f);
      turretTargetOutline.style.top = Length.Pixels((1f - screenPosition.y / Screen.height) * uiRoot.worldBound.height - outlineSize / 2f);
      turretTargetOutline.style.display = DisplayStyle.Flex;
    }

    private void UpdateSkills(EntityManager entityManager)
    {
      if (skillBar == null)
        return;

      var playerQuery = entityManager.CreateEntityQuery(
        ComponentType.ReadOnly<PlayerAttributes>(), ComponentType.ReadOnly<Skill>(), ComponentType.ReadOnly<LocalTransform>());
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

      for (var index = 0; index < skills.Length; index++)
      {
        var skill = skills[index];
        var slot = skillBar[index];
        slot.tooltip = $"{skill.Type}: {skill.RemainingUses} / {skill.MaxUses} uses";
        slot.Q<VisualElement>("skill-icon").style.backgroundImage = GetSkillIcon(skill.Type);
        slot.Q<Label>("skill-uses").text = skill.RemainingUses.ToString();
        var recharge = slot.Q<VisualElement>("skill-recharge-overlay");
        var isRecharging = skill.RechargeDuration > 0f && skill.RemainingUses < skill.MaxUses;
        recharge.style.display = isRecharging ? DisplayStyle.Flex : DisplayStyle.None;
        if (isRecharging)
          recharge.style.height = Length.Percent((1f - Mathf.Clamp01(skill.RechargeElapsed / skill.RechargeDuration)) * 100f);
      }
      playerQuery.Dispose();
    }

    private void UpdateDirectionIndicators(DynamicBuffer<Skill> skills, float3 playerPosition, EntityManager entityManager)
    {
      var dashDirection = Vector2.zero;
      for (var index = 0; index < skills.Length; index++)
      {
        if (skills[index].Type == SkillType.Dash && skills[index].IsCharging)
        {
          dashDirection = new Vector2(skills[index].ChargeDirection.x, skills[index].ChargeDirection.y);
          break;
        }
      }

      var camera = Camera.main;
      if (camera == null)
      {
        HideDirectionIndicators();
        return;
      }

      var screenPosition = camera.WorldToScreenPoint(new Vector3(playerPosition.x, playerPosition.y, playerPosition.z));
      if (screenPosition.z < 0f || Screen.width == 0 || Screen.height == 0)
      {
        HideDirectionIndicators();
        return;
      }

      UpdateDirectionIndicator(dashDirectionIndicator, dashDirection, screenPosition, DashDirectionIndicatorRadius);
      UpdateDirectionIndicator(heldDirectionIndicator, GetHeldArrowDirection(entityManager), screenPosition, HeldDirectionIndicatorRadius);
    }

    private void UpdateDirectionIndicator(Label indicator, Vector2 direction, Vector3 playerScreenPosition, float radius)
    {
      if (indicator == null)
        return;
      if (direction.sqrMagnitude == 0f)
      {
        indicator.style.display = DisplayStyle.None;
        return;
      }

      var position = (Vector2)playerScreenPosition + direction.normalized * radius;
      indicator.style.left = Length.Pixels(position.x / Screen.width * uiRoot.worldBound.width - DashDirectionIndicatorSize / 2f);
      indicator.style.top = Length.Pixels((1f - position.y / Screen.height) * uiRoot.worldBound.height - DashDirectionIndicatorSize / 2f);
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

    private static string GetDirectionIndicatorText(float2 direction)
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
      for (var index = 0; index < skillCount; index++)
      {
        var slot = new VisualElement();
        slot.AddToClassList("skill-slot");
        var icon = new VisualElement { name = "skill-icon" };
        icon.AddToClassList("skill-icon");
        var overlay = new VisualElement { name = "skill-recharge-overlay" };
        overlay.AddToClassList("skill-recharge-overlay");
        overlay.style.backgroundImage = GetRechargeOverlayTexture();
        icon.Add(overlay);
        var uses = new Label { name = "skill-uses" };
        uses.AddToClassList("skill-uses");
        icon.Add(uses);
        slot.Add(icon);
        skillBar.Add(slot);
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
      rechargeOverlayTexture = new Texture2D(1, gradientHeight, TextureFormat.RGBA32, false)
      {
        wrapMode = TextureWrapMode.Clamp,
        filterMode = FilterMode.Bilinear
      };
      var pixels = new Color[gradientHeight];
      for (var index = 0; index < gradientHeight; index++)
        pixels[index] = new Color(0.03f, 0.1f, 0.11f, Mathf.Lerp(0.88f, 0.3f, index / (float)(gradientHeight - 1)));
      rechargeOverlayTexture.SetPixels(pixels);
      rechargeOverlayTexture.Apply();
      return rechargeOverlayTexture;
    }
  }
}
