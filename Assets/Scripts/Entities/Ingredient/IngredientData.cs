
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Ingredient
{

  public enum IngredientType
  {
    None,

    Mana,
  }

  public struct IngredientData : IComponentData
  {
    public IngredientType Type;
    public float3 Position;
  }

}