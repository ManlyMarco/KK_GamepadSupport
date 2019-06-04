using UnityEngine;

namespace KK_GamepadSupport.Navigation
{
    public class CursorDrawer
    {
        private Texture2D _pointer;

        public void LoadTexture()
        {
            _pointer = new Texture2D(1, 1, TextureFormat.DXT5, false, false);
            _pointer.LoadImage(Properties.Resources.pointer);
            Properties.Resources.ResourceManager.ReleaseAllResources();

            const int recommendedHeight = 900;

            if (Mathf.Abs(Screen.height - recommendedHeight) > 100)
            {
                var scaleFactor = Screen.height / recommendedHeight;
                _pointer.Resize(_pointer.width * scaleFactor, _pointer.height * scaleFactor);
                _pointer.Apply(false);
            }
        }

        public void Draw(GameObject currentSelectedGameObject, bool poke)
        {
            if (currentSelectedGameObject == null) return;
            if (Manager.Scene.Instance.IsNowLoadingFade) return;

            var camera = Camera.current ?? Camera.main;
            if (camera == null) return;

            var r = RectTransformToScreenSpace(currentSelectedGameObject.GetComponent<RectTransform>());
            var pixelPosition = new Vector2(r.xMin, r.yMin + (r.yMax - r.yMin) / 2);

            Draw(pixelPosition, poke);
        }

        public void Draw(Vector2 pixelPosition, bool poke)
        {
            const float restTime = 1.5f;
            const float pokeTime = 0.5f;

            var offset = 0f;

            if (poke)
            {
                offset = Time.unscaledTime % (restTime + pokeTime);
                if (offset > pokeTime)
                {
                    offset = 0f;
                }
                else
                {
                    var scaled = offset * (Mathf.PI * 2 / pokeTime);
                    offset = (Mathf.Cos(scaled) - 1) * -5;
                }
            }

            GUI.Label(new Rect(Mathf.Max(0, pixelPosition.x - _pointer.width) + offset, pixelPosition.y - _pointer.height / 3f, _pointer.width, _pointer.height), _pointer);
        }

        private static Rect RectTransformToScreenSpace(RectTransform transform)
        {
            var size = Vector2.Scale(transform.rect.size, transform.lossyScale);
            var rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
            rect.x -= transform.pivot.x * size.x;
            rect.y -= (1.0f - transform.pivot.y) * size.y;
            return rect;
        }
    }
}