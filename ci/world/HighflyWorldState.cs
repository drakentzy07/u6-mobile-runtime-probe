using UnityEngine;

namespace Highfly.World
{
    [DisallowMultipleComponent]
    public sealed class HighflyWorldState : MonoBehaviour
    {
        public static HighflyWorldState Instance { get; private set; }

        private const string Prefix = "HF_WORLD_V1_";
        private float _noticeUntil;
        private string _notice = "";
        private GUIStyle _noticeStyle;
        private GUIStyle _inventoryStyle;

        public int Wood => PlayerPrefs.GetInt(Prefix + "wood", 0);
        public int Ore => PlayerPrefs.GetInt(Prefix + "ore", 0);
        public int Herb => PlayerPrefs.GetInt(Prefix + "herb", 0);
        public int Ingot => PlayerPrefs.GetInt(Prefix + "ingot", 0);
        public int Gold => PlayerPrefs.GetInt(Prefix + "gold", 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;
            var go = new GameObject("HIGHFLY_WORLD_STATE");
            DontDestroyOnLoad(go);
            go.AddComponent<HighflyWorldState>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public int Get(HighflyResourceType type)
        {
            return PlayerPrefs.GetInt(Prefix + Key(type), 0);
        }

        public void Add(HighflyResourceType type, int amount)
        {
            if (amount == 0) return;
            string key = Prefix + Key(type);
            int next = Mathf.Max(0, PlayerPrefs.GetInt(key, 0) + amount);
            PlayerPrefs.SetInt(key, next);
            PlayerPrefs.Save();
        }

        public bool Spend(HighflyResourceType type, int amount)
        {
            if (amount <= 0) return true;
            int current = Get(type);
            if (current < amount) return false;
            Add(type, -amount);
            return true;
        }

        public void MarkDiscovered(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            PlayerPrefs.SetInt(Prefix + "discovered_" + Sanitize(id), 1);
            PlayerPrefs.Save();
        }

        public bool IsDiscovered(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return PlayerPrefs.GetInt(Prefix + "discovered_" + Sanitize(id), 0) != 0;
        }

        public long GetTimestamp(string id)
        {
            string raw = PlayerPrefs.GetString(Prefix + "ts_" + Sanitize(id), "0");
            return long.TryParse(raw, out long value) ? value : 0L;
        }

        public void SetTimestamp(string id, long unixSeconds)
        {
            PlayerPrefs.SetString(Prefix + "ts_" + Sanitize(id), unixSeconds.ToString());
            PlayerPrefs.Save();
        }

        public void Notice(string message, float seconds = 2.4f)
        {
            _notice = message ?? "";
            _noticeUntil = Time.unscaledTime + Mathf.Max(0.3f, seconds);
        }

        private static string Key(HighflyResourceType type)
        {
            switch (type)
            {
                case HighflyResourceType.Wood: return "wood";
                case HighflyResourceType.Ore: return "ore";
                case HighflyResourceType.Herb: return "herb";
                case HighflyResourceType.Ingot: return "ingot";
                case HighflyResourceType.Gold: return "gold";
                default: return type.ToString().ToLowerInvariant();
            }
        }

        private static string Sanitize(string value)
        {
            return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("/", "_");
        }

        private void OnGUI()
        {
            _inventoryStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft
            };

            GUI.Box(new Rect(12f, 76f, 300f, 30f),
                $"Madera {Wood}   Mineral {Ore}   Hierba {Herb}   Lingote {Ingot}   Oro {Gold}",
                _inventoryStyle);

            if (Time.unscaledTime >= _noticeUntil || string.IsNullOrEmpty(_notice)) return;

            _noticeStyle ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            float width = Mathf.Min(Screen.width * 0.62f, 760f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height * 0.18f, width, 64f), _notice, _noticeStyle);
        }
    }
}
