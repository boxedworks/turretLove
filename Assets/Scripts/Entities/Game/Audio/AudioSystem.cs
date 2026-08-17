
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Game.Audio
{
  public partial class AudioSystem : SystemBase
  {

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
      EntityManager.AddBuffer<AudioEvent>(EntityManager.CreateEntity());

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
      var audioEventBuffer = SystemAPI.GetSingletonBuffer<AudioEvent>();
      if (audioEventBuffer.Length > 0)
      {
        var events = audioEventBuffer.ToNativeArray(Unity.Collections.Allocator.Temp);
        audioEventBuffer.Clear();

        foreach (var audioEvent in events)
        {
          switch (audioEvent.Type)
          {
            case AudioEvent.EventType.Shoot:
              PlaySound(0);
              break;
            case AudioEvent.EventType.EnemyDestroy:
              PlaySound(1);
              break;
            case AudioEvent.EventType.GoblinDamage:
              PlaySound(2);
              break;
            case AudioEvent.EventType.LootPickup:
              PlaySound(3, true);
              break;
          }
        }
        events.Dispose();
      }

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

    void PlaySound(int index, bool oneShot = false)
    {
      var audioData = _audioDataGroups["game"].AudioDataList[index];
      var pitch = audioData.Pitch + _random.NextFloat(-0.15f, 0.15f);

      if (oneShot)
      {
        // Reuse an active source playing the same clip to avoid consuming a pool slot
        foreach (var active in _activeAudioSources)
        {
          if (active.Source.clip == audioData.Sfx)
          {
            active.Source.PlayOneShot(audioData.Sfx, audioData.Volume);
            return;
          }
        }
      }

      if (_audioSourcePool.Count == 0)
      {
        Debug.LogError($"Cannot play sound: No available audio source in the pool for index {index} / {_audioSourcePool.Count}");
        return;
      }

      var audioClip = _audioSourcePool.Dequeue();
      _activeAudioSources.Add(audioClip);
      audioClip.Source.clip = audioData.Sfx;
      audioClip.Source.volume = audioData.Volume;
      audioClip.Source.pitch = pitch;

      if (oneShot)
        audioClip.Source.PlayOneShot(audioData.Sfx, audioData.Volume);
      else
        audioClip.Source.Play();
    }
  }

  public struct AudioEvent : IBufferElementData
  {
    public enum EventType
    {
      Shoot,
      EnemyDestroy,
      GoblinDamage,
      LootPickup
    }

    public EventType Type;
  }
}