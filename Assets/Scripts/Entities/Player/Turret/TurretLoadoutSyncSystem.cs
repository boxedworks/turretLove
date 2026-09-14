using Assets.Scripts.Bullets;
using Unity.Collections;
using Unity.Entities;

namespace Assets.Scripts.Entities.Player.Turret
{
  /// <summary>
  /// Copies managed persistent loadouts into the ECS magazine only between magazines.
  /// </summary>
  [UpdateInGroup(typeof(SimulationSystemGroup))]
  [UpdateAfter(typeof(Assets.Scripts.Entities.Skills.SkillSystem))]
  public partial class TurretLoadoutSyncSystem : SystemBase
  {
    protected override void OnCreate()
    {
      RequireForUpdate<TurretAmmo>();
    }

    protected override void OnUpdate()
    {
      var inventory = BulletInventoryService.Instance;
      var turrets = EntityManager.CreateEntityQuery(
          ComponentType.ReadWrite<TurretAmmo>(),
          ComponentType.ReadWrite<TurretMagazineSlot>())
        .ToEntityArray(Allocator.Temp);

      foreach (var turret in turrets)
      {
        var ammo = EntityManager.GetComponentData<TurretAmmo>(turret);
        var magazine = EntityManager.GetBuffer<TurretMagazineSlot>(turret);
        var needsInitialMagazine = magazine.Length != BulletInventoryService.EquippedSlotCount;
        var loadoutChanged = ammo.LoadoutRevision != inventory.LoadoutRevision;
        if (!needsInitialMagazine && (!loadoutChanged || ammo.CurrentAmmo != ammo.MagazineSize))
          continue;

        magazine.Clear();
        for (var slotIndex = 0; slotIndex < BulletInventoryService.EquippedSlotCount; slotIndex++)
        {
          if (!inventory.TryBuildMagazineSlot(inventory.GetEquippedBulletId(slotIndex), out var slot))
            slot = default;
          magazine.Add(slot);
        }

        ammo.MagazineSize = BulletInventoryService.EquippedSlotCount;
        if (ammo.CurrentAmmo > ammo.MagazineSize)
          ammo.CurrentAmmo = ammo.MagazineSize;
        if (ammo.CurrentAmmo == ammo.MagazineSize)
          ammo.CurrentSlotIndex = 0;
        ammo.LoadoutRevision = inventory.LoadoutRevision;
        EntityManager.SetComponentData(turret, ammo);
      }
      turrets.Dispose();
    }
  }
}
