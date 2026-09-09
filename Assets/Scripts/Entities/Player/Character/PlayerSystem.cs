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
      TryTriggerDash(ref state);
    }

    private void TryTriggerDash(ref SystemState state)
    {
      var direction = SystemAPI.GetSingleton<InputState>().ArrowReleaseDirection;
      if (math.lengthsq(direction) == 0f)
        return;

      var player = SystemAPI.GetSingletonEntity<PlayerAttributes>();
      if (SystemAPI.HasComponent<PlayerDefeated>(player))
        return;

      SkillTriggerEvent.Trigger(
        SystemAPI.GetBuffer<SkillTriggerEvent>(player),
        SkillType.Dash,
        direction);
    }
  }
}
