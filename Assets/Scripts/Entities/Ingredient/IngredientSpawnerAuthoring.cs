
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Ingredient
{

  public partial struct IngredientAuthoringSystem : ISystem
  {
    public class IngredientSpawnerAuthoring : MonoBehaviour
    {
      public GameObject IngredientPrefab;
    }

    public class IngredientSpawnerBaker : Baker<IngredientSpawnerAuthoring>
    {
      public override void Bake(IngredientSpawnerAuthoring authoring)
      {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new IngredientSpawner
        {
          IngredientPrefab = GetEntity(authoring.IngredientPrefab, TransformUsageFlags.Dynamic)
        });
      }
    }

    public struct IngredientSpawner : IComponentData
    {
      public Entity IngredientPrefab;
    }
  }

}