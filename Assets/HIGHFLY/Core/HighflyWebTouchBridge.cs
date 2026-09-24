using System.Runtime.InteropServices;
using UnityEngine;

namespace Highfly.Clean
{
    public static class HighflyWebTouchBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void HF_TouchBridgeInit();
        [DllImport("__Internal")] private static extern int HF_TouchCount();
        [DllImport("__Internal")] private static extern int HF_TouchIdAt(int index);
        [DllImport("__Internal")] private static extern int HF_TouchX100kAt(int index);
        [DllImport("__Internal")] private static extern int HF_TouchY100kAt(int index);
#endif

        private static bool _initialized;

        public static bool IsRuntimeWebGL
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                HF_TouchBridgeInit();
                Debug.Log("[RUN0C] Direct browser touch bridge ready.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[RUN0C] Touch bridge init failed: " + e.Message);
            }
#endif
        }

        public static int Count
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                try { return Mathf.Clamp(HF_TouchCount(), 0, 10); }
                catch { return 0; }
#else
                return 0;
#endif
            }
        }

        public static bool TryGet(int index, out int id, out Vector2 normalized)
        {
            id = -1;
            normalized = Vector2.zero;
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                id = HF_TouchIdAt(index);
                if (id < 0) return false;

                normalized = new Vector2(
                    Mathf.Clamp01(HF_TouchX100kAt(index) / 100000f),
                    Mathf.Clamp01(HF_TouchY100kAt(index) / 100000f));

                return true;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }
    }
}
