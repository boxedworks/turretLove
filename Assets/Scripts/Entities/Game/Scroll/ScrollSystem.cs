using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Game.Scroll
{
  public partial struct ScrollSystem : ISystem
  {
    private const float LeftScreenBound = -10f;

    [BurstCompile]
    partial struct ScrollJob : IJobEntity
    {
      public EntityCommandBuffer.ParallelWriter Ecb;
      public float DeltaTime;

      public readonly void Execute([EntityIndexInQuery] int entityIndex, Entity entity, ref LocalTransform transform, in ScrollComponent scroll)
      {
        if (transform.Position.x < LeftScreenBound)
        {
          Ecb.DestroyEntity(entityIndex, entity);
          return;
        }

        float3 delta = DeltaTime * scroll.Speed * new float3(scroll.Direction.x, scroll.Direction.y, 0f);
        transform.Position += delta;
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
        .CreateCommandBuffer(state.WorldUnmanaged)
        .AsParallelWriter();

      state.Dependency = new ScrollJob()
      {
        Ecb = ecb,
        DeltaTime = SystemAPI.Time.DeltaTime
      }
        .ScheduleParallel(state.Dependency);
    }
  }
}
