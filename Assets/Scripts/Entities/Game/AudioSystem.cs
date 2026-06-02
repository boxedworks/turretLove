
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Game
{
  [UpdateInGroup(typeof(SimulationSystemGroup))]
  public partial struct AudioSystem : ISystem
  {

    DynamicBuffer<AudioEvent> _audioEvents;

    public readonly void OnCreate(ref SystemState state)
    {
      var audioEventsEntity = state.EntityManager.CreateEntity();
      state.EntityManager.AddBuffer<AudioEvent>(audioEventsEntity);
    }

    public void OnUpdate(ref SystemState state)
    {

      // Play all sounds in buffer by creating new audio source (for now)
      _audioEvents = SystemAPI.GetBuffer<AudioEvent>(SystemAPI.GetSingletonEntity<AudioEvent>());
      if (_audioEvents.Length == 0)
        return;
      foreach (var audioEvent in _audioEvents)
      {
        switch (audioEvent.Type)
        {
          case AudioEvent.EventType.Shoot:
            // Play shoot sound
            Debug.Log("Play shoot sound");
            break;
          case AudioEvent.EventType.EnemyDeath:
            // Play enemy death sound
            Debug.Log("Play enemy death sound");
            break;
        }
      }
      _audioEvents.Clear();
    }
  }

  public struct AudioEvent : IBufferElementData
  {
    public enum EventType
    {
      Shoot,
      EnemyDeath
    }

    public EventType Type;
  }
}