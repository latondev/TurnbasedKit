using UnityEngine;

namespace GameSystems.AutoBattle.Visuals
{
    public class UIFollowTarget : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;
        private RectTransform rectTransform;
        private Camera mainCam;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            mainCam = Camera.main;
        }

        private void LateUpdate()
        {
            if (target != null && mainCam != null)
            {
                Vector3 screenPos = mainCam.WorldToScreenPoint(target.position + offset);
                if (screenPos.z < 0)
                {
                    rectTransform.anchoredPosition = new Vector2(-10000, -10000); // Hide behind camera
                }
                else
                {
                    rectTransform.position = screenPos;
                }
            }
        }
    }
}
