using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Game.Scroll
{
  public partial struct ScrollSystem : ISystem
  {
    [BurstCompile]
    partial struct ScrollJob : IJobEntity
    {
      public float DeltaTime;

      public readonly void Execute(ref LocalTransform transform, in ScrollComponent scroll)
      {
        float3 delta = DeltaTime * scroll.Speed * new float3(scroll.Direction.x, scroll.Direction.y, 0f);
        transform.Position += delta;
      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.Dependency = new ScrollJob()
      {
        DeltaTime = SystemAPI.Time.DeltaTime
      }
        .Schedule(state.Dependency);
    }
  }
}
