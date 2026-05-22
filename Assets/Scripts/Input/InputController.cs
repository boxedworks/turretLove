

using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.Input
{
  public class InputController
  {
    public static InputController s_Singleton;

    Vector3 _mousePosition;
    public static Vector3 s_MouseWorldPosition { get { return s_Singleton._mousePosition; } }

    bool _isMouse1Down;
    public static bool s_IsMouse1Down { get { return s_Singleton._isMouse1Down; } }

    public InputController()
    {
      s_Singleton = this;
    }

    public void Update()
    {
      var mousePosition = Mouse.current.position.ReadValue();
      _mousePosition = CommonResources.s_MainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, CommonResources.s_MainCamera.transform.position.z));

      _isMouse1Down = Mouse.current.leftButton.isPressed;
    }

  }
}