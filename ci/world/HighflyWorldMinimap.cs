using UnityEngine;
using UnityEngine.UI;

namespace Highfly.World
{
    [DisallowMultipleComponent]
    public sealed class HighflyWorldMinimap : MonoBehaviour
    {
        private Camera _mapCamera;
        private RenderTexture _texture;
        private PlayerController _player;
        private RectTransform _arrow;

        private void Start()
        {
            Build();
        }

        private void Build()
        {
            _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return;

            var camGo = new GameObject("HIGHFLY_MINIMAP_CAMERA");
            camGo.transform.SetParent(transform, false);
            _mapCamera = camGo.AddComponent<Camera>();
            _mapCamera.orthographic = true;
            _mapCamera.orthographicSize = 38f;
            _mapCamera.nearClipPlane = 0.3f;
            _mapCamera.farClipPlane = 140f;
            _mapCamera.clearFlags = CameraClearFlags.SolidColor;
            _mapCamera.backgroundColor = new Color(0.06f, 0.08f, 0.09f, 1f);
            _mapCamera.depth = -20f;
            _mapCamera.cullingMask = ~0;

            _texture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
            {
                name = "HIGHFLY_MINIMAP_RT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            _texture.Create();
            _mapCamera.targetTexture = _texture;

            var canvasGo = new GameObject("HIGHFLY_MINIMAP_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4900;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("MapPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-24f, -24f);
            panelRt.sizeDelta = new Vector2(224f, 224f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

            var mapGo = new GameObject("Map", typeof(RectTransform), typeof(RawImage));
            mapGo.transform.SetParent(panel.transform, false);
            var mapRt = mapGo.GetComponent<RectTransform>();
            mapRt.anchorMin = Vector2.zero;
            mapRt.anchorMax = Vector2.one;
            mapRt.offsetMin = new Vector2(8f, 8f);
            mapRt.offsetMax = new Vector2(-8f, -8f);
            mapGo.GetComponent<RawImage>().texture = _texture;

            var northGo = new GameObject("North", typeof(RectTransform), typeof(Text));
            northGo.transform.SetParent(panel.transform, false);
            var northRt = northGo.GetComponent<RectTransform>();
            northRt.anchorMin = northRt.anchorMax = new Vector2(0.5f, 1f);
            northRt.pivot = new Vector2(0.5f, 1f);
            northRt.anchoredPosition = new Vector2(0f, -5f);
            northRt.sizeDelta = new Vector2(40f, 28f);
            var north = northGo.GetComponent<Text>();
            north.text = "N";
            north.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            north.fontSize = 18;
            north.fontStyle = FontStyle.Bold;
            north.alignment = TextAnchor.MiddleCenter;
            north.color = Color.white;

            var arrowGo = new GameObject("PlayerArrow", typeof(RectTransform), typeof(Text));
            arrowGo.transform.SetParent(panel.transform, false);
            _arrow = arrowGo.GetComponent<RectTransform>();
            _arrow.anchorMin = _arrow.anchorMax = new Vector2(0.5f, 0.5f);
            _arrow.pivot = new Vector2(0.5f, 0.5f);
            _arrow.sizeDelta = new Vector2(42f, 42f);
            var arrowText = arrowGo.GetComponent<Text>();
            arrowText.text = "▲";
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            arrowText.fontSize = 30;
            arrowText.fontStyle = FontStyle.Bold;
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = new Color(0.15f, 0.9f, 1f, 1f);
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player == null) return;
            }

            if (_mapCamera != null)
            {
                Vector3 p = _player.transform.position;
                _mapCamera.transform.position = new Vector3(p.x, p.y + 72f, p.z);
                _mapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            if (_arrow != null)
                _arrow.localEulerAngles = new Vector3(0f, 0f, -_player.transform.eulerAngles.y);
        }

        private void OnDestroy()
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
