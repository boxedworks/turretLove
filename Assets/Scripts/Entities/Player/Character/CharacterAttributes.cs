
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Character
{
  public struct CharacterAttributes : IComponentData
  {
    public float MaxHealth;
    public float CurrentHealth;
    public float MoveSpeed;
    public float Damage;
    public float AttackSpeed;
  }
}