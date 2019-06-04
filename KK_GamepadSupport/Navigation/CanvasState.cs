using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using StrayTech;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_GamepadSupport.Navigation
{
    public class CanvasState
    {
        public readonly Canvas Canvas;
        public readonly GraphicRaycaster Raycaster;

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
        }

        public bool IsFullScreen => _isFullScreen && Canvas.isActiveAndEnabled;

        public int RenderOrder => Canvas.renderOrder;
        public int SortOrder => Canvas.sortingOrder;
        public bool Enabled => Raycaster.enabled && Canvas.isActiveAndEnabled;

        public IEnumerable<Selectable> GetSelectables(bool includeInactive)
        {
            var allComps = Canvas.GetComponentsInChildren<Selectable>(includeInactive);
            // Handle nested canvases
            return allComps.Where(s => s.GetComponentInParent<Canvas>() == Canvas);
        }

        public bool UpdateNavigation(bool forceDisable)
        {
            var isEnabled = !forceDisable && Enabled;
            if (isEnabled != _lastEnabledVal)
            {
                var nav = new UnityEngine.UI.Navigation { mode = isEnabled ? UnityEngine.UI.Navigation.Mode.Automatic : UnityEngine.UI.Navigation.Mode.None };
                foreach (var selectable in GetSelectables(true))
                    selectable.navigation = nav;

                _lastEnabledVal = isEnabled;

                return true;
            }

            return false;
        }

        public bool NavigationIsEnabled => _lastEnabledVal == true && Enabled;

        public bool HasSelectables() => Canvas.GetComponentInChildren<Selectable>() != null;
    }
}
