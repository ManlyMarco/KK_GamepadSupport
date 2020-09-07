using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Manager;
using StrayTech;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Scene = UnityEngine.SceneManagement.Scene;

namespace KK_GamepadSupport.Navigation
{
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, "1.12")]
    [BepInPlugin(Guid, Guid, Metadata.Version)]
    public partial class CanvasCharmer : BaseUnityPlugin
    {
        private const string Guid = Metadata.BaseGuid + ".CanvasCharmer";

        private const float CursorTimeout = 90f;
        private const float CursorPokeStart = 4f;

        private float _timeSinceLastAction;

        private static EventSystem CurrentEventSystem => EventSystem.current;

        private CursorDrawer CursorDrawer { get; } = new CursorDrawer();
        private CanvasManager CanvasManager { get; } = new CanvasManager();

        internal static new ManualLogSource Logger;
        private static CanvasCharmer _instance;

        public static ConfigEntry<bool> CanvasDebug { get; private set; }

        private void Awake()
        {
            CanvasDebug = Config.Bind("Debug", "Show debug information", false, new ConfigDescription("", null, "Advanced"));
            _instance = this;
            Logger = base.Logger;

            CursorDrawer.LoadTexture();

            Hooks.InitHooks();
        }

        private void Start()
        {
            CanvasManager.UpdateCanvases();

            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
            SceneManager.sceneUnloaded += SceneManagerOnSceneUnloaded;

            _timeSinceLastAction = CursorTimeout;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
            SceneManager.sceneUnloaded -= SceneManagerOnSceneUnloaded;

            CanvasManager.OnDestroy();

            Hooks.RemoveHooks();
        }

        private void OnGUI()
        {
            var currentSelectedGameObject = CurrentEventSystem == null ? null : CurrentEventSystem.currentSelectedGameObject;

            if (_timeSinceLastAction < CursorTimeout)
                CursorDrawer.Draw(currentSelectedGameObject, _timeSinceLastAction > CursorPokeStart);

            if (CanvasDebug.Value)
                DrawCanvasList(currentSelectedGameObject);
        }

        private void Update()
        {
            if (CurrentEventSystem == null || CurrentEventSystem.currentInputModule == null) return;

            var selected = CurrentEventSystem.currentSelectedGameObject;

            if (SceneIsLoading() || ShouldDisableNavigation())
            {
                if (selected != null)
                    CurrentEventSystem.SetSelectedGameObject(null);
                return;
            }

            var input = CurrentEventSystem.currentInputModule.input;
            if (Mathf.Abs(input.GetAxisRaw("Horizontal")) > 0.01f || Mathf.Abs(input.GetAxisRaw("Vertical")) > 0.01f || input.GetButtonDown("Submit"))
            {
                // Do not show the cursor or select anything if we are currently editing a text field
                if (selected != null && selected.GetComponent<InputField>()?.isFocused == true)
                    return;

                _timeSinceLastAction = 0f;
            }
            else
            {
                // Wait until next frame to check if current selection is still valid after pressing a button
                if (_timeSinceLastAction.Equals(0f))
                {
                    if (!CanvasManager.IsSelectionValid(selected))
                    {
                        if (CanvasDebug.Value) Logger.Log(LogLevel.Message, "invalid selection");
                        selected = null;
                    }
                }
                _timeSinceLastAction = Mathf.Min(_timeSinceLastAction + Time.deltaTime, CursorTimeout);
            }

            // When using mouse hide immediately
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(3))
                _timeSinceLastAction = CursorTimeout;

            if (_timeSinceLastAction >= CursorTimeout)
                return;

            if (CanvasManager.UpdateAllNavigation())
            {
                if (!CanvasManager.IsSelectionValid(selected))
                {
                    if (CanvasDebug.Value) Logger.Log(LogLevel.Message, "invalid selection");
                    selected = null;
                }
            }

            if (selected == null)
            {
                CanvasManager.SelectFirstControl();
            }
            else
            {
                var selectable = selected.GetComponent<Selectable>();

                if (selectable == null)
                {
                    if (CanvasDebug.Value) Logger.Log(LogLevel.Message, "not a Selectable");
                    CanvasManager.SelectFirstControl();
                }
                else
                {
                    if (!selectable.isActiveAndEnabled)
                    {
                        if (CanvasDebug.Value) Logger.Log(LogLevel.Message, "object not isActiveAndEnabled");
                        // Needed for some transitions, e.g. live mode
                        CanvasManager.UpdateCanvases();
                        CanvasManager.SelectFirstControl();
                    }
                }
            }
        }

        private static bool ShouldDisableNavigation()
        {
            if (Singleton<Game>.IsInstance())
            {
                var gameInstance = Singleton<Game>.Instance;
                var actScene = gameInstance.actScene;
                if (actScene != null && actScene.Player != null)
                {
                    var disableCanvasInteractions = actScene.isCursorLock && !actScene.Player.move.isReglateMove && !gameInstance.IsRegulate(true);
                    return disableCanvasInteractions;
                }
            }
            return false;
        }

        private void SceneManagerOnSceneLoaded(Scene arg0, LoadSceneMode loadSceneMode)
        {
            StartCoroutine(SceneLoadedCo(arg0.name == "Talk"));
        }

        private void SceneManagerOnSceneUnloaded(Scene arg0)
        {
            StartCoroutine(SceneLoadedCo(false));
        }

        private IEnumerator SceneLoadedCo(bool isTalk)
        {
            yield return new WaitWhile(SceneIsLoading);
            yield return null;
            yield return null;
            if (isTalk)
            {
                yield return new WaitWhile(() =>
                    Game.Instance.actScene == null ||
                    Game.Instance.actScene.AdvScene == null ||
                    Game.Instance.actScene.AdvScene.transform.Find("Canvas_Main") == null);
            }

            CanvasManager.UpdateCanvases();
            CanvasManager.SelectFirstControl();
        }

        private static bool SceneIsLoading()
        {
            if (Manager.Scene.Instance.IsNowLoadingFade)
                return true;

            if (Communication.IsInstance())
            {
                if (!Communication.Instance.isInit)
                    return true;
            }

            return false;
        }

        private GUIStyle _canvasBoxStyle;
        private void DrawCanvasList(GameObject currentSelectedGameObject)
        {
            if (_canvasBoxStyle == null)
            {
                _canvasBoxStyle = new GUIStyle(GUI.skin.box);
                _canvasBoxStyle.alignment = TextAnchor.UpperLeft;
                _canvasBoxStyle.normal.textColor = Color.white;
            }

            GUILayout.BeginArea(new Rect(900, 0, 700, 400));
            {
                GUILayout.BeginVertical();
                {
                    foreach (var x in CanvasManager.GetCanvases(false))
                    {
                        var backup = GUI.color;
                        if (currentSelectedGameObject != null && currentSelectedGameObject.GetComponentInParent<Canvas>() == x.Canvas)
                            GUI.color = Color.yellow;
                        else if (!x.Enabled)
                            GUI.color = Color.gray;
                        else if (x.NavigationIsEnabled)
                            GUI.color = Color.green;

                        GUILayout.Label($"{x.Canvas.gameObject.FullPath()} sort={x.SortOrder} render={x.RenderOrder} full={x.IsFullScreen} enabled={x.Enabled}", _canvasBoxStyle);

                        GUI.color = backup;
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }
    }
}
