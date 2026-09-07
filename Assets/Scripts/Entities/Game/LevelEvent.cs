using Unity.Entities;

namespace Assets.Scripts.Entities.Game
{
  public partial struct LevelEvent : IBufferElementData
  {
    public enum EventType : byte
    {
      LevelStart,
      LevelEnd
    }

    public EventType Type;
  }

  public struct LevelState : IComponentData
  {
    public enum State : byte
    {
      Inactive,
      Running,
      Ended
    }

    public State CurrentState;
  }
}
