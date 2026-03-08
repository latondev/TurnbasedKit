using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using PathologicalGames;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MythydRpg
{
    public class BehitBehavior : MonoBehaviour
    {
        [SpineAnimation, SerializeField] string _behitAnimation;

        [SpineAnimation, SerializeField] string _dieAnimation;
        [SpineAnimation, SerializeField] string _downAnimation;
        [SpineAnimation, SerializeField] string _upAnimation;
        [SpineAnimation, SerializeField] string _idleAnimation;


        [SerializeField] private FloatingText _floatingTextPrefab;

        [SerializeField] AnimationController _animationHandle;

        [SerializeField] private Image _valueHealthBar;
        [SerializeField] private Image _valueMpBar;

        [SerializeField] float maxHealth;
        [SerializeField] float currentHealth;

        [SerializeField] float maxMp;
        [SerializeField] float currentMp;

        [SerializeField]  Transform _canvasBar;

        private void OnValidate()
        {
            _animationHandle = GetComponent<AnimationController>();
            _valueHealthBar = transform.Find("Canvas/battle_HeadBar/healthPoint/value").GetComponent<Image>();
            _valueMpBar = transform.Find("Canvas/battle_HeadBar/angerPoint/value").GetComponent<Image>();
            _canvasBar = transform.Find("Canvas");
            _valueMpBar.fillAmount = 0;
        }

        public void Init(float maxHealth, float Mp)
        {
            this.maxHealth = maxHealth;
            currentHealth = maxHealth;
            _valueHealthBar.fillAmount = 1;
            this.maxMp = Mp;
            currentMp = 0;
            _valueMpBar.fillAmount = 0;
        }

        void Start()
        {
            _animationHandle.OnEndAnimation += EndAnimation;
        }

        private bool isCheck = false;

        protected void EndAnimation(string trackentry)
        {
            if (trackentry == _downAnimation)
            {
                _animationHandle.PlayAnimation(_upAnimation, 0f, 2, false);
            }

            if (trackentry == _upAnimation)
            {
                _animationHandle.PlayAnimation(_idleAnimation, 0.1f, 1, true);

            }
        }

        public void ChangeMana(float value)
        {
            currentMp += value;
            float amout = currentMp / maxMp;
            _valueMpBar.DOFillAmount(amout, 0.1f);
        }

        public void Behit(float hitValue, bool isHitEffect = false)
        {
            FloatingText floatingText = PoolManager.Pools[PoolName.poolFx].Spawn(_floatingTextPrefab.gameObject,
                transform.position + new Vector3(0, 0.5f), Quaternion.identity).GetComponent<FloatingText>();
            floatingText.SetText("-" + hitValue.ToString());
            currentHealth -= hitValue;
            float amout = currentHealth / maxHealth;
            _valueHealthBar.DOFillAmount(amout, 0.1f);

            string aniRunning = _animationHandle.GetCurrentAnimationName(1);
            string aniRunning2 = _animationHandle.GetCurrentAnimationName(2);

            if (isHitEffect)
            {
                if (aniRunning != _downAnimation && aniRunning2 != _upAnimation)
                {
                    _animationHandle.PlayAnimation(_downAnimation, 0.1f, 1, false);
                }

            }
            else
            {
                _animationHandle.PlayAnimation(_behitAnimation, 0.1f, 1, false);
            }
        }

        public void Die()
        {
            _canvasBar.gameObject.Hide();
            _animationHandle.PlayAnimation(_dieAnimation, 0.1f, 2, false, true);
            this.Wait(0.8f, () => { gameObject.Hide(); });
        }

        public void HideUI()
        {
            _canvasBar.Hide();
        }

        public void ShowUI()
        {
            _canvasBar.Show();
        }
    }
}