using Assets.Scripts.Entities.Player.Character;
using Assets.Scripts.Entities.Player.Turret;
using Assets.Scripts.Entities.Skills;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Assets.Scripts.Entities.Game
{
  /// <summary>
  /// Handles level lifecycle events: level start and level end.
  /// Spawns player and turret on level start, and cleans up entities on level end.
  /// </summary>
  public partial struct LevelLifecycleSystem : ISystem
  {
    public void OnCreate(ref SystemState state)
    {
      // Create the LevelState singleton if it doesn't exist
      var levelStateEntity = state.EntityManager.CreateEntity();
      state.EntityManager.AddComponentData(levelStateEntity, new LevelState { CurrentState = LevelState.State.Inactive });
      state.EntityManager.AddBuffer<LevelEvent>(levelStateEntity);

      state.RequireForUpdate<PlayerSpawner>();
      state.RequireForUpdate<TurretSpawner>();
    }

    public void OnUpdate(ref SystemState state)
    {
      var levelStateEntity = SystemAPI.GetSingletonEntity<LevelState>();
      var levelStateRef = SystemAPI.GetComponentRW<LevelState>(levelStateEntity);
      var eventBuffer = SystemAPI.GetSingletonBuffer<LevelEvent>();

      // Copy events before structural changes invalidate the buffer handle
      var events = eventBuffer.ToNativeArray(Allocator.Temp);
      eventBuffer.Clear();

      // Process level events
      foreach (var levelEvent in events)
      {
        if (levelEvent.Type == LevelEvent.EventType.LevelStart)
        {
          if (levelStateRef.ValueRO.CurrentState != LevelState.State.Running)
          {
            levelStateRef.ValueRW.CurrentState = LevelState.State.Running;
            SpawnPlayerAndTurret(ref state);
          }
        }
        else if (levelEvent.Type == LevelEvent.EventType.LevelEnd)
        {
          levelStateRef.ValueRW.CurrentState = LevelState.State.Ended;
        }
      }

      events.Dispose();
    }

    private void SpawnPlayerAndTurret(ref SystemState state)
    {
      // Spawn player
      var playerSpawner = SystemAPI.GetSingleton<PlayerSpawner>();
      var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

      var player = ecb.Instantiate(playerSpawner.PlayerPrefab);
      ecb.SetComponent(player, LocalTransform.FromPositionRotationScale(new float3(1f, 0f, 0f), quaternion.identity, 0.5f));
      ecb.AddComponent(player, new PlayerAttributes
      {
        MaxHealth = 100f,
        CurrentHealth = 100f,
        Damage = 1f,
        AttackSpeed = 1f
      });
      ecb.AddBuffer<DamageEvent>(player);
      ecb.AddBuffer<KnockbackEvent>(player);
      var skills = ecb.AddBuffer<Skill>(player);
      skills.Add(new Skill
      {
        Type = SkillType.Dash,
        MaxUses = 2,
        RemainingUses = 2,
        EffectStrength = 4f,
        RechargeDuration = 1f
      });
      ecb.AddBuffer<SkillTriggerEvent>(player);
      ecb.AddComponent(player, new LevelEntity());

      ecb.Playback(state.EntityManager);
      ecb.Dispose();

      // Spawn turret
      var turretSpawner = SystemAPI.GetSingleton<TurretSpawner>();
      var turretTopInstance = state.EntityManager.Instantiate(turretSpawner.TurretTopPrefab);
      var turretBaseInstance = state.EntityManager.Instantiate(turretSpawner.TurretBasePrefab);

      state.EntityManager.AddComponent<TurretTop>(turretTopInstance);
      state.EntityManager.AddComponentData(turretTopInstance, new TurretAttributes
      {
        RotationSpeed = 1f,
        FireRate = 0.5f,
        TimeSinceLastShot = 0f
      });
      state.EntityManager.AddComponent<TurretBase>(turretBaseInstance);
      state.EntityManager.AddComponentData(turretBaseInstance, new TurretHealth
      {
        MaxHealth = 100f,
        CurrentHealth = 100f
      });
      state.EntityManager.AddBuffer<DamageEvent>(turretBaseInstance);
      state.EntityManager.AddComponent<LevelEntity>(turretBaseInstance);
      state.EntityManager.AddComponent<LevelEntity>(turretTopInstance);

      // Set initial position of turret
      var turretPosition = float3.zero;
      state.EntityManager.SetComponentData(turretTopInstance, LocalTransform.FromPositionRotationScale(turretPosition + new float3(0, 0, -0.1f), quaternion.identity, 0.5f));
      state.EntityManager.SetComponentData(turretBaseInstance, LocalTransform.FromPositionRotationScale(turretPosition, quaternion.identity, 0.5f));

      // Change collision layer to avoid colliding with the bullet
      var physicsCollider = SystemAPI.GetComponentRW<PhysicsCollider>(turretBaseInstance);
      var collider = physicsCollider.ValueRO.Value;
      var filter = collider.Value.GetCollisionFilter();
      filter.BelongsTo = 1 << 1;
      filter.CollidesWith = (1 << 0) | (1 << 4) | (1 << 6);
      collider.Value.SetCollisionFilter(filter);
      physicsCollider.ValueRW.Value = collider;
    }
  }
}
