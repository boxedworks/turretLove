
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Character
{
  public struct PlayerAttributes : IComponentData
  {
    public float MaxHealth;
    public float CurrentHealth;
    public float Damage;
    public float AttackSpeed;
  }
}