using Assets.Scripts.Entities.Game.Audio;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Player.Turret
{
  [UpdateBefore(typeof(BulletSpawnerSystem))]
  public partial struct TurretSystem : ISystem
  {
    private const float AimAlignmentTolerance = 0.01f;
    private const float HalfScreenWidth = 8.5f;
    private const float HalfScreenHeight = 4.5f;
    private Random random;

    public void OnCreate(ref SystemState state)
    {
      state.RequireForUpdate<TurretAttributes>();
      state.RequireForUpdate<BulletSpawnEvent>();
      state.RequireForUpdate<PendingBulletShot>();
      var seed = (uint)System.DateTime.UtcNow.Ticks;
      random = new Random(seed == 0u ? 1u : seed);
    }

    [BurstCompile]
    private partial struct TurretUpdateJob : IJobEntity
    {
      public float3 TargetLookPosition;
      public Entity TargetEntity;
      public double CurrentTime;
      public float DeltaTime;
      public NativeList<BulletSpawnEvent> BulletSpawnEvents;
      public NativeList<PendingBulletShot> PendingBulletShots;
      public DynamicBuffer<AudioEvent> AudioEventBuffer;
      public ComponentLookup<LocalTransform> LocalTransformLookup;
      public Random Random;

      public void Execute(
        ref TurretAttributes turretAttributes,
        ref TurretAmmo turretAmmo,
        DynamicBuffer<TurretMagazineSlot> magazine)
      {
        turretAttributes.CurrentTarget = TargetEntity;
        if (TargetEntity == Entity.Null)
          return;

        var turretTopTransform = LocalTransformLookup[turretAttributes.TurretTopEntity];
        var isTargetAligned = RotateTurret(ref turretAttributes, ref turretTopTransform);
        if (isTargetAligned)
          HandleBullet(ref turretAttributes, ref turretAmmo, magazine, turretTopTransform);

        LocalTransformLookup[turretAttributes.TurretTopEntity] = turretTopTransform;
      }

      private bool RotateTurret(ref TurretAttributes turretAttributes, ref LocalTransform localTransform)
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

        var rotationSpeed = turretAttributes.RotationSpeed * DeltaTime;
        localTransform.Rotation = quaternion.RotateZ(currentAngle + math.clamp(deltaAngle, -rotationSpeed, rotationSpeed));
        return angleToTarget - rotationSpeed < AimAlignmentTolerance;
      }

      private void HandleBullet(
        ref TurretAttributes turretAttributes,
        ref TurretAmmo turretAmmo,
        DynamicBuffer<TurretMagazineSlot> magazine,
        in LocalTransform localTransform)
      {
        if (turretAmmo.CurrentAmmo <= 0 || magazine.Length == 0)
          return;
        if (CurrentTime - turretAttributes.TimeSinceLastShot < turretAttributes.FireRate)
          return;

        turretAttributes.TimeSinceLastShot = CurrentTime;
        var slotIndex = math.clamp(turretAmmo.CurrentSlotIndex, 0, magazine.Length - 1);
        var slot = magazine[slotIndex];
        turretAmmo.CurrentAmmo--;
        turretAmmo.CurrentSlotIndex = (slotIndex + 1) % magazine.Length;

        if (slot.IsEquipped == 0)
          return;

        var stats = slot.CraftedStats;
        BulletStatUtility.ResolveFireRolls(ref stats, slot.FireRolls, ref Random);
        var payload = new BulletProjectilePayload { Stats = stats };
        EmitPattern(slot, payload, localTransform);
        AudioEventBuffer.Add(new AudioEvent { Type = AudioEvent.EventType.Shoot });
      }

      private void EmitPattern(
        in TurretMagazineSlot slot,
        in BulletProjectilePayload payload,
        in LocalTransform localTransform)
      {
        switch (slot.Pattern)
        {
          case BulletFiringPattern.Shotgun:
            var pelletCount = math.max(1, payload.Stats.ProjectileCount);
            for (var pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
            {
              var ratio = pelletCount == 1 ? 0.5f : pelletIndex / (float)(pelletCount - 1);
              var angle = math.radians(math.lerp(-slot.ShotgunSpreadDegrees * 0.5f, slot.ShotgunSpreadDegrees * 0.5f, ratio));
              EmitImmediate(payload, localTransform.Position, math.mul(localTransform.Rotation, quaternion.RotateZ(angle)));
            }
            break;

          case BulletFiringPattern.Burst:
            var burstCount = math.max(1, payload.Stats.BurstCount);
            for (var burstIndex = 0; burstIndex < burstCount; burstIndex++)
            {
              if (burstIndex == 0)
                EmitImmediate(payload, localTransform.Position, localTransform.Rotation);
              else
              {
                PendingBulletShots.Add(new PendingBulletShot
                {
                  FireAtTime = CurrentTime + slot.BurstInterval * burstIndex,
                  SpawnPosition = localTransform.Position,
                  SpawnRotation = localTransform.Rotation,
                  Payload = payload
                });
              }
            }
            break;

          default:
            EmitImmediate(payload, localTransform.Position, localTransform.Rotation);
            break;
        }
      }

      private void EmitImmediate(in BulletProjectilePayload payload, float3 position, quaternion rotation)
      {
        BulletSpawnEvents.Add(new BulletSpawnEvent
        {
          SpawnPosition = position,
          SpawnRotation = rotation,
          Payload = payload
        });
      }
    }

    [BurstCompile]
    [WithNone(typeof(TurretDefeated))]
    private partial struct GatherClosestTargetJob : IJobEntity
    {
      public NativeReference<float3> ClosestTargetPosition;
      public NativeReference<float> ClosestTargetDistance;
      public NativeReference<Entity> ClosestTargetEntity;
      public float3 SourcePosition;

      public void Execute(Entity entity, in TurretTargetable _, in LocalTransform localTransform)
      {
        if (math.abs(localTransform.Position.x) > HalfScreenWidth || math.abs(localTransform.Position.y) > HalfScreenHeight)
          return;

        var distanceToTarget = math.length(SourcePosition - localTransform.Position);
        if (distanceToTarget < ClosestTargetDistance.Value)
        {
          ClosestTargetDistance.Value = distanceToTarget;
          ClosestTargetPosition.Value = localTransform.Position;
          ClosestTargetEntity.Value = entity;
        }
      }
    }

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
          SourcePosition = float3.zero,
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

      var spawnEvents = new NativeList<BulletSpawnEvent>(Allocator.TempJob);
      var pendingShots = new NativeList<PendingBulletShot>(Allocator.TempJob);
      var updateJob = new TurretUpdateJob
      {
        TargetLookPosition = targetPosition,
        TargetEntity = targetEntity,
        CurrentTime = SystemAPI.Time.ElapsedTime,
        DeltaTime = SystemAPI.Time.DeltaTime,
        BulletSpawnEvents = spawnEvents,
        PendingBulletShots = pendingShots,
        AudioEventBuffer = SystemAPI.GetSingletonBuffer<AudioEvent>(),
        LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
        Random = random
      };
      updateJob.Run();
      random = updateJob.Random;

      var bulletEvents = SystemAPI.GetSingletonBuffer<BulletSpawnEvent>();
      foreach (var bulletEvent in spawnEvents)
        bulletEvents.Add(bulletEvent);
      var delayedShots = SystemAPI.GetSingletonBuffer<PendingBulletShot>();
      foreach (var delayedShot in pendingShots)
        delayedShots.Add(delayedShot);
      spawnEvents.Dispose();
      pendingShots.Dispose();
    }
  }
}
