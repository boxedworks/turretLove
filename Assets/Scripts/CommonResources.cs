using UnityEngine;

namespace Assets.Scripts
{
  public class CommonResources
  {
    static CommonResources s_Singleton;

    Camera _mainCamera;
    public static Camera s_MainCamera { get { return s_Singleton._mainCamera; } }

    public CommonResources()
    {
      s_Singleton = this;

      _mainCamera = Camera.main;
    }
  }
}