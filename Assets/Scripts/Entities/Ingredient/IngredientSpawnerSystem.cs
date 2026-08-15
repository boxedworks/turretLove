
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Ingredient
{

  public partial struct IngredientSpawnerSystem : ISystem
  {
    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<IngredientSpawner>();

      state.EntityManager.AddBuffer<IngredientSpawnEvent>(state.EntityManager.CreateEntity());
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var spawnBuffer = SystemAPI.GetSingletonBuffer<IngredientSpawnEvent>();
      if (spawnBuffer.Length > 0)
      {
        var spawner = SystemAPI.GetSingleton<IngredientSpawner>();
        var events = spawnBuffer.ToNativeArray(Allocator.Temp);
        spawnBuffer.Clear();

        var random = new Random((uint)SystemAPI.Time.ElapsedTime);

        for (var i = events.Length - 1; i >= 0; i--)
        {
          var spawnEvent = events[i];
          var ingredient = state.EntityManager.Instantiate(spawner.IngredientPrefab);

          state.EntityManager.AddComponentData(ingredient, new IngredientData
          {
            Type = spawnEvent.Type,

            SpawnPosition = spawnEvent.SpawnPosition,
            SpawnTime = (float)SystemAPI.Time.ElapsedTime,
            SpawnArcHeight = random.NextFloat(0.5f, 1.5f),
            SpawnArcDuration = random.NextFloat(0.5f, 0.8f),
            SpawnArcDirection = random.NextFloat(-1f, 1f)
          });

          // Set position and scale
          var localTransform = new LocalTransform
          {
            Position = spawnEvent.SpawnPosition,
            Scale = 1f,
            Rotation = quaternion.identity
          };
          state.EntityManager.AddComponentData(ingredient, localTransform);
        }
        events.Dispose();
      }
    }

    public static void SpawnIngredient(DynamicBuffer<IngredientSpawnEvent> spawnBuffer, float3 spawnPosition, IngredientType type)
    {
      spawnBuffer.Add(new IngredientSpawnEvent
      {
        SpawnPosition = spawnPosition,
        Type = type
      });
    }

  }

  internal class Translation
  {
    public float3 Value { get; set; }
  }

  public struct IngredientSpawnEvent : IBufferElementData
  {
    public float3 SpawnPosition;
    public IngredientType Type;
  }
}