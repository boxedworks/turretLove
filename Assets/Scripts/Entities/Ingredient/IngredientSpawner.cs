
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.Scripts.Entities.Ingredient
{

  public struct IngredientSpawner : ISystem
  {


    // Move ingredients after spawning and check for player collection by distance
    [BurstCompile]
    partial struct IngredientMovementJob : IJobEntity
    {
      public EntityCommandBuffer Ecb;
      public NativeReference<float3> PlayerPosition;

      public readonly void Execute(Entity entity, ref IngredientData ingredientData)
      {

      }
    }

    [BurstCompile]
    public void OnUpdate()
    {

    }

  }

  public struct IngredientSpawnEvent : IBufferElementData
  {
    public float3 SpawnPosition;
    public quaternion SpawnRotation;
  }
}