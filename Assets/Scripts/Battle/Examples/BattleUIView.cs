using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameSystems.AutoBattle;
using GameSystems.Stats.Demo;

namespace GameSystems.Battle.Demo
{
    /// <summary>
    /// UI View cho BattleDemo scene.
    /// Tự tạo toàn bộ UI bằng code — không cần setup tay trong Inspector.
    /// Gắn component này vào 1 GameObject trong scene là đủ.
    /// </summary>
    public class BattleUIView : MonoBehaviour
    {
        // ────────────── Serialised (optional override) ──────────────
        [Header("Optional Refs (auto-created if null)")]
        [SerializeField] private Canvas _canvas;

        // ────────────── Runtime ──────────────
        private List<UnitHPBar> _playerBars = new List<UnitHPBar>();
        private List<UnitHPBar> _enemyBars  = new List<UnitHPBar>();

        private TMP_Text _logText;
        private ScrollRect _logScroll;
        private string _logBuffer = "";

        private GameObject _resultBanner;
        private TMP_Text   _resultText;

        // ────────────── Init ──────────────

        private void Awake()
        {
            if (_canvas == null)
            {
                _canvas = FindFirstObjectByType<Canvas>();
                if (_canvas == null)
                {
                    var canvasGo = new GameObject("Canvas");
                    _canvas = canvasGo.AddComponent<Canvas>();
                    _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasGo.AddComponent<GraphicRaycaster>();
                }
            }

            BuildUI();
        }

        // ────────────────────────────────── API ──────────────────────────────────

        public void InitUI(List<BattleUnit> players, List<BattleUnit> enemies)
        {
            _logBuffer = "";
            if (_logText != null) _logText.text = "";
            _resultBanner?.SetActive(false);

            BuildHPBars(players, _playerBars);
            BuildHPBars(enemies, _enemyBars);
            RefreshHPBars(players, enemies);
        }

        public void RefreshHPBars(List<BattleUnit> players, List<BattleUnit> enemies)
        {
            RefreshTeamBars(players, _playerBars);
            RefreshTeamBars(enemies, _enemyBars);
        }

        public void Log(string message)
        {
            _logBuffer += message + "\n";
            if (_logText != null)
            {
                _logText.text = _logBuffer;
                // Scroll to bottom
                Canvas.ForceUpdateCanvases();
                if (_logScroll != null)
                    _logScroll.verticalNormalizedPosition = 0f;
            }
        }

        public void ShowResult(BattleOutcome outcome)
        {
            if (_resultBanner == null) return;
            _resultBanner.SetActive(true);
            if (_resultText != null)
            {
                _resultText.text = outcome switch
                {
                    BattleOutcome.Victory => "🏆  VICTORY!",
                    BattleOutcome.Defeat  => "💀  DEFEAT...",
                    _                     => "🤝  DRAW"
                };
                _resultText.color = outcome switch
                {
                    BattleOutcome.Victory => new Color(0.4f, 1f, 0.6f),
                    BattleOutcome.Defeat  => new Color(1f, 0.35f, 0.35f),
                    _                     => new Color(1f, 0.85f, 0.25f)
                };
            }
        }

        // ────────────────────────────── HP Bars ──────────────────────────────────

        private class UnitHPBar
        {
            public string unitId;
            public Slider hpSlider;
            public TMP_Text nameLabel;
            public TMP_Text hpLabel;
            public Image fillImage;
        }

        private GameObject _playerBarPanel;
        private GameObject _enemyBarPanel;

        private void BuildHPBars(List<BattleUnit> units, List<UnitHPBar> barList)
        {
            barList.Clear();

            bool isPlayer  = barList == _playerBars;
            var  panel     = isPlayer ? _playerBarPanel : _enemyBarPanel;

            if (panel == null) return;

            // Xoá bars cũ
            foreach (Transform child in panel.transform)
                Destroy(child.gameObject);

            foreach (var unit in units)
            {
                var bar = CreateHPBarRow(panel.transform, unit.UnitName,
                    isPlayer ? new Color(0.3f, 0.7f, 1f) : new Color(1f, 0.3f, 0.3f));
                bar.unitId = unit.UnitId;
                barList.Add(bar);
            }
        }

