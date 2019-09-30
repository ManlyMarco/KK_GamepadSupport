using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KK_GamepadSupport.Navigation
{
    public class CanvasState
    {
        public readonly Canvas Canvas;
        public readonly GraphicRaycaster Raycaster;
        private readonly Dictionary<CanvasGroup, bool> _groups;

        private bool? _lastEnabledVal;
        private readonly bool _isFullScreen;

        public CanvasState(Canvas canvas)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));

            Canvas = canvas;
            Raycaster = canvas.GetComponent<GraphicRaycaster>();
            // Force an update at start
            _lastEnabledVal = null;

            var crt = canvas.GetComponent<RectTransform>();

            if (FilterComponents(canvas.GetComponentsInChildren<Image>(false)).Any(x =>
            {
                var rt = x.GetComponent<RectTransform>();
                return rt.rect.width >= crt.rect.width - 1 && rt.rect.height >= crt.rect.height - 1;
            }))
            {
                _isFullScreen = true;
            }

            _groups = FilterComponents(canvas.GetComponentsInChildren<CanvasGroup>()).ToDictionary(x => x, x => false);

            if (Raycaster != null && HasSelectables())
                HookScrollRects();
        }

        public bool IsFullScreen => _isFullScreen && Canvas.isActiveAndEnabled;

        public int RenderOrder => Canvas.renderOrder;
        public int SortOrder => Canvas.sortingOrder;
        public bool Enabled => Raycaster.enabled && Canvas.isActiveAndEnabled;

        public IEnumerable<Selectable> GetSelectables(bool includeInactive)
        {
            return FilterComponents(Canvas.GetComponentsInChildren<Selectable>(includeInactive));
        }

        // Handle nested canvases
        private IEnumerable<T> FilterComponents<T>(IEnumerable<T> allComps) where T : Component
        {
            return allComps.Where(s => s.GetComponentInParent<Canvas>() == Canvas);
        }

        private static readonly UnityEngine.UI.Navigation _navOn = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Automatic };
        private static readonly UnityEngine.UI.Navigation _navOff = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };

        public bool UpdateNavigation(bool forceDisable)
        {
            var anyChanged = false;

            var isEnabled = !forceDisable && Enabled;
            if (isEnabled != _lastEnabledVal)
            {
                foreach (var selectable in GetSelectables(true))
                    selectable.navigation = isEnabled ? _navOn : _navOff;

                foreach (var x in _groups.ToList())
                    _groups[x.Key] = isEnabled;

                _lastEnabledVal = isEnabled;

                anyChanged = true;
            }

            foreach (var x in _groups.ToList())
            {
                var group = x.Key;
                var groupEnabled = group.interactable && group.alpha > 0.01f;
                if (groupEnabled != x.Value)
                {
                    _groups[group] = groupEnabled;

                    foreach (var selectable in FilterComponents(group.GetComponentsInChildren<Selectable>(true)))
                    {
                        if (!isEnabled)
                            selectable.navigation = _navOff;
                        else
                            selectable.navigation = groupEnabled ? _navOn : _navOff;
                    }

                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                if (NavigationIsEnabled)
                {
                    foreach (var scrollRect in FilterComponents(Canvas.GetComponentsInChildren<ScrollRect>(true)))
                        SetNavigableInScrollRect(scrollRect, false);
                }
            }

            return anyChanged;
        }

        public bool NavigationIsEnabled => _lastEnabledVal == true && Enabled;

        public bool HasSelectables() => Canvas.GetComponentInChildren<Selectable>() != null;

        #region ScrollRects

        private bool _destroyed;

        public void Dispose()
        {
            _destroyed = true;
        }

        private void HookScrollRects()
        {
            foreach (var sr in FilterComponents(Canvas.GetComponentsInChildren<ScrollRect>(true)))
            {
                var scrollRect = sr;
                scrollRect.onValueChanged.AddListener(val => SetNavigableInScrollRect(scrollRect, true));
            }
        }

        private void SetNavigableInScrollRect(ScrollRect sr, bool canEnable)
        {
            if (_destroyed) return;

            var srt = sr.GetComponent<RectTransform>();
            if (srt == null || sr.content == null) return;

            // Ignore dropdown lists, not necessary to calculate them and there's some issue with calculating visible items
            // todo normal dropdowns too?
            var isDropdown = sr.GetComponentInParent<TMP_Dropdown>() != null;

            // Scrollview coordinates are 0,0 in the center of viewport
            var maxOffset = srt.rect.height / 2;

            foreach (var listItem in sr.content.OfType<RectTransform>())
            {
                var selectable = listItem.GetComponentInChildren<Selectable>();
                if (selectable == null) continue;

                var isVisible = isDropdown || IsListItemMostlyVisible(srt, maxOffset, listItem);

                //listItem.GetComponentInChildren<Selectable>().interactable = isMostlyVisible;
                if (isVisible)
                {
                    if (!canEnable) continue;

                    var nav = selectable.navigation;
                    nav.mode = UnityEngine.UI.Navigation.Mode.Automatic;
                    selectable.navigation = nav;
                }
                else
                {
                    var nav = selectable.navigation;
                    nav.mode = UnityEngine.UI.Navigation.Mode.None;
                    selectable.navigation = nav;
                }
            }
        }

        private static bool IsListItemMostlyVisible(RectTransform srt, float maxOffset, RectTransform listItem)
        {
            var posY = srt.InverseTransformPoint(listItem.position).y;

            var height = listItem.sizeDelta.y;

            var itemVisibleThreshold = height / 2;

            var isMostlyVisible = posY - itemVisibleThreshold < maxOffset && -posY + itemVisibleThreshold < maxOffset;
            return isMostlyVisible;
        }

        #endregion
    }
}
