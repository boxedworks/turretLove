
using UnityEngine;

namespace Assets.Scripts.Entities.Game.Audio
{
  [CreateAssetMenu(fileName = "AudioData", menuName = "ScriptableObjects/AudioData", order = 1)]
  public class AudioDataScriptableObject : ScriptableObject
  {
    public AudioClip Sfx;
    public float Volume = 1f, Pitch = 1f;
  }
}