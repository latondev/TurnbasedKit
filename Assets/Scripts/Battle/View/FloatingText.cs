using System.Collections;
using TMPro;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Floating Text - displays damage numbers.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        public void SetText(string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void Start()
        {
            StartCoroutine(FloatUp());
        }

        private IEnumerator FloatUp()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.up * 1f;
            float elapsed = 0f;
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
