using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Character
{
  public partial struct PlayerSpawnerSystem : ISystem
  {
    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<PlayerSpawner>();
    }

    [BurstCompile]
    public readonly void OnUpdate(ref SystemState state)
    {
      state.Enabled = false;

      var spawner = SystemAPI.GetSingleton<PlayerSpawner>();
      var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

      var player = ecb.Instantiate(spawner.PlayerPrefab);

      ecb.SetComponent(player, LocalTransform.FromPosition(new float3(1f, 0f, 0f)));
      ecb.SetComponent(player, new PlayerAttributes
      {
        MaxHealth = 100f,
        CurrentHealth = 100f,
        MoveSpeed = 5f,
        Damage = 1f,
        AttackSpeed = 1f
      });

      ecb.Playback(state.EntityManager);
      ecb.Dispose();
    }
  }
}
