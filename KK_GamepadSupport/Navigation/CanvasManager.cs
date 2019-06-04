using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Manager;
using StrayTech;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_GamepadSupport.Navigation
{
    public class CanvasManager
    {
        private readonly List<CanvasState> _canvases = new List<CanvasState>();

        private static EventSystem CurrentEventSystem => EventSystem.current;

        public IEnumerable<Selectable> GetAllSelectables(bool includeInactive, bool topOnly)
        {
            var canvases = GetSelectableCanvases(topOnly);
            return canvases.SelectMany(c => c.GetSelectables(includeInactive));
        }

        public IEnumerable<CanvasState> GetCanvases(bool topOnly)
        {
            _canvases.RemoveAll(x => x.Canvas == null);
            var ordered = _canvases.OrderByDescending(x => x.Canvas.isActiveAndEnabled).ThenByDescending(x => x.SortOrder).ThenByDescending(x => x.RenderOrder);
            return topOnly ? ordered.Take(1) : ordered;
        }

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
                    });
        }

        /// <summary>
        /// Check if the selection is under a valid canvas
        /// </summary>
        public bool IsSelectionValid(GameObject selected)
        {
            if (selected == null) return false;
            var selectableCanvases = GetSelectableCanvases();
            return selectableCanvases.Any(x => x.Canvas == selected.GetComponentInParent<Canvas>());
        }

        public void SelectControl()
        {
            var toSelect = GetAllSelectables(false, false).FirstOrDefault(x => x.isActiveAndEnabled);
            if (toSelect != null)
            {
                //Logger.Log(LogLevel.Info, "select " + toSelect.transform.FullPath());
                toSelect.Select();
            }
            else
            {
                //Logger.Log(LogLevel.Info, "select null");
                if (CurrentEventSystem?.currentSelectedGameObject != null)
                    CurrentEventSystem.SetSelectedGameObject(null);
            }
        }
        
        public bool UpdateAllNavigation()
        {
            var canvases = GetCanvases(false).ToList();
            var fullscreenCanvas = canvases.Find(x => x.IsFullScreen);
            var minSort = fullscreenCanvas != null ? fullscreenCanvas.SortOrder : int.MinValue;
            var minRender = fullscreenCanvas != null ? fullscreenCanvas.RenderOrder : int.MinValue;

            // Enable navigation only on canvases with sort order above the topmost fullscreen canvas
            // If sort order equals the topmost fullscreen canvas, enable navigation if render order is higher or equal to topmost fullscreen canvas
            return canvases.Count(state => state.UpdateNavigation(state.SortOrder == minSort ? state.RenderOrder < minRender : state.SortOrder < minSort)) > 0;
        }

        public void UpdateCanvases()
        {
            _canvases.Clear();

            foreach (var canvase in Object.FindObjectsOfType<Canvas>().Concat(Game.Instance?.actScene?.AdvScene?.GetComponentsInChildren<Canvas>(true) ?? new Canvas[0]).Distinct())
            {
                var cs = new CanvasState(canvase);
                if (cs.Raycaster != null && cs.HasSelectables())
                {
                    if (cs.Canvas.renderMode == RenderMode.ScreenSpaceCamera)
                        cs.Raycaster.StartCoroutine(ChangeCanvasRenderModeCo(cs.Canvas));
                    _canvases.Add(cs);
                }
            }

            HookScrollRects();
        }

        private static IEnumerator ChangeCanvasRenderModeCo(Canvas target)
        {
            // Need to wait until after loading to avoid the canvas becoming visible mid-loading
            yield return new WaitWhile(() => Scene.Instance.IsNowLoadingFade);

            Logger.Log(LogLevel.Info, $"Canvas {target.transform.FullPath()} has overlay mode {target.renderMode}, changing to ScreenSpaceOverlay");
            target.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        private void HookScrollRects()
        {
            foreach (var scrollRect in _canvases.SelectMany(c => c.Canvas.GetComponentsInChildren<ScrollRect>()))
                scrollRect.GetOrAddComponent<ScrollToSelection>();
        }

        public void OnDestroy()
        {
            foreach (var scrollToSelection in Object.FindObjectsOfType<ScrollToSelection>())
                Object.Destroy(scrollToSelection);
        }
    }
}
