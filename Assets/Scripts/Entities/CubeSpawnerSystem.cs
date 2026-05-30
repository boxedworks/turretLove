
using Assets.Scripts.Entities.Player.Turret;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities
{
  public partial struct CubeSpawnerSystem : ISystem
  {
    private int _spawnedAmount;
    private Random _random;

    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<CubeSpawner>();

      _spawnedAmount = 0;
      _random = new Random((uint)System.DateTime.Now.Ticks);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      if (_spawnedAmount < 50)
      {
        _spawnedAmount++;

        var spawner = SystemAPI.GetSingleton<CubeSpawner>();
        var newEntity = state.EntityManager.Instantiate(spawner.Prefab);

        var randomPosition = new float3(_random.NextFloat(-4f, 4f), _random.NextFloat(-4f, 4f), 0);
        state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPosition(randomPosition));

        state.EntityManager.AddComponent<CubeTest>(newEntity);
        state.EntityManager.AddBuffer<BulletCollisionEvent>(newEntity);
      }
    }
  }

}