﻿using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace CenturionCC.System.UI.HeadUI
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class HeadUINotificationElement : UdonSharpBehaviour
    {
        [SerializeField]
        private Text text;

        [SerializeField]
        private Image panel;

        [SerializeField]
        private Image icon;

        [SerializeField]
        private Slider slider;

        [SerializeField]
        private Image[] alphaChangeTargets;


        private float _alpha;
        private float _alphaTarget;
        private float _alphaVelocity;
        private int _duplicateCount;
        private float _lifeTime;

        private float _maxLifeTime = 10F;
        private float _maxSpeed;

        private HeadUINotification _notification;

        private string _originalMessage;

        private Vector2 _posVelocity;

        private float _smoothTime;

        [NonSerialized]
        public Vector2 positionTarget;

        [NonSerialized]
        public RectTransform rectTransform;

        public int Id { get; private set; }

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            _lifeTime += Time.deltaTime;

            if (_lifeTime > _maxLifeTime)
            {
                DestroyThis();
                return;
            }

            _alphaTarget = _lifeTime > _maxLifeTime - _smoothTime * 10F ? 0F : 1F;
            rectTransform.anchoredPosition = Vector2.SmoothDamp(
                rectTransform.anchoredPosition,
                positionTarget,
                ref _posVelocity,
                _smoothTime,
                _maxSpeed
            );

            SetAlpha(_alpha = Mathf.SmoothDamp(
                _alpha,
                _alphaTarget,
                ref _alphaVelocity,
                _smoothTime,
                _maxSpeed
            ));

            slider.value = Mathf.Clamp01(_lifeTime / _maxLifeTime + _smoothTime * 2);
        }

        private void OnDestroy()
        {
            if (!_notification) return;
            _notification.RemoveNotification(this);
        }

        private void SetAlpha(float alpha)
        {
            var textColor = text.color;
            text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            var panelColor = panel.color;
            panel.color = new Color(panelColor.r, panelColor.g, panelColor.b, alpha);
            var iconColor = icon.color;
            icon.color = new Color(iconColor.r, iconColor.g, iconColor.b, alpha);

            foreach (var target in alphaChangeTargets)
            {
                target.color = new Color(target.color.r, target.color.g, target.color.b, alpha);
            }
        }

        public void DestroyThis()
        {
            Destroy(gameObject);
        }

        public void AddDuplicate(string message, float dur)
        {
            _lifeTime = 0F;
            _maxLifeTime = dur;

            if (_originalMessage == message)
            {
                ++_duplicateCount;
            }
            else
            {
                _originalMessage = message;
                _duplicateCount = 0;
            }

            UpdateMessage();
        }

        public void Setup(HeadUINotification notification, Sprite iconSprite, Color panelColor,
            float dur, float smoothT, float maxSpd, string message, int id)
        {
            Id = id;
            _notification = notification;

            rectTransform = GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, -100);
            positionTarget = new Vector2(0, 0);

            _originalMessage = message;
            _maxLifeTime = dur;

            icon.sprite = iconSprite;
            panel.color = panelColor;
            SetAlpha(0F);
            _alphaTarget = 1F;
            _smoothTime = smoothT;
            _maxSpeed = maxSpd;

            UpdateMessage();
        }

        private void UpdateMessage()
        {
            text.text = _duplicateCount == 0 ? _originalMessage : $"{_originalMessage} x{_duplicateCount}";
        }
    }
}