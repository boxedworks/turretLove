
using Assets.Scripts.Entities.Game.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{

  public partial struct TurretSystem : ISystem
  {
    private const float AimAlignmentTolerance = 0.01f;
    private const float HalfScreenWidth = 8.5f;
    private const float HalfScreenHeight = 4.5f;

    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<TurretAttributes>();
    }

    //
    [BurstCompile]
    partial struct TurretUpdateJob : IJobEntity
    {

      public float3 TargetLookPosition;
      public Entity TargetEntity;
      public double CurrentTime;
      public float DeltaTime;
      public NativeList<BulletSpawnEvent> BulletSpawnEvents;

      public DynamicBuffer<AudioEvent> AudioEventBuffer;
      public ComponentLookup<LocalTransform> LocalTransformLookup;

      public void Execute(ref TurretAttributes turretAttributes, ref TurretAmmo turretAmmo)
      {
        turretAttributes.CurrentTarget = TargetEntity;
        if (TargetEntity == Entity.Null)
          return;

        var turretTopTransform = LocalTransformLookup[turretAttributes.TurretTopEntity];
        var isTargetAligned = RotateTurret(ref turretAttributes, ref turretTopTransform);
        if (isTargetAligned)
          HandleBullet(ref turretAttributes, ref turretAmmo, turretTopTransform);

        LocalTransformLookup[turretAttributes.TurretTopEntity] = turretTopTransform;
      }

      readonly bool RotateTurret(ref TurretAttributes turretAttributes, ref LocalTransform localTransform)
      {
        var directionToTarget = TargetLookPosition - localTransform.Position;
        var targetAngle = math.atan2(directionToTarget.y, directionToTarget.x) + math.radians(-90f);
        var currentAngle = 2f * math.atan2(localTransform.Rotation.value.z, localTransform.Rotation.value.w);
        var deltaAngle = math.atan2(math.sin(targetAngle - currentAngle), math.cos(targetAngle - currentAngle));
        var angleToTarget = math.abs(deltaAngle);

        if (angleToTarget < AimAlignmentTolerance)
        {
          localTransform.Rotation = quaternion.RotateZ(targetAngle);
          return true;
        }
        else
        {
          var rotationSpeed = turretAttributes.RotationSpeed * DeltaTime;
          localTransform.Rotation = quaternion.RotateZ(currentAngle + math.clamp(deltaAngle, -rotationSpeed, rotationSpeed));
          return angleToTarget - rotationSpeed < AimAlignmentTolerance;
        }
      }

      readonly void HandleBullet(ref TurretAttributes turretAttributes, ref TurretAmmo turretAmmo, in LocalTransform localTransform)
      {
        if (turretAmmo.CurrentAmmo <= 0)
          return;

        if (CurrentTime - turretAttributes.TimeSinceLastShot < turretAttributes.FireRate)
          return;
        turretAttributes.TimeSinceLastShot = CurrentTime;
        turretAmmo.CurrentAmmo--;

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
      public NativeReference<Entity> ClosestTargetEntity;

      public float3 SourcePosition;

      public void Execute(Entity entity, in TurretTargetable _, in LocalTransform localTransform)
      {
        if (math.abs(localTransform.Position.x) > HalfScreenWidth
          || math.abs(localTransform.Position.y) > HalfScreenHeight)
        {
          return;
        }

        var directionToTarget = SourcePosition - localTransform.Position;
        var distanceToTarget = math.length(directionToTarget);

        if (distanceToTarget < ClosestTargetDistance.Value)
        {
          ClosestTargetDistance.Value = distanceToTarget;
          ClosestTargetPosition.Value = localTransform.Position;
          ClosestTargetEntity.Value = entity;
        }
      }

    }

    //
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.CompleteDependency();

      if (SystemAPI.HasSingleton<TurretDefeated>())
        return;

      var turret = SystemAPI.GetSingleton<TurretAttributes>();
      var targetEntity = turret.CurrentTarget;
      float3 targetPosition;
      if (targetEntity != Entity.Null
        && state.EntityManager.Exists(targetEntity)
        && state.EntityManager.HasComponent<LocalTransform>(targetEntity))
      {
        targetPosition = state.EntityManager.GetComponentData<LocalTransform>(targetEntity).Position;
      }
      else
      {
        var closestTargetJob = new GatherClosestTargetJob
        {
          SourcePosition = float3.zero, // Assuming the turret is at the origin for this example
          ClosestTargetDistance = new NativeReference<float>(Allocator.TempJob) { Value = float.MaxValue },
          ClosestTargetPosition = new NativeReference<float3>(Allocator.TempJob) { Value = float3.zero },
          ClosestTargetEntity = new NativeReference<Entity>(Allocator.TempJob) { Value = Entity.Null }
        };
        closestTargetJob.Run();

        targetEntity = closestTargetJob.ClosestTargetEntity.Value;
        targetPosition = closestTargetJob.ClosestTargetPosition.Value;

        closestTargetJob.ClosestTargetDistance.Dispose();
        closestTargetJob.ClosestTargetPosition.Dispose();
        closestTargetJob.ClosestTargetEntity.Dispose();
      }

      // Update turret
      var spawnEvents = new NativeList<BulletSpawnEvent>(Allocator.TempJob);
      new TurretUpdateJob
      {
        TargetLookPosition = targetPosition,
        TargetEntity = targetEntity,
        CurrentTime = SystemAPI.Time.ElapsedTime,
        DeltaTime = SystemAPI.Time.DeltaTime,
        BulletSpawnEvents = spawnEvents,

        AudioEventBuffer = SystemAPI.GetSingletonBuffer<AudioEvent>(),
        LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>()
      }.Run();

      var buffer = SystemAPI.GetSingletonBuffer<BulletSpawnEvent>();
      foreach (var e in spawnEvents)
        buffer.Add(e);

      spawnEvents.Dispose();
    }

  }
}