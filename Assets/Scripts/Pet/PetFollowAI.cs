using UnityEngine;

namespace GameSystems.Pet
{
    public class PetFollowAI : MonoBehaviour
    {
        [Header("Follow Settings")]
        public Transform target; // player transform
        public float followDistance = 2f;
        public float moveSpeed = 4f;
        public float rotateSpeed = 720f;
        public float stopDistance = 1.5f;

        [Header("Animation (optional)")]
        public Animator animator;
        private static readonly int MoveParam = Animator.StringToHash("Move");

        private Vector3 targetPosition;
        private bool isMoving;

        void Update()
        {
            if (target == null) return;

            float dist = Vector3.Distance(transform.position, target.position);

            if (dist > followDistance)
            {
                // Follow target
                targetPosition = target.position - (target.position - transform.position).normalized * stopDistance;
                Vector3 dir = (targetPosition - transform.position);
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
                    isMoving = true;
                }
                else
                {
                    isMoving = false;
                }
            }
            else
            {
                isMoving = false;
            }

            // Animation
            if (animator != null)
                animator.SetBool(MoveParam, isMoving);
        }

        // Optional: pickup loot helper
        public void PickupItem(GameObject item)
        {
            // TODO: animate grab, add to inventory
            Destroy(item);
        }
    }
}