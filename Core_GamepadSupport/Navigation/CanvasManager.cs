using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KKAPI;
using KKAPI.MainGame;
using StrayTech;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KK_GamepadSupport.Navigation
{
    public class CanvasManager
    {
        // Prefer to select controls from these canvases when looking for controls to select
        private static readonly HashSet<string> _preferredCanvasNames = new HashSet<string> {
            "CvsMainMenu", "ActionMenuCanvas", "Canvas_Main", 
 #if KKS
            "ExitDialog(Clone), ConfirmDialog(Clone)"
 #endif
        };

        private readonly List<CanvasState> _canvases = new List<CanvasState>();

        private static EventSystem CurrentEventSystem => EventSystem.current;

        public bool NeedsCanvasesRefresh;

        /// <summary>
        /// Get all components selectable by keyboard/gamepad input, in the order of input capure importance
        /// </summary>
        /// <param name="includeInactive">Include inactive Selectables in the search</param>
        /// <param name="topOnly">Only return results from the topmost canvas instead of all valid canvases</param>
        public IEnumerable<Selectable> GetAllSelectables(bool includeInactive, bool topOnly)
        {
            var canvases = GetSelectableCanvases(topOnly);
            return canvases.SelectMany(c => c.GetSelectables(includeInactive));
        }

        /// <summary>
        /// Get all valid canvases in the order of input capure importance
        /// </summary>
        /// <param name="topOnly">Only return the topmost canvas instead of all valid canvases</param>
        public IEnumerable<CanvasState> GetCanvases(bool topOnly)
        {
            _canvases.RemoveAll(x =>
            {
                var shouldRemove = x.Canvas == null;
                if (shouldRemove) x.Dispose();
                return shouldRemove;
            });
            var ordered = _canvases.OrderByDescending(x => x.Enabled).ThenByDescending(x => x.SortOrder).ThenByDescending(x => x.RenderOrder);
            return topOnly ? ordered.Take(1) : ordered;
        }

        /// <summary>
        /// Get all valid canvases that can be selected by keyboard/gamepad input, in the order of input capure importance
        /// </summary>
        /// <param name="topOnly">Only return the topmost canvas instead of all valid canvases</param>
        public IEnumerable<CanvasState> GetSelectableCanvases(bool topOnly = false)
        {
            var anyFullscreen = false;
            var canvases = GetCanvases(topOnly);
            return canvases
                .Where(x => x.Enabled)
                .TakeWhile(
                    c =>
                    {
                        if (anyFullscreen) return false;
                        if (c.IsFullScreen) anyFullscreen = true;
                        return true;
                    })
                .OrderByDescending(x => _preferredCanvasNames.Contains(x.Canvas.name));
        }

        /// <summary>
        /// Check if the selection is under a valid canvas
        /// </summary>
        public bool IsSelectionValid(GameObject selected)
        {
            if (_destroyed) return false;
            if (selected == null) return false;

            var selectableCanvases = GetSelectableCanvases();
            return selectableCanvases.Any(x => x.Canvas == selected.GetComponentInParent<Canvas>());
        }

        public void SelectFirstControl()
        {
            if (_destroyed) return;

            var canvases = GetSelectableCanvases();
            var cmps = canvases
                .Where(x => x.Canvas.name != "ActionCycleCanvas") // If this canvas gets selected first, there's danger of accidentally pressing A and advancing to next period
                .SelectMany(c => c.GetSelectables(false));

            var toSelect = cmps.FirstOrDefault(x => x.isActiveAndEnabled && x.navigation.mode != UnityEngine.UI.Navigation.Mode.None);
            if (toSelect != null)
            {
                //Logger.Log(LogLevel.Info, "select " + toSelect.transform.FullPath());
                toSelect.Select();
            }
            else
            {
                //Logger.Log(LogLevel.Info, "select null");
                if (CurrentEventSystem != null && CurrentEventSystem.currentSelectedGameObject != null)
                    CurrentEventSystem.SetSelectedGameObject(null);
            }
        }

        public bool UpdateAllNavigation()
        {
            if (_destroyed) return false;

            var canvases = GetCanvases(false).ToList();
            var fullscreenCanvas = canvases.Find(x => x.IsFullScreen);
            var minSort = fullscreenCanvas?.SortOrder ?? int.MinValue;
            var minRender = fullscreenCanvas?.RenderOrder ?? int.MinValue;

            var anyChanged = false;
            foreach (var state in canvases)
            {
                // Enable navigation only on canvases with sort order above the topmost fullscreen canvas
                // If sort order equals the topmost fullscreen canvas, enable navigation if render order is higher or equal to topmost fullscreen canvas
                var forceDisable = state.SortOrder == minSort ? state.RenderOrder < minRender : state.SortOrder < minSort;
                if (state.UpdateNavigation(forceDisable))
                    anyChanged = true;
            }
            return anyChanged;
        }

        public void UpdateCanvases()
        {
            if (_destroyed) return;

            ClearCanvasStates();

            var canvases = Object.FindObjectsOfType<Canvas>().AsEnumerable();
            var advScene = GameAPI.GetADVScene();
            if (advScene != null)
                canvases = canvases.Union(advScene.GetComponentsInChildren<Canvas>(true)); // Also finds disabled canvases, necessary in some cases
            foreach (var canvase in canvases)
            {
                var cs = new CanvasState(canvase);
                if (cs.Raycaster != null && cs.HasSelectables())
                {
                    if (cs.Canvas.renderMode == RenderMode.ScreenSpaceCamera)
                        cs.Raycaster.StartCoroutine(ChangeCanvasRenderModeCo(cs.Canvas));
                    _canvases.Add(cs);
                }
                else
                {
                    cs.Dispose();
                }
            }

            if (GamepadSupportPlugin.CanvasDebug.Value)
                GamepadSupportPlugin.Logger.LogInfo($"UpdateCanvases finished with {_canvases.Count} canvases found");

            NeedsCanvasesRefresh = false;
        }

        private static IEnumerator ChangeCanvasRenderModeCo(Canvas target)
        {
            // Need to wait until after loading to avoid the canvas becoming visible mid-loading
            yield return new WaitWhile(SceneApi.GetIsNowLoadingFade);

            GamepadSupportPlugin.Logger.LogWarning($"Canvas {target.transform.FullPath()} has overlay mode {target.renderMode}, changing to ScreenSpaceOverlay");
            target.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        private void ClearCanvasStates()
        {
            foreach (var canvasState in _canvases)
                canvasState.Dispose();

            _canvases.Clear();
        }

        private bool _destroyed;

        public void OnDestroy()
        {
            _destroyed = true;

            ClearCanvasStates();
        }
    }
}
