using Assets.Scripts.Input;
using UnityEngine;

namespace Assets.Scripts
{

  public class GameController : MonoBehaviour
  {


    public static GameController s_Singleton;

    // Start is called before the first frame update
    void Start()
    {
      s_Singleton = this;

      new CommonResources();
    }

    // Update is called once per frame
    void Update()
    {
    }
  }

}