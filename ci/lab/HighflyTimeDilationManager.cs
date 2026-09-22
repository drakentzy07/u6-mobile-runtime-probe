using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyTimeDilationManager : MonoBehaviour
    {
        private struct Request
        {
            public float EndAt;
            public float Scale;
        }

        private static HighflyTimeDilationManager _instance;
        private readonly List<Request> _requests = new List<Request>(12);
        private bool _ownsScale;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Ensure();
        }

        private static HighflyTimeDilationManager Ensure()
        {
            if (_instance != null)
                return _instance;

            var existing = Object.FindFirstObjectByType<HighflyTimeDilationManager>();
            if (existing != null)
            {
                _instance = existing;
                return _instance;
            }

            var go = new GameObject("HIGHFLY_TIME_DILATION_MANAGER");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HighflyTimeDilationManager>();
            return _instance;
        }

        public static void RequestHitStop(float duration, float scale = 0.06f)
        {
            if (!HighflySkillLabMode.IsActive)
                return;

            HighflyTimeDilationManager mgr = Ensure();
            mgr._requests.Add(new Request
            {
                EndAt = Time.unscaledTime + Mathf.Max(0.01f, duration),
                Scale = Mathf.Clamp(scale, 0.02f, 1f)
            });
        }

        public static void ForceReset()
        {
            HighflyTimeDilationManager mgr = Ensure();
            mgr._requests.Clear();
            mgr.ReleaseScale();
        }

        private void Update()
        {
            if (!HighflySkillLabMode.IsActive)
            {
                if (_ownsScale)
                    ReleaseScale();
                return;
            }

            float now = Time.unscaledTime;
            for (int i = _requests.Count - 1; i >= 0; i--)
                if (_requests[i].EndAt <= now)
                    _requests.RemoveAt(i);

            if (_requests.Count == 0)
            {
                if (_ownsScale)
                    ReleaseScale();
                return;
            }

            float target = 1f;
            for (int i = 0; i < _requests.Count; i++)
                target = Mathf.Min(target, _requests[i].Scale);

            Time.timeScale = target;
            _ownsScale = true;
        }

        private void ReleaseScale()
        {
            Time.timeScale = 1f;
            _ownsScale = false;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;

            if (_ownsScale)
                Time.timeScale = 1f;
        }
    }
}
