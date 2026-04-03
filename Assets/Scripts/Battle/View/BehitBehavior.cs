using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystems.Battle
{
    /// <summary>
    /// Behit Behavior - handles getting hit, damage display, death
    /// </summary>
    public class BehitBehavior : MonoBehaviour
    {
        [Header("Animations")]
        [SerializeField] private string behitAnimation = "hit";
        [SerializeField] private string dieAnimation = "die";

        [Header("UI")]
        [SerializeField] private FloatingText floatingTextPrefab;
        [SerializeField] private AnimationHandle animationHandle;
        [SerializeField] private UnityEngine.UI.Image valueHealthBar;
        [SerializeField] private UnityEngine.UI.Image valueMpBar;
        [SerializeField] private Transform canvasBar;

        [Header("Stats")]
        [SerializeField] private float maxHealth;
        [SerializeField] private float currentHealth;
        [SerializeField] private float maxMp;
        [SerializeField] private float currentMp;

        private Coroutine healthFillRoutine;
        private Coroutine manaFillRoutine;

        private void OnValidate()
        {
            TryGetComponent(out animationHandle);
            if (animationHandle == null)
            {
                animationHandle = GetComponentInChildren<AnimationHandle>(true);
            }

            // Try to find UI elements
            var canvas = transform.Find("Canvas");
            if (canvas != null)
            {
                canvasBar = canvas;
                var healthBar = canvas.Find("battle_HeadBar/healthPoint/value");
                var mpBar = canvas.Find("battle_HeadBar/angerPoint/value");

                if (healthBar != null)
                    valueHealthBar = healthBar.GetComponent<UnityEngine.UI.Image>();
                if (mpBar != null)
                    valueMpBar = mpBar.GetComponent<UnityEngine.UI.Image>();
            }
        }

        public void Init(float maxHP, float Mp)
        {
            StopFillCoroutines();
            CancelInvoke(nameof(HideGameObject));

            this.maxHealth = Mathf.Max(0f, maxHP);
            currentHealth = this.maxHealth;
            this.maxMp = Mathf.Max(0f, Mp);
            currentMp = 0;

            EnsureRuntimeBars();

            if (valueHealthBar != null)
                valueHealthBar.fillAmount = this.maxHealth > 0f ? 1f : 0f;
            if (valueMpBar != null)
                valueMpBar.fillAmount = 0;
        }

        void Start()
        {
            if (animationHandle == null)
            {
                animationHandle = GetComponentInChildren<AnimationHandle>(true);
            }

            if (animationHandle != null)
            {
                animationHandle.Initialize();
            }
        }

        public void ChangeMana(float value)
        {
            currentMp = Mathf.Clamp(currentMp + value, 0f, maxMp);
            float amount = maxMp > 0f ? currentMp / maxMp : 0f;
            if (valueMpBar != null)
            {
                if (manaFillRoutine != null)
                {
                    StopCoroutine(manaFillRoutine);
                }

                manaFillRoutine = StartCoroutine(AnimateFill(valueMpBar, amount));
            }
        }

        private IEnumerator AnimateFill(UnityEngine.UI.Image img, float target)
        {
            float start = img.fillAmount;
            float elapsed = 0;
            float duration = 0.1f;

            while (elapsed < duration)
            {
                img.fillAmount = Mathf.Lerp(start, target, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            img.fillAmount = target;
        }

        public void Behit(float hitValue, bool isHitEffect = false)
        {
            // Spawn floating text
            if (floatingTextPrefab != null)
            {
                var ft = Instantiate(floatingTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                ft.SetText("-" + hitValue.ToString("F0"));
            }

            currentHealth = Mathf.Clamp(currentHealth - hitValue, 0f, maxHealth);
            float amount = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            if (valueHealthBar != null)
            {
                if (healthFillRoutine != null)
                {
                    StopCoroutine(healthFillRoutine);
                }

                healthFillRoutine = StartCoroutine(AnimateFill(valueHealthBar, amount));
            }

            if (animationHandle != null)
            {
                animationHandle.PlayAnimation(behitAnimation, 0.1f, 1, false);
            }
        }

        public void Die()
        {
            StopFillCoroutines();
            CancelInvoke(nameof(HideGameObject));

            if (canvasBar != null)
                canvasBar.gameObject.SetActive(false);

            if (animationHandle != null && !string.IsNullOrEmpty(dieAnimation))
            {
                animationHandle.PlayAnimation(dieAnimation, 0.1f, 2, false, true);
            }

            // Hide after animation
            Invoke(nameof(HideGameObject), 0.8f);
        }

        private void HideGameObject()
        {
            CancelInvoke(nameof(HideGameObject));
            gameObject.SetActive(false);
        }

        public void HideUI()
        {
            if (canvasBar != null)
                canvasBar.gameObject.SetActive(false);
        }

        public void ShowUI()
        {
            if (canvasBar != null)
                canvasBar.gameObject.SetActive(true);
        }

        private void EnsureRuntimeBars()
        {
            if (valueHealthBar != null && valueMpBar != null && canvasBar != null)
            {
                return;
            }

            if (canvasBar == null)
            {
                var existingCanvas = transform.Find("Canvas");
                if (existingCanvas != null)
                {
                    canvasBar = existingCanvas;
                }
                else
                {
                    var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvasGo.transform.SetParent(transform, false);
                    canvasBar = canvasGo.transform;

                    var canvas = canvasGo.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.WorldSpace;

                    var rect = canvasGo.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(220f, 60f);
                    rect.anchoredPosition = new Vector2(0f, 1.65f);
                }
            }

            var headBar = canvasBar.Find("battle_HeadBar");
            if (headBar == null)
            {
                var headBarGo = new GameObject("battle_HeadBar", typeof(RectTransform));
                headBarGo.transform.SetParent(canvasBar, false);
                var headRect = headBarGo.GetComponent<RectTransform>();
                headRect.anchorMin = Vector2.zero;
                headRect.anchorMax = Vector2.one;
                headRect.offsetMin = Vector2.zero;
                headRect.offsetMax = Vector2.zero;
                var layout = headBarGo.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 2f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                headBar = headBarGo.transform;
            }

            valueHealthBar = EnsureBarFill(headBar, "healthPoint", new Color(0.35f, 0.95f, 0.4f));
            valueMpBar = EnsureBarFill(headBar, "angerPoint", new Color(0.35f, 0.65f, 1f));
        }

        private Image EnsureBarFill(Transform parent, string rowName, Color fillColor)
        {
            var row = parent.Find(rowName);
            if (row == null)
            {
                var rowGo = new GameObject(rowName, typeof(RectTransform));
                rowGo.transform.SetParent(parent, false);
                var rowRect = rowGo.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(180f, 14f);

                var background = new GameObject("background", typeof(RectTransform));
                background.transform.SetParent(rowGo.transform, false);
                var backgroundRect = background.GetComponent<RectTransform>();
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;

                var backgroundImage = background.AddComponent<Image>();
                backgroundImage.color = new Color(0f, 0f, 0f, 0.65f);

                var value = new GameObject("value", typeof(RectTransform));
                value.transform.SetParent(background.transform, false);
                var valueRect = value.GetComponent<RectTransform>();
                valueRect.anchorMin = Vector2.zero;
                valueRect.anchorMax = Vector2.one;
                valueRect.offsetMin = new Vector2(2f, 2f);
                valueRect.offsetMax = new Vector2(-2f, -2f);

                var valueImage = value.AddComponent<Image>();
                valueImage.color = fillColor;
                valueImage.type = Image.Type.Filled;
                valueImage.fillMethod = Image.FillMethod.Horizontal;
                valueImage.fillOrigin = 0;
                valueImage.fillAmount = 1f;

                return valueImage;
            }

            var valueTransform = row.Find("background/value");
            if (valueTransform != null)
            {
                var valueImage = valueTransform.GetComponent<Image>();
                if (valueImage != null)
                {
                    valueImage.type = Image.Type.Filled;
                    valueImage.fillMethod = Image.FillMethod.Horizontal;
                    valueImage.fillOrigin = 0;
                    return valueImage;
                }
            }

            return row.GetComponentInChildren<Image>(true);
        }

        private void OnDestroy()
        {
            StopFillCoroutines();
            CancelInvoke(nameof(HideGameObject));
        }

        private void StopFillCoroutines()
        {
            if (healthFillRoutine != null)
            {
                StopCoroutine(healthFillRoutine);
                healthFillRoutine = null;
            }

            if (manaFillRoutine != null)
            {
                StopCoroutine(manaFillRoutine);
                manaFillRoutine = null;
            }
        }
    }
}
