using Unity.Burst;
using Unity.Entities;

namespace Assets.Scripts.Entities.Game
{
  /// <summary>
  /// Handles cleanup of all level entities (player, turret, enemies, loot, projectiles)
  /// when the level ends.
  /// </summary>
  public partial struct LevelCleanupSystem : ISystem
  {
    [BurstCompile]
    partial struct LevelCleanupJob : IJobEntity
    {
      public EntityCommandBuffer.ParallelWriter Ecb;

      private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in LevelEntity levelEntity)
      {
        Ecb.DestroyEntity(sortKey, entity);
      }
    }

    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<LevelState>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var levelStateEntity = SystemAPI.GetSingletonEntity<LevelState>();
      var levelState = SystemAPI.GetComponent<LevelState>(levelStateEntity);

      // Only process if level has ended
      if (levelState.CurrentState != LevelState.State.Ended)
        return;

      // Schedule job to destroy all entities marked with LevelEntity
      var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
      var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

      state.Dependency = new LevelCleanupJob { Ecb = ecb.AsParallelWriter() }
        .ScheduleParallel(state.Dependency);

      // Reset level state to inactive
      SystemAPI.SetComponent(levelStateEntity, new LevelState
      {
        CurrentState = LevelState.State.Inactive,
        SelectedAreaIndex = levelState.SelectedAreaIndex,
        SelectedLevelIndex = levelState.SelectedLevelIndex
      });
    }
  }
}
