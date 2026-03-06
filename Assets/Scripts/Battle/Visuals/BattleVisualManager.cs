using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GameSystems.AutoBattle.Visuals
{
    public class BattleVisualManager : MonoBehaviour
    {
        public AutoBattleController battleController;
        public List<BattleUnitView> unitViews = new List<BattleUnitView>();

        [Header("Settings")]
        public float moveDuration = 0.3f;
        public float attackPause = 0.15f;
        public float projectileSpeed = 12f;

        private void Start()
        {
            if (battleController != null)
            {
                battleController.OnActionExecuted += HandleActionExecuted;
                battleController.OnBattleStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (battleController != null)
            {
                battleController.OnActionExecuted -= HandleActionExecuted;
                battleController.OnBattleStateChanged -= HandleStateChanged;
            }
        }

        public void RegisterUnitView(BattleUnitView view)
        {
            if (!unitViews.Contains(view))
                unitViews.Add(view);
        }

        private void HandleStateChanged(BattleState state)
        {
            if (state == BattleState.Idle || state == BattleState.Ended)
            {
                foreach(var view in unitViews)
                {
                    view.transform.position = view.StartPosition;
                    view.transform.rotation = Quaternion.identity;
                    view.UpdateHP();
                }
            }
        }

        private void HandleActionExecuted(BattleAction action)
        {
            battleController.IsWaitingForVisuals = true;
            StartCoroutine(AnimateActionSequence(action));
        }

        private IEnumerator AnimateActionSequence(BattleAction action)
        {
            BattleUnitView actorView = unitViews.FirstOrDefault(v => v.LinkedUnit == action.actor);
            BattleUnitView targetView = unitViews.FirstOrDefault(v => v.LinkedUnit == action.target);

            if (actorView == null || targetView == null)
            {
                battleController.IsWaitingForVisuals = false;
                yield break;
            }

            bool isAttackOrSkill = action.type == ActionType.Attack || action.type == ActionType.Skill;
            
            if (isAttackOrSkill)
            {
                bool isMelee = action.actor.Range == AttackRange.Melee;
                bool isSkill = action.type == ActionType.Skill;

                if (isMelee)
                {
                    // === MELEE: move to target, hit, return ===
                    Vector3 attackPos = targetView.transform.position + 
                        (actorView.StartPosition - targetView.transform.position).normalized * 1.25f;
                    yield return StartCoroutine(actorView.MoveToTarget(attackPos, moveDuration));
                    
                    if (isSkill)
                        yield return StartCoroutine(actorView.PlaySkillFlash());
                    
                    yield return StartCoroutine(targetView.PopDamage());
                    yield return new WaitForSeconds(attackPause);
                    yield return StartCoroutine(actorView.MoveToTarget(actorView.StartPosition, moveDuration));
                }
                else
                {
                    // === RANGED: stay in place, fire projectile ===
                    if (isSkill)
                        yield return StartCoroutine(actorView.PlaySkillFlash());
                    
                    yield return StartCoroutine(FireProjectile(actorView, targetView, isSkill));
                    yield return StartCoroutine(targetView.PopDamage());
                    yield return new WaitForSeconds(attackPause);
                }
            }
            else
            {
                // Heal or Defend
                actorView.UpdateHP();
                if (targetView != actorView) targetView.UpdateHP();
                yield return new WaitForSeconds(attackPause);
            }

            battleController.IsWaitingForVisuals = false;
        }

        private IEnumerator FireProjectile(BattleUnitView from, BattleUnitView to, bool isSkill)
        {
            // Create a small sphere as projectile
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Projectile";
            projectile.transform.localScale = isSkill ? Vector3.one * 0.4f : Vector3.one * 0.2f;
            projectile.transform.position = from.transform.position + Vector3.up * 0.5f;
            
            // Remove collider so it doesn't interfere
            var col = projectile.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Color: normal = white, skill = orange/yellow
            var rend = projectile.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = isSkill ? new Color(1f, 0.6f, 0.1f) : Color.white;
            }

            Vector3 targetPos = to.transform.position + Vector3.up * 0.5f;
            float dist = Vector3.Distance(projectile.transform.position, targetPos);
            float duration = dist / projectileSpeed;
            float elapsed = 0f;
            Vector3 startPos = projectile.transform.position;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                // Add arc for skill projectiles
                float arc = isSkill ? Mathf.Sin(t * Mathf.PI) * 1.5f : 0f;
                projectile.transform.position = Vector3.Lerp(startPos, targetPos, t) + Vector3.up * arc;
                elapsed += Time.deltaTime;
                yield return null;
            }

            Object.Destroy(projectile);
        }
    }
}
