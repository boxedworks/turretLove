
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Loot
{

  public enum LootType
  {
    None,

    Mana,

    Wood,
    Stone,

    Emerald,
    Sapphire,
    Ruby,
    Diamond
  }

  public struct LootData : IComponentData
  {
    public LootType Type;
  }

}