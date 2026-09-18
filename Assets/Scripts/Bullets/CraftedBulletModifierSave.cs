using System;
using Assets.Scripts.Entities.Player.Turret;

namespace Assets.Scripts.Bullets
{
  [Serializable]
  public sealed class CraftedBulletModifierSave
  {
    public string ModifierId;
    public ModifierRollTiming RollTiming;
    public float RolledValue;
  }
}
