using System;
using System.Collections.Generic;
using System.Linq;
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

            if (canvas.GetComponentsInChildren<Image>(false).Any(x =>
            {
                var rt = x.GetComponent<RectTransform>();
                return rt.rect.width >= crt.rect.width - 1 && rt.rect.height >= crt.rect.height - 1;
            }))
            {
                _isFullScreen = true;
            }

            _groups = FilterComponents(canvas.GetComponentsInChildren<CanvasGroup>()).ToDictionary(x => x, x => false);
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

            return anyChanged;
        }

        public bool NavigationIsEnabled => _lastEnabledVal == true && Enabled;

        public bool HasSelectables() => Canvas.GetComponentInChildren<Selectable>() != null;
    }
}
