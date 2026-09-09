using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Skills
{
  public enum SkillType : byte
  {
    Dash
  }

  public struct Skill : IBufferElementData
  {
    public SkillType Type;
    public int MaxUses;
    public int RemainingUses;
    public float EffectStrength;
    // A non-positive duration disables recharging for this skill.
    public float RechargeDuration;
    public float RechargeElapsed;
  }

  public struct SkillTriggerEvent : IBufferElementData
  {
    public SkillType Type;
    public float2 Direction;

    public static void Trigger(DynamicBuffer<SkillTriggerEvent> triggerBuffer, SkillType type, float2 direction)
    {
      triggerBuffer.Add(new SkillTriggerEvent
      {
        Type = type,
        Direction = direction
      });
    }
  }
}