        private UnitHPBar CreateHPBarRow(Transform parent, string unitName, Color fillColor)
        {
            // Row container
            var row = new GameObject("HPBar_" + unitName, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 36;

            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6;
            hLayout.childForceExpandHeight = true;
            hLayout.childControlHeight = true;
            hLayout.padding = new RectOffset(4, 4, 2, 2);

            // Name label
            var nameLabelGo = MakeText(row.transform, unitName, 11, TextAlignmentOptions.MidlineLeft);
            nameLabelGo.GetComponent<LayoutElement>().preferredWidth = 70;

            // Slider background
            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(row.transform, false);
            sliderGo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var slider = sliderGo.AddComponent<Slider>();

            // Background image
            var bg = new GameObject("BG", typeof(RectTransform));
            bg.transform.SetParent(sliderGo.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill area
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero; fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.sizeDelta = new Vector2(-10, 0);
            fillAreaRT.anchoredPosition = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fill.GetComponent<RectTransform>().anchorMax = Vector2.one;
            fill.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = bgImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            slider.interactable = false;

            // HP text label
            var hpLabelGo = MakeText(row.transform, "HP", 10, TextAlignmentOptions.MidlineRight);
            hpLabelGo.GetComponent<LayoutElement>().preferredWidth = 65;

            return new UnitHPBar
            {
                unitId    = "",
                hpSlider  = slider,
                nameLabel = nameLabelGo.GetComponentInChildren<TMP_Text>(),
                hpLabel   = hpLabelGo.GetComponentInChildren<TMP_Text>(),
                fillImage = fillImg
            };
        }

        private void RefreshTeamBars(List<BattleUnit> units, List<UnitHPBar> bars)
        {
            for (int i = 0; i < units.Count && i < bars.Count; i++)
            {
                var unit = units[i];
                var bar  = bars[i];
                float pct = unit.MaxHP > 0 ? (float)unit.CurrentHP / unit.MaxHP : 0f;
                if (bar.hpSlider != null) bar.hpSlider.value = pct;
                if (bar.hpLabel  != null)
                    bar.hpLabel.text = unit.IsAlive ? $"{unit.CurrentHP}/{unit.MaxHP}" : "DEAD";
                if (bar.fillImage != null)
                {
                    if (!unit.IsAlive)
                        bar.fillImage.color = new Color(0.4f, 0.4f, 0.4f);
                    else if (pct < 0.25f)
                        bar.fillImage.color = new Color(1f, 0.3f, 0.3f);
                    else if (pct < 0.5f)
                        bar.fillImage.color = new Color(1f, 0.75f, 0.2f);
                }
            }
        }

        // ──────────────────────────── Build Full UI ──────────────────────────────

        private void BuildUI()
        {
            var rt = _canvas.GetComponent<RectTransform>();

            // ── Background Panel ──
            var mainPanel = MakePanel(rt, "BattleUI_Main",
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
                new Color(0.08f, 0.08f, 0.12f, 0.92f));
            var vertLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 8;
            vertLayout.padding = new RectOffset(12, 12, 10, 10);
            vertLayout.childForceExpandWidth = true;
            vertLayout.childControlWidth     = true;
            vertLayout.childForceExpandHeight = false;
            vertLayout.childControlHeight     = false;

            // ── Title ──
            var titleGo = MakeText(mainPanel.transform, "⚔️  TURN-BASED BATTLE DEMO", 18, TextAlignmentOptions.Center);
            titleGo.GetComponent<LayoutElement>().preferredHeight = 36;
            var titleText = titleGo.GetComponentInChildren<TMP_Text>();
            titleText.color = new Color(1f, 0.85f, 0.3f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 16;
            titleText.fontSizeMax = 18;

            // ── Teams Row ──
            var teamsRow = MakeHorizontalPanel(mainPanel.transform, "TeamsRow", 50);

            // Player Panel
            var playerPanel = MakeVerticalPanel(teamsRow.transform, "PlayerPanel");
            var playerLabel = MakeText(playerPanel.transform, "🔵  PLAYER TEAM", 12, TextAlignmentOptions.Center);
            var playerLabelText = playerLabel.GetComponentInChildren<TMP_Text>();
            playerLabelText.color = new Color(0.4f, 0.8f, 1f);
            playerLabelText.fontStyle = FontStyles.Bold;
            playerLabel.GetComponent<LayoutElement>().preferredHeight = 22;
            _playerBarPanel = MakeVerticalPanel(playerPanel.transform, "PlayerBars");

            // VS Label
            var vsGo = MakeText(teamsRow.transform, "⚡", 20, TextAlignmentOptions.Center);
            vsGo.GetComponent<LayoutElement>().preferredWidth = 30;
            var vsText = vsGo.GetComponentInChildren<TMP_Text>();
            vsText.color = new Color(1f, 1f, 0.4f);
            vsText.fontStyle = FontStyles.Bold;

            // Enemy Panel
            var enemyPanel = MakeVerticalPanel(teamsRow.transform, "EnemyPanel");
            var enemyLabel = MakeText(enemyPanel.transform, "🔴  ENEMY TEAM", 12, TextAlignmentOptions.Center);
            var enemyLabelText = enemyLabel.GetComponentInChildren<TMP_Text>();
            enemyLabelText.color = new Color(1f, 0.5f, 0.5f);
            enemyLabelText.fontStyle = FontStyles.Bold;
            enemyLabel.GetComponent<LayoutElement>().preferredHeight = 22;
            _enemyBarPanel = MakeVerticalPanel(enemyPanel.transform, "EnemyBars");

            // ── Separator ──
            var sep = new GameObject("Sep", typeof(RectTransform));
            sep.transform.SetParent(mainPanel.transform, false);
            sep.AddComponent<LayoutElement>().preferredHeight = 2;
            sep.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);

            // ── Battle Log ──
            var logLabel = MakeText(mainPanel.transform, "📋  BATTLE LOG", 11, TextAlignmentOptions.Left);
            logLabel.GetComponent<LayoutElement>().preferredHeight = 18;
            var logLabelText = logLabel.GetComponentInChildren<TMP_Text>();
            logLabelText.color = new Color(0.8f, 0.8f, 0.8f);
            logLabelText.fontStyle = FontStyles.Bold;

            var scrollObj = new GameObject("BattleLogScroll", typeof(RectTransform));
            scrollObj.transform.SetParent(mainPanel.transform, false);
            scrollObj.AddComponent<LayoutElement>().preferredHeight = 200;
            scrollObj.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

            var mask = scrollObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            _logScroll = scrollObj.AddComponent<ScrollRect>();
            _logScroll.horizontal = false;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(scrollObj.transform, false);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var logGo = new GameObject("LogText", typeof(RectTransform));
            logGo.transform.SetParent(content.transform, false);
            var logRT = logGo.GetComponent<RectTransform>();
            logRT.anchorMin = new Vector2(0, 1);
            logRT.anchorMax = new Vector2(1, 1);
            logRT.pivot     = new Vector2(0.5f, 1f);
            logRT.anchoredPosition = Vector2.zero;
            logRT.sizeDelta = Vector2.zero;

            _logText = logGo.AddComponent<TextMeshProUGUI>();
            _logText.fontSize    = 11;
            _logText.color       = new Color(0.9f, 0.9f, 0.9f);
            _logText.richText    = true;
            _logText.alignment   = TextAlignmentOptions.TopLeft;
            _logText.margin      = new Vector4(6, 4, 6, 4);
            _logText.enableWordWrapping = true;

            var logCSF = logGo.AddComponent<ContentSizeFitter>();
            logCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _logScroll.content  = contentRT;
            _logScroll.viewport = scrollObj.GetComponent<RectTransform>();

            // ── Buttons ──
            var btnRow = MakeHorizontalPanel(mainPanel.transform, "ButtonRow", 44);
            btnRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;

            MakeButton(btnRow.transform, "▶ Start Battle", new Color(0.2f, 0.6f, 0.2f), () =>
            {
                FindFirstObjectByType<BattleSceneSetup>()?.StartBattle();
            });

            MakeButton(btnRow.transform, "🔄 Reset", new Color(0.2f, 0.4f, 0.7f), () =>
            {
                FindFirstObjectByType<BattleSceneSetup>()?.ResetBattle();
            });

            MakeButton(btnRow.transform, "⏩ 2x Speed", new Color(0.6f, 0.4f, 0.1f), () =>
            {
                FindFirstObjectByType<BattleSceneSetup>()?.ToggleSpeed();
            });

            // ── Stats Battle Controls ──
            var statsSectionLabel = MakeText(mainPanel.transform, "⚙ STATS DEMO", 11, TextAlignmentOptions.Center);
            statsSectionLabel.GetComponent<LayoutElement>().preferredHeight = 20;
            var statsSectionText = statsSectionLabel.GetComponentInChildren<TMP_Text>();
            statsSectionText.color = new Color(0.7f, 0.95f, 0.9f);
            statsSectionText.fontStyle = FontStyles.Bold;

            var statsBtnRow = MakeHorizontalPanel(mainPanel.transform, "StatsButtonRow", 44);
            statsBtnRow.GetComponent<HorizontalLayoutGroup>().spacing = 10;

            MakeButton(statsBtnRow.transform, "▶ Start", new Color(0.18f, 0.55f, 0.45f), () =>
            {
                FindFirstObjectByType<StatsSystemMiniBattleDemo>()?.StartBattle();
            });

            MakeButton(statsBtnRow.transform, "🔄 Reset", new Color(0.18f, 0.42f, 0.65f), () =>
            {
                FindFirstObjectByType<StatsSystemMiniBattleDemo>()?.ResetBattle();
            });

            MakeButton(statsBtnRow.transform, "⏩ Speed", new Color(0.60f, 0.44f, 0.16f), () =>
            {
                FindFirstObjectByType<StatsSystemMiniBattleDemo>()?.ToggleSpeed();
            });

            // ── Result Banner ──
            _resultBanner = MakePanel(rt, "ResultBanner",
                new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.65f), Vector2.zero, Vector2.zero,
                new Color(0, 0, 0, 0.88f));
            _resultBanner.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            var outline = _resultBanner.AddComponent<Outline>();
            outline.effectColor    = new Color(1f, 0.85f, 0.3f, 0.8f);
            outline.effectDistance = new Vector2(2, 2);

            var resultGo = new GameObject("ResultText", typeof(RectTransform));
            resultGo.transform.SetParent(_resultBanner.transform, false);
            var resultRT = resultGo.GetComponent<RectTransform>();
            resultRT.anchorMin = Vector2.zero; resultRT.anchorMax = Vector2.one;
            resultRT.sizeDelta = Vector2.zero;
            _resultText = resultGo.AddComponent<TextMeshProUGUI>();
            _resultText.fontSize  = 32;
            _resultText.alignment = TextAlignmentOptions.Center;
            _resultText.richText  = true;

            _resultBanner.SetActive(false);
        }

        // ────────────────────────────── UI Helpers ────────────────────────────────

        private GameObject MakePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt  = go.GetComponent<RectTransform>();
            rt.anchorMin  = anchorMin;  rt.anchorMax  = anchorMax;
            rt.offsetMin  = offsetMin;  rt.offsetMax  = offsetMax;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private GameObject MakeVerticalPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 3;
            layout.childForceExpandWidth  = true;
            layout.childControlWidth      = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight     = true;
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            return go;
        }

        private GameObject MakeHorizontalPanel(Transform parent, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childForceExpandWidth  = false;
            layout.childControlWidth      = true;
            layout.childForceExpandHeight = true;
            layout.childControlHeight     = true;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return go;
        }

        private GameObject MakeText(Transform parent, string content, int fontSize, TextAlignmentOptions alignment)
        {
            var go  = new GameObject("Text_" + content.Substring(0, Mathf.Min(8, content.Length)),
                typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>();

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = content;
            tmp.fontSize  = fontSize;
            tmp.alignment = alignment;
            tmp.color     = Color.white;
            tmp.richText  = true;
            tmp.enableWordWrapping = false;
            return go;
        }

        private void MakeButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;

            var img = go.AddComponent<Image>();
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.25f);
            colors.selectedColor = Color.Lerp(color, Color.white, 0.08f);
            colors.disabledColor = Color.Lerp(color, Color.black, 0.4f);
            btn.colors = colors;
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(onClick);

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
            shadow.effectDistance = new Vector2(1f, -1f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Label
            var txtGo = new GameObject("Label", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.sizeDelta = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 12;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 10;
            tmp.fontSizeMax = 12;
        }
    }
}
