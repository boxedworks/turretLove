using System;
using System.Collections.Generic;
using Assets.Scripts.Entities.Player.Turret;

namespace Assets.Scripts.Bullets
{
  [Serializable]
  public sealed class CraftedBulletSave
  {
    public string Id;
    public string DefinitionId;
    public int Level;
    public bool HasRolledBaseStats;
    public BulletRuntimeStats BaseStats;
    public float ShotgunSpreadDegrees;
    public float BurstInterval;
    public float FireInterval;
    public List<CraftedBulletModifierSave> Modifiers = new();
  }
}
