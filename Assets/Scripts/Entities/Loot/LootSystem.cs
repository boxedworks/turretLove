using Assets.Scripts.Entities.Game.Audio;
using Assets.Scripts.Entities.Player.Character;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Loot
{
  public partial struct LootMovementSystem : ISystem
  {
    public readonly void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<PlayerAttributes>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
      var playerPosition = new NativeReference<float3>(SystemAPI.GetComponentLookup<LocalTransform>(true).GetRefRO(SystemAPI.GetSingletonEntity<PlayerAttributes>()).ValueRO.Position, Allocator.TempJob);
      var currentTime = new NativeReference<float>((float)SystemAPI.Time.ElapsedTime, Allocator.TempJob);
      var deltaTime = new NativeReference<float>(SystemAPI.Time.DeltaTime, Allocator.TempJob);

      state.Dependency = new LootMovementJob
      {
        Ecb = ecb,
        PlayerPosition = playerPosition,
        CurrentTime = currentTime,
        DeltaTime = deltaTime,
      }.Schedule(state.Dependency);

      state.Dependency = new LootAbsorbJob
      {
        Ecb = ecb,
        PlayerPosition = playerPosition,
        CurrentTime = currentTime,
        DeltaTime = deltaTime,
        AudioEventBuffer = SystemAPI.GetSingletonBuffer<AudioEvent>()
      }.Schedule(state.Dependency);
    }

    [BurstCompile]
    [WithNone(typeof(AbsorbingToPlayerEvent))]
    partial struct LootMovementJob : IJobEntity
    {
      public EntityCommandBuffer Ecb;
      public NativeReference<float3> PlayerPosition;
      public NativeReference<float> CurrentTime, DeltaTime;

      public Random Random;

      public readonly void Execute(Entity entity, ref LootSpawnData spawnData, LocalTransform localTransform)
      {
        var timeSinceSpawn = CurrentTime.Value - spawnData.SpawnTime;

        var throwHorizontalDirection = spawnData.SpawnArcDirection;
        var throwVerticalDirection = spawnData.SpawnArcDirectionY;
        var arcDuration = spawnData.SpawnArcDuration;
        if (timeSinceSpawn < arcDuration)
        {
          var arcHeight = spawnData.SpawnArcHeight;
          var arcProgress = timeSinceSpawn / arcDuration;
          var arcY = math.sin(arcProgress * math.PI) * arcHeight;

          var throwVelocity = new float3(throwHorizontalDirection * timeSinceSpawn, arcY + throwVerticalDirection * timeSinceSpawn, 0f) + spawnData.SpawnPosition;
          Ecb.AddComponent(entity, new LocalTransform
          {
            Position = throwVelocity,
            Scale = 1f,
            Rotation = quaternion.identity
          });
        }
        else
        {
          var distanceToPlayer = math.length(PlayerPosition.Value - localTransform.Position);
          if (distanceToPlayer < 0.3f)
          {
            Ecb.AddComponent(entity, new AbsorbingToPlayerEvent { StartTime = CurrentTime.Value });
            Ecb.RemoveComponent<LootSpawnData>(entity);
          }
        }
      }
    }

    [BurstCompile]
    partial struct LootAbsorbJob : IJobEntity
    {
      public EntityCommandBuffer Ecb;
      public DynamicBuffer<AudioEvent> AudioEventBuffer;
      public NativeReference<float3> PlayerPosition;
      public NativeReference<float> CurrentTime, DeltaTime;

      public readonly void Execute(Entity entity, in AbsorbingToPlayerEvent absorbEvent, LocalTransform localTransform)
      {
        var directionToPlayer = PlayerPosition.Value - localTransform.Position;
        var distanceToPlayer = math.length(directionToPlayer);

        if (distanceToPlayer < 0.1f)
        {
          Ecb.DestroyEntity(entity);
          AudioEventBuffer.Add(new AudioEvent { Type = AudioEvent.EventType.LootPickup });
        }
        else
        {
          var absorbDuration = CurrentTime.Value - absorbEvent.StartTime;
          var speed = 3f + absorbDuration * 5f;
          var movement = speed * DeltaTime.Value * math.normalize(directionToPlayer);
          Ecb.SetComponent(entity, LocalTransform.FromPosition(localTransform.Position + movement));
        }
      }
    }
  }
}
