using Unity.Entities;

namespace Assets.Scripts.Entities.Game
{
  public partial struct LevelSpawnerSystem : ISystem
  {
    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<LevelSpawner>();
    }

    public readonly void OnUpdate(ref SystemState state)
    {
      state.Enabled = false;

      var spawner = SystemAPI.GetSingleton<LevelSpawner>();
      var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

      ecb.Instantiate(spawner.Border0Prefab);
      ecb.Instantiate(spawner.Border1Prefab);

      ecb.Playback(state.EntityManager);
      ecb.Dispose();
    }
  }
}
