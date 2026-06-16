
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Game.Audio
{
  public partial class AudioSystem : SystemBase
  {

    DynamicBuffer<AudioEvent> _audioEvents;
    Queue<CustomAudioSource> _audioSourcePool;
    List<CustomAudioSource> _activeAudioSources;
    Dictionary<string, AudioDataGroupScriptableObject> _audioDataGroups;
    class CustomAudioSource
    {
      public AudioSource Source;
      public float Pitch, Volume;
    }

    Unity.Mathematics.Random _random;

    protected override void OnCreate()
    {
      var audioEventsEntity = EntityManager.CreateEntity();
      EntityManager.AddBuffer<AudioEvent>(audioEventsEntity);

      _audioSourcePool = new();
      _activeAudioSources = new();

      var audioContainer = new GameObject("AudioContainer");
      for (var i = 0; i < 10; i++)
      {
        var audioSource = new GameObject("AudioSource").AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.transform.parent = audioContainer.transform;
        _audioSourcePool.Enqueue(new CustomAudioSource { Source = audioSource });
      }

      // Load audio data groups from resources
      _audioDataGroups = new();
      var audioDataGroupAsset = Resources.Load<AudioDataGroupScriptableObject>("AudioGroups/game");
      _audioDataGroups["game"] = audioDataGroupAsset;

      //
      _random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
    }

    protected override void OnUpdate()
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
            PlaySound();
            break;
          case AudioEvent.EventType.EnemyDeath:
            PlaySound();
            break;
        }
      }
      _audioEvents.Clear();

      // Recycle finished audio sources
      for (var i = _activeAudioSources.Count - 1; i >= 0; i--)
      {
        var audioClip = _activeAudioSources[i];
        if (!audioClip.Source.isPlaying)
        {
          _activeAudioSources.RemoveAt(i);
          _audioSourcePool.Enqueue(audioClip);
        }
      }
    }

    //
    void PlaySound()
    {
      if (_audioSourcePool.Count == 0)
      {
        Debug.LogWarning("No available audio sources in pool!");
        return;
      }

      var audioClip = _audioSourcePool.Dequeue();
      _activeAudioSources.Add(audioClip);
      var audioData = _audioDataGroups["game"].AudioDataList[0];
      audioClip.Source.clip = audioData.Sfx;
      audioClip.Source.volume = audioData.Volume;
      audioClip.Source.pitch = audioData.Pitch + _random.NextFloat(-0.15f, 0.15f);
      audioClip.Source.Play();
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