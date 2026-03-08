using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MythydRpg
{
    public class HeroLineup : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        Action<IndexSlot> OnSwapHero;
        private RectTransform rectTransform;
        private Vector3 originalPosition;

        [SerializeField] SkeletonGraphic _skeletonGraphic;
        private Vector2 dragOffset;
        public void OnBeginDrag(PointerEventData eventData)
        {
            _skeletonGraphic.raycastTarget = false;
            //  canvasGroup.blocksRaycasts = false; 

            originalPosition = rectTransform.position;


        }

        public void OnDrag(PointerEventData eventData)
        {

            Vector2 localPoint;


            // Gán tọa độ local cho RectTransform
            transform.position = Input.mousePosition + new Vector3(0, 1, 0);


        }
        public void SetCallback(Action<IndexSlot> callback)
        {
            if (OnSwapHero == null)
            {
                OnSwapHero = null;
            }

            OnSwapHero = callback;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            GameObject hitObject = eventData.pointerCurrentRaycast.gameObject;

            if (hitObject != null)
            {
                Debug.Log(" anme = " + hitObject.gameObject.name);
            }

            if (hitObject != null && hitObject.CompareTag(Tags.T_SwapSlot) && hitObject != gameObject)
            {
                Debug.Log("l1");
                // Thực hiện hoán đổi
                SwapWith(hitObject);
            }
            else
            {
                Debug.Log("l2");

                // Trả về vị trí ban đầu
                rectTransform.position = originalPosition;
            }
            _skeletonGraphic.raycastTarget = true;
        }
        private void SwapWith(GameObject target)
        {
            HeroLineup targetHero = target.GetComponent<HeroLineup>();
            IndexSlot slot = target.transform.parent.GetComponent<IndexSlot>();
            if (slot != null)
            {
                OnSwapHero?.Invoke(slot);
            }
            else
            {
                IndexSlot slotBase = target.GetComponent<IndexSlot>();
                OnSwapHero?.Invoke(slotBase);


            }

            //if (targetHero != null)
            //{
            //    Debug.Log("l1");
            //    Transform targetParent = target.transform.parent;
            //    Vector3 targetPosition = Vector3.zero;

            //    // Đổi vị trí
            //    target.transform.position = originalPosition;
            //    target.transform.SetParent(transform.parent);
            //    target.GetComponent<RectTransform>().ResetAnchoredPosition();


            //    rectTransform.GetComponent<RectTransform>().ResetAnchoredPosition();
            //    transform.SetParent(targetParent);
            //    transform.GetComponent<RectTransform>().ResetAnchoredPosition();
            //}
            //else
            //{
            //    Debug.Log("l2");

            //    transform.SetParent(target.transform);
            //    rectTransform.GetComponent<RectTransform>().ResetAnchoredPosition();

            //}    



        }
        public void ResetPosition()
        {
            rectTransform.ResetAnchoredPosition();
            rectTransform.localScale = Vector3.one;
        }
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

        }

        void Start()
        {
            _skeletonGraphic = GetComponent<SkeletonGraphic>();
            originalPosition = Vector3.zero;

        }



        public void Init()
        {
        }
    }
}
