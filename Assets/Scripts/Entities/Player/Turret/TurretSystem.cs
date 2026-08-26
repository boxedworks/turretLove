
using Assets.Scripts.Entities.Game.Audio;
using Assets.Scripts.Input;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{

  public partial struct TurretSystem : ISystem
  {

    //
    [BurstCompile]
    partial struct TurretUpdateJob : IJobEntity
    {

      public float3 TargetLookPosition;
      public double CurrentTime;
      public float DeltaTime;
      public NativeList<BulletSpawnEvent> BulletSpawnEvents;

      public DynamicBuffer<AudioEvent> AudioEventBuffer;

      public readonly void Execute(ref TurretAttributes turretAttributes, ref LocalTransform localTransform)
      {
        RotateTurret(ref turretAttributes, ref localTransform);
        HandleBullet(ref turretAttributes, localTransform);
      }

      readonly void RotateTurret(ref TurretAttributes turretAttributes, ref LocalTransform localTransform)
      {
        var directionToTarget = TargetLookPosition - localTransform.Position;
        var targetAngle = math.atan2(directionToTarget.y, directionToTarget.x) + math.radians(-90f);
        var currentAngle = 2f * math.atan2(localTransform.Rotation.value.z, localTransform.Rotation.value.w);
        var deltaAngle = math.atan2(math.sin(targetAngle - currentAngle), math.cos(targetAngle - currentAngle));

        if (math.abs(deltaAngle) < 0.01f)
          localTransform.Rotation = quaternion.RotateZ(targetAngle);
        else
        {
          var rotationSpeed = turretAttributes.RotationSpeed * DeltaTime;
          localTransform.Rotation = quaternion.RotateZ(currentAngle + math.clamp(deltaAngle, -rotationSpeed, rotationSpeed));
        }
      }

      readonly void HandleBullet(ref TurretAttributes turretAttributes, in LocalTransform localTransform)
      {
        if (CurrentTime - turretAttributes.TimeSinceLastShot < turretAttributes.FireRate)
          return;
        turretAttributes.TimeSinceLastShot = CurrentTime;

        BulletSpawnEvents.Add(new BulletSpawnEvent
        {
          SpawnPosition = localTransform.Position,
          SpawnRotation = localTransform.Rotation,
        });

        // Add audio event for turret shooting
        AudioEventBuffer.Add(new AudioEvent { Type = AudioEvent.EventType.Shoot });
      }
    }

    // Job to gather closest target
    [BurstCompile]
    [WithNone(typeof(TurretDefeated))]
    partial struct GatherClosestTargetJob : IJobEntity
    {

      public NativeReference<float3> ClosestTargetPosition;
      public NativeReference<float> ClosestTargetDistance;

      public float3 SourcePosition;

      public void Execute(in TurretTargetable turretTargetable, in LocalTransform localTransform)
      {
        var directionToTarget = SourcePosition - localTransform.Position;
        var distanceToTarget = math.length(directionToTarget);

        if (distanceToTarget < ClosestTargetDistance.Value)
        {
          ClosestTargetDistance.Value = distanceToTarget;
          ClosestTargetPosition.Value = localTransform.Position;
        }
      }

    }

    //
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.CompleteDependency();

      // Gather closest target
      var closestTargetJob = new GatherClosestTargetJob
      {
        SourcePosition = float3.zero, // Assuming the turret is at the origin for this example
        ClosestTargetDistance = new NativeReference<float>(Allocator.TempJob) { Value = float.MaxValue },
        ClosestTargetPosition = new NativeReference<float3>(Allocator.TempJob) { Value = float3.zero }
      };
      closestTargetJob.Run();

      // Update turret
      var inputData = SystemAPI.GetSingleton<InputState>();
      var spawnEvents = new NativeList<BulletSpawnEvent>(Allocator.TempJob);

      new TurretUpdateJob
      {
        TargetLookPosition = closestTargetJob.ClosestTargetPosition.Value,
        CurrentTime = SystemAPI.Time.ElapsedTime,
        DeltaTime = SystemAPI.Time.DeltaTime,
        BulletSpawnEvents = spawnEvents,

        AudioEventBuffer = SystemAPI.GetSingletonBuffer<AudioEvent>()
      }.Run();

      var buffer = SystemAPI.GetSingletonBuffer<BulletSpawnEvent>();
      foreach (var e in spawnEvents)
        buffer.Add(e);

      closestTargetJob.ClosestTargetDistance.Dispose();
      closestTargetJob.ClosestTargetPosition.Dispose();

      spawnEvents.Dispose();
    }

  }
}