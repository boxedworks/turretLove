using Assets.Scripts.Bullets;
using Unity.Entities;

namespace Assets.Scripts.Entities.Loot
{
  public struct LootCollectedEvent : IBufferElementData
  {
    public LootType Type;
    public int Amount;
  }

  /// <summary>
  /// Bridges Burst-safe loot events to the persistent managed inventory.
  /// </summary>
  [UpdateInGroup(typeof(SimulationSystemGroup))]
  [UpdateAfter(typeof(LootMovementSystem))]
  public partial class LootInventorySystem : SystemBase
  {
    protected override void OnCreate()
    {
      var inventoryEvents = EntityManager.CreateEntity();
      EntityManager.AddBuffer<LootCollectedEvent>(inventoryEvents);
    }

    protected override void OnUpdate()
    {
      var events = SystemAPI.GetSingletonBuffer<LootCollectedEvent>();
      if (events.Length == 0)
        return;

      var inventory = BulletInventoryService.Instance;
      for (var index = 0; index < events.Length; index++)
        inventory.AddLoot(events[index].Type, events[index].Amount, out _);
      events.Clear();
    }
  }
}
