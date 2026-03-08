using System.Collections;
using UnityEngine;

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
        [SerializeField] private string downAnimation = "down";
        [SerializeField] private string upAnimation = "up";
        [SerializeField] private string idleAnimation = "idle";

        [Header("UI")]
        [SerializeField] private FloatingText floatingTextPrefab;
        [SerializeField] private AnimationController animationHandle;
        [SerializeField] private UnityEngine.UI.Image valueHealthBar;
        [SerializeField] private UnityEngine.UI.Image valueMpBar;
        [SerializeField] private Transform canvasBar;

        [Header("Stats")]
        [SerializeField] private float maxHealth;
        [SerializeField] private float currentHealth;
        [SerializeField] private float maxMp;
        [SerializeField] private float currentMp;

        private void OnValidate()
        {
            TryGetComponent(out animationHandle);

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
            this.maxHealth = maxHP;
            currentHealth = maxHealth;
            this.maxMp = Mp;
            currentMp = 0;

            if (valueHealthBar != null)
                valueHealthBar.fillAmount = 1;
            if (valueMpBar != null)
                valueMpBar.fillAmount = 0;
        }

        void Start()
        {
            if (animationHandle != null)
            {
                animationHandle.OnEndAnimation += EndAnimation;
            }
        }

        private bool isCheck = false;

        protected void EndAnimation(string trackentry)
        {
            if (trackentry == downAnimation)
            {
                if (animationHandle != null)
                    animationHandle.PlayAnimation(upAnimation, 0f, 2, false);
            }

            if (trackentry == upAnimation)
            {
                if (animationHandle != null)
                    animationHandle.PlayAnimation(idleAnimation, 0.1f, 1, true);
            }
        }

        public void ChangeMana(float value)
        {
            currentMp += value;
            float amount = maxMp > 0 ? currentMp / maxMp : 0;
            if (valueMpBar != null)
            {
                StartCoroutine(AnimateFill(valueMpBar, amount));
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

            currentHealth -= hitValue;
            float amount = maxHealth > 0 ? currentHealth / maxHealth : 0;
            if (valueHealthBar != null)
            {
                StartCoroutine(AnimateFill(valueHealthBar, amount));
            }

            if (animationHandle != null)
            {
                string currentAnim = animationHandle.GetCurrentAnimationName(1);
                string currentAnim2 = animationHandle.GetCurrentAnimationName(2);

                if (isHitEffect)
                {
                    if (currentAnim != downAnimation && currentAnim2 != upAnimation)
                    {
                        animationHandle.PlayAnimation(downAnimation, 0.1f, 1, false);
                    }
                }
                else
                {
                    animationHandle.PlayAnimation(behitAnimation, 0.1f, 1, false);
                }
            }
        }

        public void Die()
        {
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
    }

    /// <summary>
    /// Floating Text - displays damage numbers
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Text text;

        public void SetText(string value)
        {
            if (text != null)
                text.text = value;
        }

        void Start()
        {
            // Animate floating up
            StartCoroutine(FloatUp());
        }

        private IEnumerator FloatUp()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * 1f;
            float elapsed = 0;
            float duration = 1f;

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(start, end, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject, 0.5f);
        }
    }
}
