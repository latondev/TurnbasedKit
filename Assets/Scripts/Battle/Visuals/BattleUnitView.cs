using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace GameSystems.AutoBattle.Visuals
{
    public class BattleUnitView : MonoBehaviour
    {
        public BattleUnit LinkedUnit;
        public Vector3 StartPosition;
        
        [Header("UI")]
        public Slider HPBar;

        private Color originalColor;

        public void Setup(BattleUnit unit)
        {
            LinkedUnit = unit;
            StartPosition = transform.position;
            gameObject.name = unit.UnitName + (unit.Range == AttackRange.Ranged ? " [R]" : " [M]");
            
            var rend = GetComponent<Renderer>();
            if (rend != null) 
            {
                rend.material.color = unit.GetUnitColor();
                originalColor = rend.material.color;
            }
            
            UpdateHP();
        }

        public void UpdateHP()
        {
            if (LinkedUnit == null) return;
            
            if (HPBar != null)
            {
                HPBar.value = LinkedUnit.GetHPPercentage();
            }

            if (!LinkedUnit.IsAlive)
            {
                var rend = GetComponent<Renderer>();
                if (rend != null) rend.material.color = Color.gray;
                transform.rotation = Quaternion.Euler(0, 0, 90f);
            }
        }

        public IEnumerator MoveToTarget(Vector3 targetPos, float duration)
        {
            Vector3 startPos = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Ease-out for snappy feel
                float eased = 1f - (1f - t) * (1f - t);
                transform.position = Vector3.Lerp(startPos, targetPos, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPos;
        }

        public IEnumerator PopDamage()
        {
            // Flash red + scale up on hit
            var rend = GetComponent<Renderer>();
            Vector3 originalScale = transform.localScale;
            
            if (rend != null) rend.material.color = Color.red;
            transform.localScale = originalScale * 1.3f;
            yield return new WaitForSeconds(0.1f);
            
            transform.localScale = originalScale;
            if (rend != null) rend.material.color = LinkedUnit.IsAlive ? originalColor : Color.gray;
            
            UpdateHP();
        }

        public IEnumerator PlaySkillFlash()
        {
            // Flash yellow/orange glow before attacking
            var rend = GetComponent<Renderer>();
            Vector3 originalScale = transform.localScale;
            
            if (rend != null) rend.material.color = new Color(1f, 0.8f, 0.2f); // Golden glow
            transform.localScale = originalScale * 1.2f;
            yield return new WaitForSeconds(0.2f);
            
            transform.localScale = originalScale;
            if (rend != null) rend.material.color = originalColor;
        }
    }
}
