
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Input
{
  public partial struct InputSystem : ISystem
  {

    public readonly void OnCreate(ref SystemState state)
    {
      var inputEntity = state.EntityManager.CreateEntity();
      state.EntityManager.AddComponent<InputState>(inputEntity);
    }

    public readonly void OnUpdate(ref SystemState state)
    {
      var inputState = SystemAPI.GetSingletonRW<InputState>();

      // Check mouse down
      var mousePosition = Mouse.current.position.ReadValue();
      var mouseWorldPosition = CommonResources.s_MainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, CommonResources.s_MainCamera.transform.position.z));

      var mouse1Down = Mouse.current.leftButton.isPressed;

      // Set state
      inputState.ValueRW.MouseWorldPosition = mouseWorldPosition;
      inputState.ValueRW.Mouse1Down = mouse1Down;
    }
  }
}