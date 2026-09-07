
using Assets.Scripts.Entities.Enemy;
using Assets.Scripts.Entities.Game;
using Assets.Scripts.Entities.Game.Scroll;
using Assets.Scripts.Entities.Loot.DropTables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Loot
{

  public partial struct LootSpawnerSystem : ISystem
  {
    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<LootSpawner>();

      state.EntityManager.AddBuffer<LootSpawnEvent>(state.EntityManager.CreateEntity());
    }


    // [BurstCompile] omitted: accesses managed DropTableSystem statics
    public void OnUpdate(ref SystemState state)
    {
      var spawnBuffer = SystemAPI.GetSingletonBuffer<LootSpawnEvent>();
      if (spawnBuffer.Length == 0)
        return;

      var spawner = SystemAPI.GetSingleton<LootSpawner>();
      var events = spawnBuffer.ToNativeArray(Allocator.Temp);
      spawnBuffer.Clear();

      var random = new Random((uint)SystemAPI.Time.ElapsedTime);

      for (var i = events.Length - 1; i >= 0; i--)
      {
        var spawnEvent = events[i];
        var drops = DropTableSystem.RollDrops(spawnEvent.Type);

        foreach (var drop in drops)
        {
          var dropType = drop.Key;
          var dropAmount = drop.Value;
          var prefab = dropType switch
          {
            LootType.Mana => spawner.ManaPrefab,
            LootType.Wood => spawner.WoodPrefab,
            LootType.Stone => spawner.StonePrefab,
            LootType.Emerald => spawner.EmeraldPrefab,
            LootType.Sapphire => spawner.SapphirePrefab,
            LootType.Ruby => spawner.RubyPrefab,
            LootType.Diamond => spawner.DiamondPrefab,
            _ => Entity.Null
          };

          if (prefab == Entity.Null)
            continue;

          if (dropAmount <= 0)
            continue;

          for (var j = 0; j < dropAmount; j++)
          {
            var loot = state.EntityManager.Instantiate(prefab);
            state.EntityManager.AddComponentData(loot, new LootData { Type = dropType });
            state.EntityManager.AddComponentData(loot, new ScrollComponent { Direction = new float2(-1f, 0f), Speed = 0.5f });
            state.EntityManager.AddComponentData(loot, new LootSpawnData
            {
              SpawnPosition = spawnEvent.SpawnPosition,
              SpawnTime = (float)SystemAPI.Time.ElapsedTime,
              SpawnArcHeight = random.NextFloat(0.5f, 1.5f),
              SpawnArcDuration = random.NextFloat(0.5f, 0.8f),
              SpawnArcDirection = random.NextFloat(-1f, 1f),
              SpawnArcDirectionY = random.NextFloat(-1f, 1f)
            });
            state.EntityManager.AddComponentData(loot, new LocalTransform
            {
              Position = spawnEvent.SpawnPosition,
              Scale = 1f,
              Rotation = quaternion.identity
            });
            state.EntityManager.AddComponent<LevelEntity>(loot);
          }
        }
      }
      events.Dispose();
    }

    public static void SpawnLoot(DynamicBuffer<LootSpawnEvent> spawnBuffer, float3 spawnPosition, EnemyType enemyType)
    {
      spawnBuffer.Add(new LootSpawnEvent
      {
        SpawnPosition = spawnPosition,
        Type = enemyType
      });
    }

  }

  internal class Translation
  {
    public float3 Value { get; set; }
  }

  public struct LootSpawnEvent : IBufferElementData
  {
    public float3 SpawnPosition;
    public EnemyType Type;
  }
}