using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MythydRpg
{
    [Serializable]
    public struct SpeedInfo
    {
        public float speed;
        public Sprite sprite;
    }

    public class GamePlayScreen : ViewBase
    {
        public Action<float> OnSpeedChange;
        [SerializeField] Button _btnPause;
        [SerializeField] private Button _btnSpeed;
        [SerializeField] private Image _imageSpeed;

        private float _speed = 1;

        [SerializeField] private CustomQueue<SpeedInfo> speedList;

        void Start()
        {
            _imageSpeed = _btnSpeed.GetComponent<Image>();
            _speed = speedList.First().speed;
            _btnSpeed.onClick.AddListener(OnClickSpeed);
        }

        private void OnClickSpeed()
        {
            SpeedInfo info = speedList.Next();
            _imageSpeed.sprite = info.sprite;
            _speed = info.speed;
            OnSpeedChange?.Invoke(_speed);
        }
    }
}