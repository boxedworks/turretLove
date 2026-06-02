
using Assets.Scripts.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{
  public partial struct TurretSystem : ISystem
  {

    [BurstCompile]
    partial struct TurretLookJob : IJobEntity
    {

      public float3 TargetLookPosition;
      public float Speed;
      public float DeltaTime;

      public readonly void Execute(in TurretTop turretTop, ref LocalTransform localTransform)
      {
        var directionToTarget = TargetLookPosition - localTransform.Position;
        var angle = math.atan2(directionToTarget.y, directionToTarget.x) + math.radians(-90f);
        var targetRotation = quaternion.RotateZ(angle);

        // Rotate in a constant rate unless we're very close to the target rotation, in which case just snap to it to avoid jittering
        if (math.abs(math.angle(targetRotation, localTransform.Rotation)) < 0.01f)
          localTransform.Rotation = targetRotation;
        else
          localTransform.Rotation = math.slerp(localTransform.Rotation, targetRotation, Speed * DeltaTime);
      }
    }

    [BurstCompile]
    public readonly void OnUpdate(ref SystemState state)
    {
      var inputData = SystemAPI.GetSingleton<InputState>();

      new TurretLookJob
      {
        TargetLookPosition = inputData.MouseWorldPosition,
        Speed = 1f,
        DeltaTime = SystemAPI.Time.DeltaTime
      }.ScheduleParallel();
    }

  }
}