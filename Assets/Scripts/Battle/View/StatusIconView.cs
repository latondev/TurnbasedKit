using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystems.Battle
{
    /// <summary>
    /// Single status icon row entry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusIconView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text counterText;

        private StatusEffectType _type;

        public StatusEffectType Type => _type;

        public void Bind(
            StatusEffectType type,
            string label,
            int remainingTurns,
            int stackCount,
            Sprite iconSprite,
            Color tint)
        {
            _type = type;
            EnsureRuntimeUi();

            if (background != null)
            {
                background.color = new Color(tint.r, tint.g, tint.b, 0.25f);
            }

            if (iconImage != null)
            {
                iconImage.sprite = iconSprite;
                iconImage.enabled = iconSprite != null;
                iconImage.color = tint;
            }

            if (labelText != null)
            {
                labelText.text = iconSprite == null ? label : string.Empty;
                labelText.color = Color.white;
                labelText.gameObject.SetActive(iconSprite == null);
            }

            if (counterText != null)
            {
                string counter = string.Empty;
                if (remainingTurns > 0)
                {
                    counter = remainingTurns.ToString();
                }

                if (stackCount > 1)
                {
                    counter = string.IsNullOrEmpty(counter) ? $"x{stackCount}" : $"{counter}x{stackCount}";
                }

                counterText.text = counter;
                counterText.gameObject.SetActive(!string.IsNullOrEmpty(counter));
            }
        }

        public void EnsureRuntimeUi()
        {
            if (background != null && iconImage != null && labelText != null && counterText != null)
            {
                return;
            }

            var root = transform as RectTransform;
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
            }

            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0f, 0.5f);
            root.sizeDelta = new Vector2(34f, 34f);

            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = 34f;
            layoutElement.preferredHeight = 34f;
            layoutElement.minWidth = 34f;
            layoutElement.minHeight = 34f;

            if (background == null)
            {
                var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(transform, false);
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                background = bgGo.GetComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.45f);
                background.raycastTarget = false;
            }

            if (iconImage == null)
            {
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(3f, 3f);
                iconRt.offsetMax = new Vector2(-3f, -3f);
                iconImage = iconGo.GetComponent<Image>();
                iconImage.raycastTarget = false;
                iconImage.enabled = false;
            }

            if (labelText == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(transform, false);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(1f, 1f);
                labelRt.offsetMax = new Vector2(-1f, -1f);
                labelText = labelGo.AddComponent<TextMeshProUGUI>();
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.fontSize = 10f;
                labelText.color = Color.white;
                labelText.raycastTarget = false;
                labelText.enableWordWrapping = false;
            }

            if (counterText == null)
            {
                var counterGo = new GameObject("Counter", typeof(RectTransform));
                counterGo.transform.SetParent(transform, false);
                var counterRt = counterGo.GetComponent<RectTransform>();
                counterRt.anchorMin = new Vector2(1f, 0f);
                counterRt.anchorMax = new Vector2(1f, 0f);
                counterRt.pivot = new Vector2(1f, 0f);
                counterRt.anchoredPosition = new Vector2(-1f, 1f);
                counterText = counterGo.AddComponent<TextMeshProUGUI>();
                counterText.alignment = TextAlignmentOptions.BottomRight;
                counterText.fontSize = 8f;
                counterText.color = Color.white;
                counterText.raycastTarget = false;
                counterText.enableWordWrapping = false;
            }
        }
    }
}
