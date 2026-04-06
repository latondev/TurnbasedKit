using System;
using System.Collections.Generic;
using GameSystems.AutoBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameSystems.Battle
{
    /// <summary>
    /// Runtime status overlay for a single battle unit.
    /// Icons are grouped by unit socket lanes so the overlay follows the model.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatusView : MonoBehaviour
    {
        [SerializeField] private UnitSocketResolver socketResolver;
        [SerializeField] private List<StatusVisualDefinition> visualDefinitions = new List<StatusVisualDefinition>();
        [SerializeField] private float laneWidth = 128f;
        [SerializeField] private float laneHeight = 30f;
        [SerializeField] private float iconSpacing = 2f;
        [SerializeField] private int canvasSortingOrder = 1000;

        private readonly Dictionary<UnitSocketPoint, Transform> _laneRoots = new Dictionary<UnitSocketPoint, Transform>();
        private readonly Dictionary<StatusEffectType, StatusVisualDefinition> _definitionCache = new Dictionary<StatusEffectType, StatusVisualDefinition>();

        private BattleUnit _boundUnit;
        private bool _eventsBound;

        public void Bind(BattleUnit unit, UnitSocketResolver resolver = null)
        {
            if (_boundUnit == unit && (resolver == null || resolver == socketResolver))
            {
                RefreshView();
                return;
            }

            Unbind();

            _boundUnit = unit;
            socketResolver = resolver != null ? resolver : ResolveSocketResolver();
            if (socketResolver != null)
            {
                socketResolver.RefreshCache();
            }

            BindEvents();
            RefreshView();
        }

        public void Unbind()
        {
            if (_boundUnit != null && _eventsBound)
            {
                _boundUnit.OnStatusApplied -= HandleStatusChanged;
                _boundUnit.OnStatusRemoved -= HandleStatusChanged;
                _boundUnit.OnTurnStarted -= HandleTurnChanged;
                _boundUnit.OnTurnEnded -= HandleTurnChanged;
                _boundUnit.OnReset -= HandleUnitReset;
                _boundUnit.OnDefeated -= HandleUnitReset;
            }

            _eventsBound = false;
            _boundUnit = null;
            ClearAll();
            foreach (var laneRoot in _laneRoots.Values)
            {
                if (laneRoot == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(laneRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(laneRoot.gameObject);
                }
            }

            _laneRoots.Clear();
        }

        public void ClearAll()
        {
            foreach (var laneRoot in _laneRoots.Values)
            {
                if (laneRoot == null)
                {
                    continue;
                }

                for (int i = laneRoot.childCount - 1; i >= 0; i--)
                {
                    var child = laneRoot.GetChild(i);
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }

                laneRoot.gameObject.SetActive(false);
            }
        }

        public void RefreshView()
        {
            EnsureSocketResolver();
            socketResolver?.RefreshCache();
            EnsureLaneRoots();

            var statuses = _boundUnit?.RuntimeService?.StatusController?.GetActiveStatuses();
            if (statuses == null || statuses.Count == 0)
            {
                ClearAll();
                return;
            }

            ClearAll();

            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status == null)
                {
                    continue;
                }

                AddIcon(status);
            }
        }

        private void BindEvents()
        {
            if (_boundUnit == null || _eventsBound)
            {
                return;
            }

            _boundUnit.OnStatusApplied += HandleStatusChanged;
            _boundUnit.OnStatusRemoved += HandleStatusChanged;
            _boundUnit.OnTurnStarted += HandleTurnChanged;
            _boundUnit.OnTurnEnded += HandleTurnChanged;
            _boundUnit.OnReset += HandleUnitReset;
            _boundUnit.OnDefeated += HandleUnitReset;
            _eventsBound = true;
        }

        private void HandleStatusChanged(BattleUnit unit, StatusEffect status)
        {
            if (unit == _boundUnit)
            {
                RefreshView();
            }
        }

        private void HandleTurnChanged(BattleUnit unit)
        {
            if (unit == _boundUnit)
            {
                RefreshView();
            }
        }

        private void HandleUnitReset(BattleUnit unit)
        {
            if (unit == _boundUnit)
            {
                RefreshView();
            }
        }

        private void EnsureSocketResolver()
        {
            if (socketResolver != null)
            {
                return;
            }

            socketResolver = ResolveSocketResolver();
        }

        private UnitSocketResolver ResolveSocketResolver()
        {
            var existing = GetComponent<UnitSocketResolver>();
            if (existing != null)
            {
                return existing;
            }

            existing = GetComponentInChildren<UnitSocketResolver>(true);
            if (existing != null)
            {
                return existing;
            }

            return gameObject.AddComponent<UnitSocketResolver>();
        }

        private void EnsureLaneRoots()
        {
            EnsureLaneRoot(UnitSocketPoint.BuffTop);
            EnsureLaneRoot(UnitSocketPoint.BuffMiddle);
            EnsureLaneRoot(UnitSocketPoint.BuffBottom);
        }

        private Transform EnsureLaneRoot(UnitSocketPoint point)
        {
            if (_laneRoots.TryGetValue(point, out var laneRoot) && laneRoot != null)
            {
                return laneRoot;
            }

            var socket = socketResolver != null ? socketResolver.GetSocket(point) : transform;
            if (socket == null)
            {
                socket = transform;
            }

            var go = new GameObject($"{point}_StatusLane", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(socket, false);
            go.transform.localScale = Vector3.one;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(laneWidth, laneHeight);
            rt.anchoredPosition = Vector2.zero;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = canvasSortingOrder;

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = iconSpacing;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            _laneRoots[point] = go.transform;
            SyncLaneScale(point);
            return go.transform;
        }

        private void LateUpdate()
        {
            if (_laneRoots.Count == 0 || socketResolver == null)
            {
                return;
            }

            SyncLaneScale(UnitSocketPoint.BuffTop);
            SyncLaneScale(UnitSocketPoint.BuffMiddle);
            SyncLaneScale(UnitSocketPoint.BuffBottom);
        }

        private void SyncLaneScale(UnitSocketPoint point)
        {
            if (!_laneRoots.TryGetValue(point, out var laneRoot) || laneRoot == null)
            {
                return;
            }

            var socket = socketResolver.GetSocket(point);
            if (socket == null)
            {
                return;
            }

            float desiredScaleX = socket.lossyScale.x < 0f ? -1f : 1f;
            var localScale = laneRoot.localScale;
            if (Mathf.Approximately(localScale.x, desiredScaleX))
            {
                return;
            }

            laneRoot.localScale = new Vector3(desiredScaleX, 1f, 1f);
        }

        private void AddIcon(StatusEffect status)
        {
            var lane = ResolveLane(status.Type);
            var laneRoot = EnsureLaneRoot(lane);
            if (laneRoot == null)
            {
                return;
            }

            laneRoot.gameObject.SetActive(true);

            var iconGo = new GameObject($"{status.Type}_Icon", typeof(RectTransform), typeof(StatusIconView));
            iconGo.transform.SetParent(laneRoot, false);

            var iconView = iconGo.GetComponent<StatusIconView>();
            if (iconView == null)
            {
                return;
            }

            var definition = GetDefinition(status.Type);
            iconView.Bind(
                status.Type,
                definition.DisplayLabel,
                status.RemainingTurns,
                status.StackCount,
                definition.Icon,
                definition.Tint);
        }

        private UnitSocketPoint ResolveLane(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.AttackBuff => UnitSocketPoint.BuffTop,
                StatusEffectType.DefenseBuff => UnitSocketPoint.BuffTop,
                StatusEffectType.SpeedBuff => UnitSocketPoint.BuffTop,
                StatusEffectType.Shield => UnitSocketPoint.BuffTop,
                StatusEffectType.Regeneration => UnitSocketPoint.BuffTop,
                StatusEffectType.Stun => UnitSocketPoint.BuffMiddle,
                StatusEffectType.Freeze => UnitSocketPoint.BuffMiddle,
                StatusEffectType.Silence => UnitSocketPoint.BuffMiddle,
                StatusEffectType.Weakness => UnitSocketPoint.BuffMiddle,
                StatusEffectType.Burn => UnitSocketPoint.BuffBottom,
                StatusEffectType.Poison => UnitSocketPoint.BuffBottom,
                StatusEffectType.Slow => UnitSocketPoint.BuffBottom,
                _ => UnitSocketPoint.BuffMiddle,
            };
        }

        private StatusVisualDefinition GetDefinition(StatusEffectType type)
        {
            if (_definitionCache.TryGetValue(type, out var definition) && definition != null)
            {
                return definition;
            }

            definition = visualDefinitions.Find(entry => entry != null && entry.Type == type);
            if (definition == null)
            {
                definition = CreateFallbackDefinition(type);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(definition.DisplayLabel))
                {
                    definition.DisplayLabel = GetShortLabel(type);
                }
            }

            _definitionCache[type] = definition;
            return definition;
        }

        private static StatusVisualDefinition CreateFallbackDefinition(StatusEffectType type)
        {
            return new StatusVisualDefinition
            {
                Type = type,
                DisplayLabel = GetShortLabel(type),
                Tint = GetTint(type),
            };
        }

        private static string GetShortLabel(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Stun => "STN",
                StatusEffectType.Freeze => "FRZ",
                StatusEffectType.Silence => "SIL",
                StatusEffectType.Poison => "PSN",
                StatusEffectType.Burn => "BRN",
                StatusEffectType.Regeneration => "REG",
                StatusEffectType.AttackBuff => "ATK+",
                StatusEffectType.DefenseBuff => "DEF+",
                StatusEffectType.SpeedBuff => "SPD+",
                StatusEffectType.Weakness => "ATK-",
                StatusEffectType.Slow => "SPD-",
                StatusEffectType.Shield => "SHD",
                _ => type.ToString().ToUpperInvariant(),
            };
        }

        private static Color GetTint(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.Stun => new Color(1f, 0.42f, 0.42f, 1f),
                StatusEffectType.Freeze => new Color(0.42f, 0.85f, 1f, 1f),
                StatusEffectType.Silence => new Color(0.82f, 0.55f, 1f, 1f),
                StatusEffectType.Poison => new Color(0.49f, 1f, 0.48f, 1f),
                StatusEffectType.Burn => new Color(1f, 0.61f, 0.24f, 1f),
                StatusEffectType.Regeneration => new Color(0.53f, 1f, 0.69f, 1f),
                StatusEffectType.AttackBuff => new Color(0.49f, 0.81f, 1f, 1f),
                StatusEffectType.DefenseBuff => new Color(0.62f, 0.82f, 1f, 1f),
                StatusEffectType.SpeedBuff => new Color(1f, 0.84f, 0.45f, 1f),
                StatusEffectType.Weakness => new Color(1f, 0.62f, 0.70f, 1f),
                StatusEffectType.Slow => new Color(0.72f, 0.72f, 0.72f, 1f),
                StatusEffectType.Shield => new Color(0.65f, 0.93f, 1f, 1f),
                _ => Color.white,
            };
        }

        private void OnDestroy()
        {
            Unbind();
        }

        [Serializable]
        public sealed class StatusVisualDefinition
        {
            public StatusEffectType Type;
            public string DisplayLabel;
            public Sprite Icon;
            public Color Tint = Color.white;
        }
    }
}
