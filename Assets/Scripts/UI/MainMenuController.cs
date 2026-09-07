using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Assets.Scripts.Entities.Game;

namespace Assets.Scripts.UI
{
  public class MainMenuController : MonoBehaviour
  {
    [SerializeField] private PanelRenderer panelRenderer;
    private VisualElement menuScreen;
    private Label statusLabel;
    private Button playButton;
    private Button optionsButton;
    private Button exitButton;

    private void OnEnable()
    {
      if (panelRenderer != null)
      {
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
      }
    }

    private void OnDisable()
    {
      if (panelRenderer == null)
      {
        return;
      }

      panelRenderer.UnregisterUIReloadCallback(OnUIReload);
      UnregisterButtonCallbacks();
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
    {
      menuScreen = root.Q<VisualElement>("menu-screen");
      statusLabel = root.Q<Label>("status-label");
      playButton = root.Q<Button>("play-button");
      optionsButton = root.Q<Button>("options-button");
      exitButton = root.Q<Button>("exit-button");

      playButton?.RegisterCallback<ClickEvent>(OnPlayClicked);
      optionsButton?.RegisterCallback<ClickEvent>(OnOptionsClicked);
      exitButton?.RegisterCallback<ClickEvent>(OnExitClicked);
    }

    private void UnregisterButtonCallbacks()
    {
      playButton?.UnregisterCallback<ClickEvent>(OnPlayClicked);
      optionsButton?.UnregisterCallback<ClickEvent>(OnOptionsClicked);
      exitButton?.UnregisterCallback<ClickEvent>(OnExitClicked);
    }

    private void OnPlayClicked(ClickEvent clickEvent)
    {
      menuScreen.style.display = DisplayStyle.None;
      StartGame();
    }

    private void OnOptionsClicked(ClickEvent clickEvent)
    {
      statusLabel.text = "Options will be available soon.";
    }

    private void OnExitClicked(ClickEvent clickEvent)
    {
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }

    private void StartGame()
    {
      // Get the default world and send LevelStart event
      var world = World.DefaultGameObjectInjectionWorld;
      if (world == null || !world.IsCreated)
        return;

      // Get the existing LevelState singleton created by LevelLifecycleSystem
      var levelStateEntity = world.EntityManager.CreateEntityQuery(typeof(LevelState)).GetSingletonEntity();
      var eventBuffer = world.EntityManager.GetBuffer<LevelEvent>(levelStateEntity);
      eventBuffer.Add(new LevelEvent { Type = LevelEvent.EventType.LevelStart });
    }
  }
}
