using Assets.Scripts.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Ingredient
{
  public partial struct IngredientMovementSystem : ISystem
  {
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.Dependency = new IngredientMovementJob
      {
        Ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
        PlayerPosition = new NativeReference<float3>(SystemAPI.GetSingleton<InputState>().MouseWorldPosition, Allocator.TempJob),
        CurrentTime = new NativeReference<float>((float)SystemAPI.Time.ElapsedTime, Allocator.TempJob),
        DeltaTime = new NativeReference<float>((float)SystemAPI.Time.DeltaTime, Allocator.TempJob)
      }.Schedule(state.Dependency);
    }

    [BurstCompile]
    partial struct IngredientMovementJob : IJobEntity
    {
      public EntityCommandBuffer Ecb;
      public NativeReference<float3> PlayerPosition;
      public NativeReference<float> CurrentTime, DeltaTime;

      public Random Random;

      public readonly void Execute(Entity entity, ref IngredientData ingredientData, LocalTransform localTransform)
      {
        var timeSinceSpawn = CurrentTime.Value - ingredientData.SpawnTime;

        var throwHorizontalDirection = ingredientData.SpawnArcDirection;
        var arcDuration = ingredientData.SpawnArcDuration;
        if (timeSinceSpawn < arcDuration)
        {
          var arcHeight = ingredientData.SpawnArcHeight;
          var arcProgress = timeSinceSpawn / arcDuration;
          var arcY = math.sin(arcProgress * math.PI) * arcHeight;

          var throwVelocity = new float3(throwHorizontalDirection * timeSinceSpawn, arcY, 0f) + ingredientData.SpawnPosition;
          Ecb.AddComponent(entity, new LocalTransform
          {
            Position = throwVelocity,
            Scale = 1f,
            Rotation = quaternion.identity
          });
        }
        else
        {
          var directionToPlayer = PlayerPosition.Value - localTransform.Position;

          if (math.length(directionToPlayer) < 0.5f)
            Ecb.DestroyEntity(entity);
        }
      }
    }
  }
}
