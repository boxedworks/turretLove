using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Skills
{
  public partial struct SkillSystem : ISystem
  {
    [BurstCompile]
    partial struct ActivateSkillsJob : IJobEntity
    {
      public float DeltaTime;

      public readonly void Execute(
        ref DynamicBuffer<Skill> skills,
        ref DynamicBuffer<SkillTriggerEvent> triggerEvents,
        ref PhysicsVelocity velocity,
        in PhysicsMass mass,
        in LocalTransform transform)
      {
        for (var skillIndex = 0; skillIndex < skills.Length; skillIndex++)
        {
          var skill = skills[skillIndex];
          Recharge(ref skill);
          skills[skillIndex] = skill;
        }

        foreach (var triggerEvent in triggerEvents)
        {
          for (var skillIndex = 0; skillIndex < skills.Length; skillIndex++)
          {
            var skill = skills[skillIndex];
            if (skill.Type != triggerEvent.Type || skill.RemainingUses <= 0)
              continue;

            if (skill.Type == SkillType.Dash && math.lengthsq(triggerEvent.Direction) > 0f)
            {
              velocity.Linear = float3.zero;
              velocity.ApplyImpulse(
                mass,
                transform.Position,
                transform.Rotation,
                new float3(math.normalize(triggerEvent.Direction) * skill.EffectStrength, 0f),
                transform.Position);
            }

            skill.RemainingUses--;
            skill.RechargeElapsed = 0f;
            skills[skillIndex] = skill;

            break;
          }
        }

        triggerEvents.Clear();
      }

      private readonly void Recharge(ref Skill skill)
      {
        if (skill.RechargeDuration <= 0f || skill.MaxUses <= skill.RemainingUses)
        {
          skill.RechargeElapsed = 0f;
          return;
        }

        skill.RechargeElapsed += DeltaTime;
        var usesToRecharge = (int)(skill.RechargeElapsed / skill.RechargeDuration);
        if (usesToRecharge == 0)
          return;

        usesToRecharge = math.min(usesToRecharge, skill.MaxUses - skill.RemainingUses);
        skill.RemainingUses += usesToRecharge;
        skill.RechargeElapsed -= usesToRecharge * skill.RechargeDuration;
        if (skill.RemainingUses == skill.MaxUses)
          skill.RechargeElapsed = 0f;
      }
    }

    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<Skill>();
      state.RequireForUpdate<SkillTriggerEvent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.Dependency = new ActivateSkillsJob
      {
        DeltaTime = SystemAPI.Time.DeltaTime
      }.ScheduleParallel(state.Dependency);
    }
  }
}
