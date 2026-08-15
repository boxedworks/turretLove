
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

      var mouse = Mouse.current;

      // Check mouse down
      var mousePosition = mouse.position.ReadValue();
      var mouseWorldPosition = CommonResources.s_MainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, CommonResources.s_MainCamera.transform.position.z));
      mouseWorldPosition.z = 0f;

      var mouse1Down = mouse.leftButton.isPressed;
      var keyboard = Keyboard.current;

      // Set state
      inputState.ValueRW.MouseWorldPosition = mouseWorldPosition;
      inputState.ValueRW.Mouse1Down = mouse1Down;
      inputState.ValueRW.ArrowUpDown = keyboard.upArrowKey.isPressed;
      inputState.ValueRW.ArrowDownDown = keyboard.downArrowKey.isPressed;
      inputState.ValueRW.ArrowLeftDown = keyboard.leftArrowKey.isPressed;
      inputState.ValueRW.ArrowRightDown = keyboard.rightArrowKey.isPressed;
    }
  }
}