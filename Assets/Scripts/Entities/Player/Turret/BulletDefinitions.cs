using System;
using Assets.Scripts.Entities.Loot;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Player.Turret
{
  public enum BulletFiringPattern : byte
  {
    Single,
    Shotgun,
    Burst
  }

  public enum BulletModifierKind : byte
  {
    Damage,
    SpeedPercent,
    Size,
    ProjectileCount,
    BurstCount,
    Knockback,
    FireDamageOverTime,
    PoisonDamageOverTime
  }

  public enum ModifierRollTiming : byte
  {
    OnCrafting,
    OnFire
  }

  [Serializable]
  public struct BulletRuntimeStats
  {
    public float Damage;
    public float Speed;
    public float Size;
    public int ProjectileCount;
    public int BurstCount;
    public float Knockback;
    public float FireDamagePerSecond;
    public float FireDuration;
    public float PoisonDamagePerSecond;
    public float PoisonDuration;
  }

  public struct BulletStatRange
  {
    public float DamageMinimum;
    public float DamageMaximum;
    public float SpeedPercentMinimum;
    public float SpeedPercentMaximum;
    public float SizeMinimum;
    public float SizeMaximum;
    public int ProjectileCountMinimum;
    public int ProjectileCountMaximum;
    public int BurstCountMinimum;
    public int BurstCountMaximum;
    public float KnockbackMinimum;
    public float KnockbackMaximum;
    public float FireDamageMinimum;
    public float FireDamageMaximum;
    public float FireDuration;
    public float PoisonDamageMinimum;
    public float PoisonDamageMaximum;
    public float PoisonDuration;
  }

  public struct BulletProjectilePayload
  {
    public BulletRuntimeStats Stats;
  }

  public struct TurretMagazineSlot : IBufferElementData
  {
    public FixedString64Bytes CraftedBulletId;
    public BulletFiringPattern Pattern;
    public BulletRuntimeStats CraftedStats;
    public BulletStatRange FireRolls;
    public float ShotgunSpreadDegrees;
    public float BurstInterval;
    public byte IsEquipped;
  }

  public static class BulletStatUtility
  {
    public static void ApplyModifier(
      ref BulletRuntimeStats stats,
      BulletModifierKind kind,
      float value,
      float effectDuration)
    {
      switch (kind)
      {
        case BulletModifierKind.Damage:
          stats.Damage += value;
          break;
        case BulletModifierKind.SpeedPercent:
          stats.Speed *= 1f + value / 100f;
          break;
        case BulletModifierKind.Size:
          stats.Size = math.max(0.05f, stats.Size + value);
          break;
        case BulletModifierKind.ProjectileCount:
          stats.ProjectileCount = math.max(1, stats.ProjectileCount + (int)math.round(value));
          break;
        case BulletModifierKind.BurstCount:
          stats.BurstCount = math.max(1, stats.BurstCount + (int)math.round(value));
          break;
        case BulletModifierKind.Knockback:
          stats.Knockback += value;
          break;
        case BulletModifierKind.FireDamageOverTime:
          stats.FireDamagePerSecond += value;
          stats.FireDuration = math.max(stats.FireDuration, effectDuration);
          break;
        case BulletModifierKind.PoisonDamageOverTime:
          stats.PoisonDamagePerSecond += value;
          stats.PoisonDuration = math.max(stats.PoisonDuration, effectDuration);
          break;
      }
    }

    public static void AddFireRange(
      ref BulletStatRange ranges,
      BulletModifierKind kind,
      float minimum,
      float maximum,
      float effectDuration)
    {
      switch (kind)
      {
        case BulletModifierKind.Damage:
          ranges.DamageMinimum += minimum;
          ranges.DamageMaximum += maximum;
          break;
        case BulletModifierKind.SpeedPercent:
          ranges.SpeedPercentMinimum += minimum;
          ranges.SpeedPercentMaximum += maximum;
          break;
        case BulletModifierKind.Size:
          ranges.SizeMinimum += minimum;
          ranges.SizeMaximum += maximum;
          break;
        case BulletModifierKind.ProjectileCount:
          ranges.ProjectileCountMinimum += (int)math.round(minimum);
          ranges.ProjectileCountMaximum += (int)math.round(maximum);
          break;
        case BulletModifierKind.BurstCount:
          ranges.BurstCountMinimum += (int)math.round(minimum);
          ranges.BurstCountMaximum += (int)math.round(maximum);
          break;
        case BulletModifierKind.Knockback:
          ranges.KnockbackMinimum += minimum;
          ranges.KnockbackMaximum += maximum;
          break;
        case BulletModifierKind.FireDamageOverTime:
          ranges.FireDamageMinimum += minimum;
          ranges.FireDamageMaximum += maximum;
          ranges.FireDuration = math.max(ranges.FireDuration, effectDuration);
          break;
        case BulletModifierKind.PoisonDamageOverTime:
          ranges.PoisonDamageMinimum += minimum;
          ranges.PoisonDamageMaximum += maximum;
          ranges.PoisonDuration = math.max(ranges.PoisonDuration, effectDuration);
          break;
      }
    }

    public static void ResolveFireRolls(ref BulletRuntimeStats stats, in BulletStatRange ranges, ref Unity.Mathematics.Random random)
    {
      ApplyModifier(ref stats, BulletModifierKind.Damage, random.NextFloat(ranges.DamageMinimum, ranges.DamageMaximum), 0f);
      ApplyModifier(ref stats, BulletModifierKind.SpeedPercent, random.NextFloat(ranges.SpeedPercentMinimum, ranges.SpeedPercentMaximum), 0f);
      ApplyModifier(ref stats, BulletModifierKind.Size, random.NextFloat(ranges.SizeMinimum, ranges.SizeMaximum), 0f);
      ApplyModifier(ref stats, BulletModifierKind.ProjectileCount, random.NextInt(ranges.ProjectileCountMinimum, ranges.ProjectileCountMaximum + 1), 0f);
      ApplyModifier(ref stats, BulletModifierKind.BurstCount, random.NextInt(ranges.BurstCountMinimum, ranges.BurstCountMaximum + 1), 0f);
      ApplyModifier(ref stats, BulletModifierKind.Knockback, random.NextFloat(ranges.KnockbackMinimum, ranges.KnockbackMaximum), 0f);
      ApplyModifier(ref stats, BulletModifierKind.FireDamageOverTime, random.NextFloat(ranges.FireDamageMinimum, ranges.FireDamageMaximum), ranges.FireDuration);
      ApplyModifier(ref stats, BulletModifierKind.PoisonDamageOverTime, random.NextFloat(ranges.PoisonDamageMinimum, ranges.PoisonDamageMaximum), ranges.PoisonDuration);
    }
  }

  public readonly struct ResourceCost
  {
    public readonly LootType Type;
    public readonly int Amount;

    public ResourceCost(LootType type, int amount)
    {
      Type = type;
      Amount = amount;
    }
  }

  public sealed class BulletDefinition
  {
    public string Id;
    public string DisplayName;
    public string Description;
    public BulletFiringPattern Pattern;
    public BulletRuntimeStats BaseStats;
    public float ShotgunSpreadDegrees;
    public float BurstInterval;
    public ResourceCost[] CraftCost;
  }

  public sealed class BulletModifierDefinition
  {
    public string Id;
    public string DisplayName;
    public string Description;
    public BulletModifierKind Kind;
    public ModifierRollTiming RollTiming;
    public float MinimumValue;
    public float MaximumValue;
    public float EffectDuration;
    public ResourceCost[] ApplyCost;
  }

  // This is the editable gameplay catalog. Persistent saves only store these stable IDs and rolls.
  public static class BulletCatalog
  {
    public static readonly BulletDefinition[] Definitions =
    {
      new BulletDefinition
      {
        Id = "a163b61e-7df7-48c2-9a64-04bf4d7b0101",
        DisplayName = "Copper Slug",
        Description = "A dependable single high-velocity round.",
        Pattern = BulletFiringPattern.Single,
        BaseStats = new BulletRuntimeStats { Damage = 1.5f, Speed = 6f, Size = 0.25f, ProjectileCount = 1, BurstCount = 1, Knockback = 4f },
        CraftCost = new[] { new ResourceCost(LootType.Wood, 3), new ResourceCost(LootType.Stone, 2) }
      },
      new BulletDefinition
      {
        Id = "a163b61e-7df7-48c2-9a64-04bf4d7b0102",
        DisplayName = "Ember Round",
        Description = "A single round that leaves enemies burning.",
        Pattern = BulletFiringPattern.Single,
        BaseStats = new BulletRuntimeStats { Damage = 1f, Speed = 5.5f, Size = 0.27f, ProjectileCount = 1, BurstCount = 1, Knockback = 3f, FireDamagePerSecond = 1f, FireDuration = 3f },
        CraftCost = new[] { new ResourceCost(LootType.Mana, 3), new ResourceCost(LootType.Ruby, 1) }
      },
      new BulletDefinition
      {
        Id = "a163b61e-7df7-48c2-9a64-04bf4d7b0103",
        DisplayName = "Scatter Shell",
        Description = "Three pellets spread across a cone.",
        Pattern = BulletFiringPattern.Shotgun,
        BaseStats = new BulletRuntimeStats { Damage = 0.8f, Speed = 5f, Size = 0.18f, ProjectileCount = 3, BurstCount = 1, Knockback = 2f },
        ShotgunSpreadDegrees = 24f,
        CraftCost = new[] { new ResourceCost(LootType.Wood, 5), new ResourceCost(LootType.Stone, 3) }
      },
      new BulletDefinition
      {
        Id = "a163b61e-7df7-48c2-9a64-04bf4d7b0104",
        DisplayName = "Venom Burst",
        Description = "A timed three-shot burst that poisons its target.",
        Pattern = BulletFiringPattern.Burst,
        BaseStats = new BulletRuntimeStats { Damage = 0.7f, Speed = 5.5f, Size = 0.22f, ProjectileCount = 1, BurstCount = 3, Knockback = 2f, PoisonDamagePerSecond = 0.8f, PoisonDuration = 4f },
        BurstInterval = 0.16f,
        CraftCost = new[] { new ResourceCost(LootType.Mana, 2), new ResourceCost(LootType.Emerald, 1) }
      }
    };

    public static readonly BulletModifierDefinition[] Modifiers =
    {
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90101", DisplayName = "Tempered Core", Description = "+1 to +3 damage, rolled when installed.",
        Kind = BulletModifierKind.Damage, RollTiming = ModifierRollTiming.OnCrafting, MinimumValue = 1f, MaximumValue = 3f,
        ApplyCost = new[] { new ResourceCost(LootType.Stone, 3) }
      },
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90102", DisplayName = "Overcharge", Description = "+15% to +45% speed, rolled every fire.",
        Kind = BulletModifierKind.SpeedPercent, RollTiming = ModifierRollTiming.OnFire, MinimumValue = 15f, MaximumValue = 45f,
        ApplyCost = new[] { new ResourceCost(LootType.Mana, 2) }
      },
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90103", DisplayName = "Expansion Chamber", Description = "+0.08 to +0.22 projectile size.",
        Kind = BulletModifierKind.Size, RollTiming = ModifierRollTiming.OnCrafting, MinimumValue = 0.08f, MaximumValue = 0.22f,
        ApplyCost = new[] { new ResourceCost(LootType.Wood, 2), new ResourceCost(LootType.Stone, 1) }
      },
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90104", DisplayName = "Scatter Bore", Description = "+1 or +2 pellets for shotgun rounds.",
        Kind = BulletModifierKind.ProjectileCount, RollTiming = ModifierRollTiming.OnCrafting, MinimumValue = 1f, MaximumValue = 2f,
        ApplyCost = new[] { new ResourceCost(LootType.Wood, 4) }
      },
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90105", DisplayName = "Burst Capacitor", Description = "+1 or +2 timed burst shots, rolled every fire.",
        Kind = BulletModifierKind.BurstCount, RollTiming = ModifierRollTiming.OnFire, MinimumValue = 1f, MaximumValue = 2f,
        ApplyCost = new[] { new ResourceCost(LootType.Mana, 3) }
      },
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90106", DisplayName = "Impact Driver", Description = "+2 to +5 knockback.",
        Kind = BulletModifierKind.Knockback, RollTiming = ModifierRollTiming.OnCrafting, MinimumValue = 2f, MaximumValue = 5f,
        ApplyCost = new[] { new ResourceCost(LootType.Stone, 2) }
      },
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90107", DisplayName = "Ignition Gel", Description = "+0.4 to +1.0 fire DPS for 3 seconds, rolled every fire.",
        Kind = BulletModifierKind.FireDamageOverTime, RollTiming = ModifierRollTiming.OnFire, MinimumValue = 0.4f, MaximumValue = 1f, EffectDuration = 3f,
        ApplyCost = new[] { new ResourceCost(LootType.Ruby, 1), new ResourceCost(LootType.Mana, 2) }
      },
      new BulletModifierDefinition
      {
        Id = "bd0fa4d4-c6d7-47cb-a50a-048dc6a90108", DisplayName = "Toxin Vial", Description = "+0.4 to +0.9 poison DPS for 4 seconds.",
        Kind = BulletModifierKind.PoisonDamageOverTime, RollTiming = ModifierRollTiming.OnCrafting, MinimumValue = 0.4f, MaximumValue = 0.9f, EffectDuration = 4f,
        ApplyCost = new[] { new ResourceCost(LootType.Emerald, 1), new ResourceCost(LootType.Mana, 1) }
      }
    };

    public static BulletDefinition FindDefinition(string id)
    {
      for (var index = 0; index < Definitions.Length; index++)
        if (Definitions[index].Id == id)
          return Definitions[index];
      return null;
    }

    public static BulletModifierDefinition FindModifier(string id)
    {
      for (var index = 0; index < Modifiers.Length; index++)
        if (Modifiers[index].Id == id)
          return Modifiers[index];
      return null;
    }
  }
}
