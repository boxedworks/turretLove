using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Skills
{
  public enum SkillType : byte
  {
    Dash,
    Halt
  }

  public enum SkillActivationType : byte
  {
    Release,
    Held
  }

  public struct Skill : IBufferElementData
  {
    public SkillType Type;
    public SkillActivationType ActivationType;
    public int MaxUses;
    public int RemainingUses;
    public float EffectStrength;
    // A non-positive duration disables recharging for this skill.
    public float RechargeDuration;
    public float RechargeElapsed;
    // A non-positive duration activates the skill immediately.
    public float ChargeDuration;
    public float ChargeElapsed;
    public float2 ChargeDirection;
    public bool IsCharging;
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
