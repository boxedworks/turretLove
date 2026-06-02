
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Entities.Game.Audio
{
  [CreateAssetMenu(fileName = "AudioDataGroup", menuName = "ScriptableObjects/AudioDataGroup", order = 1)]
  public class AudioDataGroupScriptableObject : ScriptableObject
  {
    public List<AudioDataScriptableObject> AudioDataList;
  }
}