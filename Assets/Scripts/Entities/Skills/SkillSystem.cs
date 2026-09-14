using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Assets.Scripts.Entities.Player.Turret;

namespace Assets.Scripts.Entities.Skills
{
  public partial struct SkillSystem : ISystem
  {
    [BurstCompile]
    partial struct ActivateSkillsJob : IJobEntity
    {
      public float DeltaTime;
      public ComponentLookup<TurretAmmo> TurretAmmoLookup;

      public void Execute(
        ref DynamicBuffer<Skill> skills,
        ref DynamicBuffer<SkillTriggerEvent> triggerEvents,
        ref PhysicsVelocity velocity,
        in PhysicsMass mass)
      {
        for (var skillIndex = 0; skillIndex < skills.Length; skillIndex++)
        {
          var skill = skills[skillIndex];
          if (skill.IsCharging)
          {
            skill.ChargeElapsed += DeltaTime;
            if (skill.ChargeDuration <= 0f || skill.ChargeElapsed >= skill.ChargeDuration)
            {
              Fire(ref velocity, mass, skill, skill.ChargeDirection);
              if (skill.Type == SkillType.Reload)
                skill.TargetEntity = Entity.Null;
              skill.IsCharging = false;
              skill.ChargeElapsed = 0f;
              skill.RechargeElapsed = 0f;
            }
          }
          else
          {
            Recharge(ref skill);
          }

          skills[skillIndex] = skill;
        }

        foreach (var triggerEvent in triggerEvents)
        {
          for (var skillIndex = 0; skillIndex < skills.Length; skillIndex++)
          {
            var skill = skills[skillIndex];
            if (skill.Type != triggerEvent.Type)
              continue;

            if (triggerEvent.TargetEntity != Entity.Null)
              skill.TargetEntity = triggerEvent.TargetEntity;

            if (skill.IsCharging)
            {
              if (CanFire(skill, triggerEvent.Direction))
              {
                if (triggerEvent.Direction.x != 0f)
                  skill.ChargeDirection.x = triggerEvent.Direction.x;
                if (triggerEvent.Direction.y != 0f)
                  skill.ChargeDirection.y = triggerEvent.Direction.y;
                skills[skillIndex] = skill;
              }

              break;
            }

            if (skill.RemainingUses <= 0 || !CanFire(skill, triggerEvent.Direction))
              break;

            skill.RemainingUses--;
            skill.RechargeElapsed = 0f;
            if (skill.ChargeDuration > 0f)
            {
              skill.IsCharging = true;
              skill.ChargeElapsed = 0f;
              skill.ChargeDirection = triggerEvent.Direction;
            }
            else
            {
              Fire(ref velocity, mass, skill, triggerEvent.Direction);
              if (skill.Type == SkillType.Reload)
                skill.TargetEntity = Entity.Null;
            }

            skills[skillIndex] = skill;

            break;
          }
        }

        triggerEvents.Clear();
      }

      private static bool CanFire(in Skill skill, float2 direction)
      {
        return skill.Type == SkillType.Halt ||
          (skill.Type == SkillType.Reload && skill.TargetEntity != Entity.Null) ||
          (skill.Type == SkillType.Dash && math.lengthsq(direction) > 0f);
      }

      private void Fire(ref PhysicsVelocity velocity, in PhysicsMass mass, in Skill skill, float2 direction)
      {
        if (skill.Type == SkillType.Dash && math.lengthsq(direction) > 0f)
          velocity.ApplyLinearImpulse(mass, new float3(math.normalize(direction) * skill.EffectStrength, 0f));
        else if (skill.Type == SkillType.Halt)
          velocity.Linear = math.lerp(velocity.Linear, float3.zero, math.saturate(skill.EffectStrength));
        else if (skill.Type == SkillType.Reload && TurretAmmoLookup.HasComponent(skill.TargetEntity))
        {
          var ammo = TurretAmmoLookup[skill.TargetEntity];
          if (ammo.CurrentAmmo <= 0)
          {
            ammo.CurrentAmmo = ammo.MagazineSize;
            TurretAmmoLookup[skill.TargetEntity] = ammo;
          }
        }
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
        DeltaTime = SystemAPI.Time.DeltaTime,
        TurretAmmoLookup = SystemAPI.GetComponentLookup<TurretAmmo>()
      }.Schedule(state.Dependency);
    }
  }
}
