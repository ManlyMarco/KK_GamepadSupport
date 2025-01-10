using KKAPI;
using KKAPI.Utilities;
using UnityEngine;

namespace KK_GamepadSupport.Navigation
{
    public class CursorDrawer
    {
        private Texture2D _pointer;
        private Texture2D _mousePointer;

        public void LoadTexture()
        {
            _pointer = ResourceUtils.GetEmbeddedResource("pointer.png", typeof(CursorDrawer).Assembly).LoadTexture();
            _mousePointer = ResourceUtils.GetEmbeddedResource("pointer.png", typeof(CursorDrawer).Assembly).LoadTexture();

            const int idealResolutionH = 1800;

            if (Mathf.Abs(Screen.height - idealResolutionH) >= 200)
            {
                var scaleFactor = (float)Screen.height / idealResolutionH;
                TextureScale.Bilinear(_pointer, (int)(_pointer.width * scaleFactor), (int)(_pointer.height * scaleFactor));
                _pointer.Apply(false);

                TextureScale.Bilinear(_mousePointer, (int)(_mousePointer.width * scaleFactor), (int)(_mousePointer.height * scaleFactor));
                _mousePointer.Apply(false);
            }
        }

        public void Draw(GameObject currentSelectedGameObject, bool poke)
        {
            if (currentSelectedGameObject == null) return;
            if (SceneApi.GetIsNowLoadingFade()) return;

            var camera = Camera.current;
            if (camera == null) camera = Camera.main;
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

        public void DrawMousePointer(Vector3 mousePosition)
        {
            GUI.Label(new Rect(mousePosition.x - _mousePointer.width / 2, Screen.height - mousePosition.y - _mousePointer.height / 2, _mousePointer.width, _mousePointer.height), _mousePointer);
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