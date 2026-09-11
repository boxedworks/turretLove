
using Unity.Entities;
using Unity.Mathematics;
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
      var arrowUpState = GetButtonState(keyboard.upArrowKey.isPressed, inputState.ValueRO.ArrowUpState);
      var arrowDownState = GetButtonState(keyboard.downArrowKey.isPressed, inputState.ValueRO.ArrowDownState);
      var arrowLeftState = GetButtonState(keyboard.leftArrowKey.isPressed, inputState.ValueRO.ArrowLeftState);
      var arrowRightState = GetButtonState(keyboard.rightArrowKey.isPressed, inputState.ValueRO.ArrowRightState);
      var spaceState = GetButtonState(keyboard.spaceKey.isPressed, inputState.ValueRO.SpaceState);

      inputState.ValueRW.ArrowReleaseDirection = new float2(
          (arrowRightState == InputButtonState.Released ? 1f : 0f) -
          (arrowLeftState == InputButtonState.Released ? 1f : 0f),
          (arrowUpState == InputButtonState.Released ? 1f : 0f) -
          (arrowDownState == InputButtonState.Released ? 1f : 0f)
        );
      inputState.ValueRW.ArrowUpState = arrowUpState;
      inputState.ValueRW.ArrowDownState = arrowDownState;
      inputState.ValueRW.ArrowLeftState = arrowLeftState;
      inputState.ValueRW.ArrowRightState = arrowRightState;
      inputState.ValueRW.SpaceState = spaceState;
    }

    private static InputButtonState GetButtonState(bool isPressed, InputButtonState previousState)
    {
      if (isPressed)
        return previousState == InputButtonState.None || previousState == InputButtonState.Released
          ? InputButtonState.Pressed
          : InputButtonState.Held;

      return previousState == InputButtonState.Pressed || previousState == InputButtonState.Held
        ? InputButtonState.Released
        : InputButtonState.None;
    }
  }
}