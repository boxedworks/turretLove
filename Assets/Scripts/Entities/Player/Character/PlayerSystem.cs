using Assets.Scripts.Input;
using Assets.Scripts.Entities.Skills;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Player.Character
{
  [UpdateBefore(typeof(SkillSystem))]
  public partial struct PlayerSystem : ISystem
  {
    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<InputState>();
      state.RequireForUpdate<PlayerAttributes>();
      state.RequireForUpdate<SkillTriggerEvent>();
    }

    public void OnUpdate(ref SystemState state)
    {
      var player = SystemAPI.GetSingletonEntity<PlayerAttributes>();
      if (SystemAPI.HasComponent<PlayerDefeated>(player))
        return;

      var input = SystemAPI.GetSingleton<InputState>();
      TryTriggerDash(ref state, player, input);
      TryTriggerHeldSkills(ref state, player, input);
    }

    private void TryTriggerDash(ref SystemState state, Entity player, InputState input)
    {
      if (math.lengthsq(input.ArrowReleaseDirection) == 0f)
        return;

      SkillTriggerEvent.Trigger(
        SystemAPI.GetBuffer<SkillTriggerEvent>(player),
        new SkillTriggerEvent
        {
          Type = SkillType.Dash,
          Direction = input.ArrowReleaseDirection
        });
    }

    private void TryTriggerHeldSkills(ref SystemState state, Entity player, InputState input)
    {
      if (input.SpaceState != InputButtonState.Pressed && input.SpaceState != InputButtonState.Held)
        return;

      var skills = SystemAPI.GetBuffer<Skill>(player);
      var triggerEvents = SystemAPI.GetBuffer<SkillTriggerEvent>(player);
      for (var skillIndex = 0; skillIndex < skills.Length; skillIndex++)
      {
        var skill = skills[skillIndex];
        if (skill.ActivationType == SkillActivationType.Held)
          SkillTriggerEvent.Trigger(triggerEvents, new SkillTriggerEvent { Type = skill.Type });
      }
    }
  }
}
