using Assets.Scripts.Entities.Game;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
    public void OnUpdate(ref SystemState state)
    {
      state.Enabled = false;

      var spawner = SystemAPI.GetSingleton<PlayerSpawner>();
      var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
      var physicsCollider = state.EntityManager.GetComponentData<PhysicsCollider>(spawner.PlayerPrefab);

      var player = ecb.Instantiate(spawner.PlayerPrefab);

      ecb.SetComponent(player, LocalTransform.FromPosition(new float3(1f, 0f, 0f)));
      ecb.AddComponent(player, new PlayerAttributes
      {
        MaxHealth = 100f,
        CurrentHealth = 100f,
        MoveSpeed = 1f,
        Damage = 1f,
        AttackSpeed = 1f
      });
      ecb.AddBuffer<DamageEvent>(player);

      // // Change collision layer to avoid colliding with the map
      // var collider = physicsCollider.Value;
      // var filter = collider.Value.GetCollisionFilter();
      // filter.BelongsTo = 1 << 4;
      // filter.CollidesWith = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 4);
      // collider.Value.SetCollisionFilter(filter);
      // physicsCollider.Value = collider;
      // ecb.SetComponent(player, physicsCollider);

      ecb.Playback(state.EntityManager);
      ecb.Dispose();
    }
  }
}
