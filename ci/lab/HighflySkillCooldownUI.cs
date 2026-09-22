using UnityEngine;
using UnityEngine.UI;

namespace Highfly.SkillLab
{
    public static class HighflySkillCooldowns
    {
        public static float GetRemaining(HighflyPremiumSkillId id)
        {
            if ((int)id <= 5)
                return HighflyPremiumSkillRuntime.Instance != null
                    ? HighflyPremiumSkillRuntime.Instance.GetCooldownRemaining(id)
                    : 0f;

            if ((int)id <= 10)
                return HighflyAdvancedSkillRuntime.Instance != null
                    ? HighflyAdvancedSkillRuntime.Instance.GetCooldownRemaining(id)
                    : 0f;

            if ((int)id <= 13)
                return HighflyReferenceSkillRuntime.Instance != null
                    ? GetReferenceRemaining(id)
                    : 0f;

            return HighflyApexPassSkillRuntime.Instance != null
                ? HighflyApexPassSkillRuntime.Instance.GetCooldownRemaining(id)
                : 0f;
        }

        private static float GetReferenceRemaining(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.EclipseRend:
                    return HighflyReferenceSkillRuntime.Instance.RendRemaining;
                case HighflyPremiumSkillId.ReturnWall:
                    return HighflyReferenceSkillRuntime.Instance.ReflectRemaining;
                case HighflyPremiumSkillId.VoraciousEcho:
                    return HighflyReferenceSkillRuntime.Instance.DecoyRemaining;
            }

            return 0f;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflySkillCooldownVisual : MonoBehaviour
    {
        private int _slot;
        private Image _fill;
        private Text _text;
        private Text _name;
        private Font _font;

        public void Configure(int slot, Sprite discSprite)
        {
            _slot = Mathf.Clamp(slot, 0, 4);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var fillGo = new GameObject(
                "CooldownRadial",
                typeof(RectTransform),
                typeof(Image));

            fillGo.transform.SetParent(transform, false);

            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(7f, 7f);
            fillRect.offsetMax = new Vector2(-7f, -7f);

            _fill = fillGo.GetComponent<Image>();
            _fill.sprite = discSprite;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Radial360;
            _fill.fillOrigin = (int)Image.Origin360.Top;
            _fill.fillClockwise = false;
            _fill.fillAmount = 0f;
            _fill.color = new Color(0.01f, 0.015f, 0.025f, 0.76f);
            _fill.raycastTarget = false;

            var textGo = new GameObject(
                "CooldownText",
                typeof(RectTransform),
                typeof(Text));

            textGo.transform.SetParent(transform, false);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _text = textGo.GetComponent<Text>();
            _text.font = _font;
            _text.fontSize = 24;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = Color.white;
            _text.raycastTarget = false;

            var nameGo = new GameObject(
                "SkillCompactName",
                typeof(RectTransform),
                typeof(Text));

            nameGo.transform.SetParent(transform, false);

            RectTransform nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 4f);
            nameRect.sizeDelta = new Vector2(18f, 28f);

            _name = nameGo.GetComponent<Text>();
            _name.font = _font;
            _name.fontSize = 10;
            _name.alignment = TextAnchor.LowerCenter;
            _name.color = new Color(0.82f, 0.92f, 1f, 0.92f);
            _name.raycastTarget = false;
        }

        private void LateUpdate()
        {
            bool active = HighflySkillLabMode.IsActive;

            if (_fill != null)
                _fill.gameObject.SetActive(active);

            if (_text != null)
                _text.gameObject.SetActive(active);

            if (_name != null)
                _name.gameObject.SetActive(active);

            if (!active) return;

            HighflyPremiumSkillId id =
                HighflySkillLoadout.Get(_slot);

            float remaining =
                HighflySkillCooldowns.GetRemaining(id);

            float duration =
                HighflySkillCatalog.GetCooldownDuration(id);

            if (_fill != null)
                _fill.fillAmount =
                    remaining > 0.01f
                        ? Mathf.Clamp01(remaining / duration)
                        : 0f;

            if (_text != null)
            {
                if (remaining > 0.05f)
                {
                    _text.text =
                        remaining >= 10f
                            ? Mathf.CeilToInt(remaining).ToString()
                            : remaining.ToString("0.0");
                }
                else
                {
                    _text.text = "";
                }
            }

            if (_name != null)
            {
                HighflySkillDefinitionLite def =
                    HighflySkillCatalog.Get(id);

                _name.text =
                    def != null
                        ? Compact(def.Name)
                        : "";
            }
        }

        private static string Compact(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            string[] parts = value.Split(' ');
            if (parts.Length == 1) return parts[0];
            return parts[0] + " " + parts[1];
        }
    }
}
