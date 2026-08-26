using Assets.Scripts.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Assets.Scripts.Entities.Player.Character
{
  public partial struct PlayerSystem : ISystem
  {
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      var inputState = SystemAPI.GetSingleton<InputState>();

      var moveDirection = new float2(
        (inputState.ArrowRightDown ? 1f : 0f) - (inputState.ArrowLeftDown ? 1f : 0f),
        (inputState.ArrowUpDown ? 1f : 0f) - (inputState.ArrowDownDown ? 1f : 0f)
      );

      state.Dependency = new PlayerMovementJob
      {
        MoveDirection = new NativeReference<float2>(moveDirection, Allocator.TempJob)
      }.Schedule(state.Dependency);
    }

    [BurstCompile]
    partial struct PlayerMovementJob : IJobEntity
    {
      public NativeReference<float2> MoveDirection;

      public void Execute(ref PhysicsVelocity velocity, in PlayerAttributes attributes)
      {
        var dir = MoveDirection.Value;
        if (math.lengthsq(dir) > 0f)
          dir = math.normalize(dir);

        velocity.Linear = new float3(attributes.MoveSpeed * dir, 0f);
      }
    }
  }
}
