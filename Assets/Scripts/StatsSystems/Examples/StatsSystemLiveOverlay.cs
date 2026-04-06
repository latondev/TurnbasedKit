using System.Collections;
using System.Collections.Generic;
using System.Text;
using GameSystems.Battle.Demo;
using GameSystems.Stats;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StatsSystems.Examples
{
	/// <summary>
	/// Runtime HUD for the stats battle demo.
	/// Builds its own canvas and shows unit cards with HP bars, logs, and battle status.
	/// </summary>
	public class StatsSystemLiveOverlay : MonoBehaviour
	{
		[Header("UI")]
		[SerializeField] private Canvas canvas;
		[SerializeField] private bool autoFindSceneUnits = false;
		[SerializeField] private string title = "Stats Battle Demo";
		[SerializeField, Range(4, 20)] private int maxLogLines = 10;
		[SerializeField] private StatsSystemMiniBattleDemo battleDemo;

		[Header("Style")]
		[SerializeField] private Color backgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.95f);
		[SerializeField] private Color panelColor = new Color(0.10f, 0.12f, 0.17f, 0.92f);
		[SerializeField] private Color cardColor = new Color(0.13f, 0.15f, 0.20f, 0.96f);
		[SerializeField] private Color accentColor = new Color(0.95f, 0.80f, 0.30f, 1f);
		[SerializeField] private Color heroColor = new Color(0.40f, 0.78f, 1f, 1f);
		[SerializeField] private Color enemyColor = new Color(1f, 0.45f, 0.45f, 1f);
		[SerializeField] private Color hpGoodColor = new Color(0.38f, 0.86f, 0.48f, 1f);
		[SerializeField] private Color hpWarnColor = new Color(1f, 0.77f, 0.20f, 1f);
		[SerializeField] private Color hpLowColor = new Color(1f, 0.35f, 0.35f, 1f);
		[SerializeField] private Color mutedTextColor = new Color(0.84f, 0.88f, 0.92f, 1f);

		private readonly List<UnitStatController> trackedUnits = new();
		private readonly List<UnitCard> unitCards = new();
		private readonly Queue<string> logLines = new();

		private TMP_Text statusText;
		private TMP_Text speedText;
		private TMP_Text logText;
		private GameObject emptyStateObject;
		private TMP_Text emptyStateText;
		private ButtonBinding startBinding;
		private ButtonBinding resetBinding;
		private ButtonBinding speedBinding;

		private UnitStatController activeUnit;
		private RectTransform cardsContainer;
		private bool uiBuilt;
		private bool dirty = true;
		private float nextAutoScanTime;

		private void Awake()
		{
			ResolveBattleDemo();
			EnsureCanvas();
			EnsureEventSystem();
			BuildUI();
		}

		private IEnumerator Start()
		{
			yield return null;
			if (battleDemo != null)
			{
				SyncTrackedUnitsFromBattleDemo();
			}
			else if (autoFindSceneUnits)
			{
				RefreshSceneUnits();
			}
		}

		private void LateUpdate()
		{
			ResolveBattleDemo();

			if (battleDemo != null)
			{
				SyncTrackedUnitsFromBattleDemo();
			}
			else if (autoFindSceneUnits && Time.unscaledTime >= nextAutoScanTime)
			{
				RefreshSceneUnits();
				nextAutoScanTime = Time.unscaledTime + 0.5f;
			}

			UpdateUnitCards();
			UpdateControlState();
			RefreshLogText();
		}

		public void Register(UnitStatController unit)
		{
			if (unit == null || trackedUnits.Contains(unit))
			{
				return;
			}

			trackedUnits.Add(unit);
			RebuildUnitCards();
			dirty = true;
		}

		public void SetTrackedUnits(IEnumerable<UnitStatController> units)
		{
			trackedUnits.Clear();

			if (units != null)
			{
				foreach (var unit in units)
				{
					if (unit != null && !trackedUnits.Contains(unit))
					{
						trackedUnits.Add(unit);
					}
				}
			}

			RebuildUnitCards();
			dirty = true;
		}

		public void ClearTrackedUnits()
		{
			trackedUnits.Clear();
			activeUnit = null;
			RebuildUnitCards();
			dirty = true;
		}

		public void SetActiveUnit(UnitStatController unit)
		{
			activeUnit = unit;
			dirty = true;
		}

		public void Log(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			logLines.Enqueue(message);
			while (logLines.Count > maxLogLines)
			{
				logLines.Dequeue();
			}

			dirty = true;
		}

		public void MarkDirty()
		{
			dirty = true;
		}

		public void RefreshSceneUnits()
		{
			var found = FindObjectsByType<UnitStatController>(FindObjectsSortMode.None);
			if (found == null)
			{
				return;
			}

			if (!HasSameTrackedUnits(found))
			{
				SetTrackedUnits(found);
			}
		}

		private void ResolveBattleDemo()
		{
			if (battleDemo == null)
			{
				battleDemo = GetComponent<StatsSystemMiniBattleDemo>();
			}

			if (battleDemo == null)
			{
				battleDemo = FindFirstObjectByType<StatsSystemMiniBattleDemo>();
			}
		}

		private void SyncTrackedUnitsFromBattleDemo(bool force = false)
		{
			if (battleDemo == null)
			{
				return;
			}

			var hero = battleDemo.HeroUnit;
			var enemy = battleDemo.EnemyUnit;

			if (hero == null && enemy == null)
			{
				return;
			}

			var needsUpdate = force
				|| trackedUnits.Count != 2
				|| trackedUnits[0] != hero
				|| trackedUnits[1] != enemy;

			if (needsUpdate)
			{
				SetTrackedUnits(new[] { hero, enemy });
				Log("Battle units synced.");
			}
		}

		private void UpdateUnitCards()
		{
			if (emptyStateObject != null)
			{
				emptyStateObject.SetActive(trackedUnits.Count == 0);
			}

			if (trackedUnits.Count == 0 || unitCards.Count == 0)
			{
				return;
			}

			for (var i = 0; i < unitCards.Count; i++)
			{
				var card = unitCards[i];
				var unit = card.unit;
				if (unit == null)
				{
					continue;
				}

				RefreshUnitCard(card, unit, i);
			}

			AnimateUnitCards();
		}

		private void RefreshUnitCard(UnitCard card, UnitStatController unit, int index)
		{
			if (card == null || unit == null)
			{
				return;
			}

			var isActive = unit == activeUnit;
			var isHero = index == 0;
			var accent = isHero ? heroColor : enemyColor;
			var hp = unit.GetStat(StatType.Health);
			var mana = unit.GetStat(StatType.Mana);
			var stamina = unit.GetStat(StatType.Stamina);

			if (card.nameText != null)
			{
				card.nameText.text = unit.UnitName;
				card.nameText.color = isActive ? accentColor : accent;
			}

			if (card.levelText != null)
			{
				var status = unit.IsDead() ? "<color=#FF6B6B>DEAD</color>" : "<color=#8FE79A>ALIVE</color>";
				card.levelText.text = $"Lv.{unit.Level}  {status}";
				card.levelText.color = mutedTextColor;
			}

			var currentHp = hp != null ? hp.CurrentValue : 0f;
			var maxHp = hp != null ? hp.MaxValue : 0f;
			var hpPct = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

			if (!card.initialized)
			{
				card.visualHpPercent = hpPct;
				card.initialized = true;
			}

			card.targetHpPercent = hpPct;
			card.isActive = isActive;
			card.isDead = unit.IsDead();

			if (card.hpText != null)
			{
				card.hpText.text = card.isDead
					? "DEAD"
					: $"{currentHp:F0}/{maxHp:F0}";
			}

			if (card.resourceText != null)
			{
				var manaCurrent = mana != null ? mana.CurrentValue : 0f;
				var manaMax = mana != null ? mana.MaxValue : 0f;
				var staminaCurrent = stamina != null ? stamina.CurrentValue : 0f;
				var staminaMax = stamina != null ? stamina.MaxValue : 0f;
				card.resourceText.text = $"MP {manaCurrent:F0}/{manaMax:F0}   STA {staminaCurrent:F0}/{staminaMax:F0}";
			}

			if (card.combatText != null)
			{
				var attack = unit.GetStatValue(StatType.Attack);
				var defense = unit.GetStatValue(StatType.Defense);
				var speed = unit.GetStatValue(StatType.Speed);
				card.combatText.text = $"ATK {attack:F1}   DEF {defense:F1}   SPD {speed:F1}";
			}
		}

		private void AnimateUnitCards()
		{
			if (unitCards.Count == 0)
			{
				return;
			}

			var delta = Time.unscaledDeltaTime;
			var pulseTime = Time.unscaledTime * 4.5f;

			foreach (var card in unitCards)
			{
				if (card == null)
				{
					continue;
				}

				var cardTransform = card.root != null ? card.root : null;
				var pulse = card.isActive ? (Mathf.Sin(pulseTime + card.pulseSeed) * 0.5f + 0.5f) : 0f;
				var backgroundTarget = card.isDead
					? new Color(0.10f, 0.10f, 0.12f, 0.96f)
					: card.isActive
						? Color.Lerp(card.baseColor, card.teamColor, 0.22f + pulse * 0.08f)
						: card.baseColor;

				if (card.background != null)
				{
					card.background.color = Color.Lerp(card.background.color, backgroundTarget, delta * 8f);
				}

				if (cardTransform != null)
				{
					var targetScale = card.isActive ? 1.015f + pulse * 0.008f : 1f;
					cardTransform.localScale = Vector3.Lerp(cardTransform.localScale, Vector3.one * targetScale, delta * 8f);
				}

				card.visualHpPercent = Mathf.MoveTowards(card.visualHpPercent, card.targetHpPercent, delta * (card.isDead ? 12f : 6f));

				if (card.hpSlider != null)
				{
					card.hpSlider.value = card.visualHpPercent;
				}

				if (card.hpFill != null)
				{
					card.hpFill.color = card.isDead
						? new Color(0.45f, 0.45f, 0.45f, 1f)
						: card.visualHpPercent > 0.5f ? hpGoodColor : card.visualHpPercent > 0.25f ? hpWarnColor : hpLowColor;
				}
			}
		}

		private void UpdateControlState()
		{
			if (battleDemo == null)
			{
				SetButtonState(startBinding, false, "NO DEMO");
				SetButtonState(resetBinding, false, "RESET");
				SetButtonState(speedBinding, false, "SPEED");
				if (statusText != null)
				{
					statusText.text = "Battle demo not found.";
				}

				if (speedText != null)
				{
					speedText.text = "x0";
				}

				return;
			}

			var running = battleDemo.IsBattleRunning;
			var speed = Mathf.Max(0.1f, battleDemo.BattleSpeed);

			SetButtonState(startBinding, !running, running ? "RUNNING" : "START");
			SetButtonState(resetBinding, true, "RESET");
			SetButtonState(speedBinding, true, $"SPEED x{speed:0.#}");

			if (statusText != null)
			{
				var state = running ? "RUNNING" : battleDemo.IsBattleReady ? "READY" : "SETTING UP";
				statusText.text = $"STATE: {state}   UNITS: {trackedUnits.Count}";
			}

			if (speedText != null)
			{
				speedText.text = $"{speed:0.#}x";
			}
		}

		private void SetButtonState(ButtonBinding binding, bool interactable, string label)
		{
			if (binding == null)
			{
				return;
			}

			if (binding.button != null)
			{
				binding.button.interactable = interactable;
			}

			if (binding.label != null)
			{
				binding.label.text = label;
			}
		}

		private void RefreshLogText()
		{
			if (!dirty)
			{
				return;
			}

			dirty = false;

			if (logText == null)
			{
				return;
			}

			logText.text = BuildLogText();

			Canvas.ForceUpdateCanvases();
			var scrollRect = logText.transform.GetComponentInParent<ScrollRect>();
			if (scrollRect != null)
			{
				scrollRect.verticalNormalizedPosition = 0f;
			}
		}

		private string BuildLogText()
		{
			if (logLines.Count == 0)
			{
				return "Waiting for battle events...";
			}

			var sb = new StringBuilder();
			foreach (var line in logLines)
			{
				sb.AppendLine(line);
			}

			return sb.ToString().TrimEnd();
		}

		private bool HasSameTrackedUnits(IReadOnlyList<UnitStatController> units)
		{
			if (units == null)
			{
				return trackedUnits.Count == 0;
			}

			if (trackedUnits.Count != units.Count)
			{
				return false;
			}

			for (var i = 0; i < units.Count; i++)
			{
				if (trackedUnits[i] != units[i])
				{
					return false;
				}
			}

			return true;
		}

		private void RebuildUnitCards()
		{
			if (cardsContainer != null)
			{
				for (var i = cardsContainer.childCount - 1; i >= 0; i--)
				{
					Destroy(cardsContainer.GetChild(i).gameObject);
				}
			}

			unitCards.Clear();

			if (cardsContainer == null || trackedUnits.Count == 0)
			{
				return;
			}

			for (var i = 0; i < trackedUnits.Count; i++)
			{
				var unit = trackedUnits[i];
				if (unit == null)
				{
					continue;
				}

				var accent = i == 0 ? heroColor : enemyColor;
				unitCards.Add(CreateUnitCard(cardsContainer, unit, accent));
			}
		}

		private void EnsureCanvas()
		{
			if (canvas == null)
			{
				canvas = FindFirstObjectByType<Canvas>();
			}

			if (canvas == null)
			{
				var canvasGo = new GameObject("StatsBattleOverlayCanvas");
				canvas = canvasGo.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				canvas.overrideSorting = true;
				canvas.sortingOrder = 200;
				canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				canvasGo.AddComponent<GraphicRaycaster>();
				return;
			}

			if (canvas.GetComponent<GraphicRaycaster>() == null)
			{
				canvas.gameObject.AddComponent<GraphicRaycaster>();
			}
		}

		private void EnsureEventSystem()
		{
			if (FindFirstObjectByType<EventSystem>() != null)
			{
				return;
			}

			var eventSystemGo = new GameObject("EventSystem");
			eventSystemGo.AddComponent<EventSystem>();
			eventSystemGo.AddComponent<StandaloneInputModule>();
		}

		private void BuildUI()
		{
			if (uiBuilt || canvas == null)
			{
				return;
			}

			var root = canvas.transform as RectTransform;
			if (root == null)
			{
				return;
			}

			var dockedLayout = false;
			var mainPanel = CreatePanel(root, "StatsBattleMainPanel", backgroundColor);
			var mainRect = mainPanel.GetComponent<RectTransform>();
			if (dockedLayout)
			{
				mainRect.anchorMin = new Vector2(0.64f, 0.04f);
				mainRect.anchorMax = new Vector2(0.99f, 0.96f);
				mainRect.offsetMin = new Vector2(-10f, 0f);
				mainRect.offsetMax = new Vector2(-10f, 0f);
			}
			else
			{
				mainRect.anchorMin = Vector2.zero;
				mainRect.anchorMax = Vector2.one;
				mainRect.offsetMin = new Vector2(12f, 12f);
				mainRect.offsetMax = new Vector2(-12f, -12f);
			}

			var mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
			mainLayout.spacing = 10;
			mainLayout.padding = new RectOffset(14, 14, 14, 14);
			mainLayout.childForceExpandWidth = true;
			mainLayout.childControlWidth = true;
			mainLayout.childForceExpandHeight = true;
			mainLayout.childControlHeight = true;

			var headerRow = CreateRow(mainPanel.transform, "HeaderRow", 54f);
			var titleGo = CreateText(headerRow.transform, "Title", title, 20, TextAlignmentOptions.MidlineLeft, accentColor, false);
			titleGo.GetComponent<LayoutElement>().flexibleWidth = 1f;

			var speedGo = CreateText(headerRow.transform, "SpeedLabel", "1x", 14, TextAlignmentOptions.MidlineRight, mutedTextColor, false);
			speedGo.GetComponent<LayoutElement>().preferredWidth = 80f;
			speedText = speedGo.GetComponentInChildren<TMP_Text>();

			var contentRow = CreateRow(mainPanel.transform, "ContentRow", 0f);
			contentRow.GetComponent<LayoutElement>().flexibleHeight = 1f;
			var contentLayout = contentRow.GetComponent<HorizontalLayoutGroup>();
			contentLayout.childForceExpandHeight = true;
			contentLayout.childControlHeight = true;
			contentLayout.childForceExpandWidth = true;
			contentLayout.childControlWidth = true;
			contentLayout.spacing = 12;

			var leftColumn = CreateColumn(contentRow.transform, "LeftColumn", 8f);
			leftColumn.GetComponent<LayoutElement>().flexibleWidth = 1.15f;
			leftColumn.GetComponent<LayoutElement>().flexibleHeight = 1f;

			var rightColumn = CreateColumn(contentRow.transform, "RightColumn", 8f);
			rightColumn.GetComponent<LayoutElement>().flexibleWidth = 0.85f;
			rightColumn.GetComponent<LayoutElement>().flexibleHeight = 1f;

			var summaryPanel = CreatePanel(leftColumn.transform, "SummaryPanel", panelColor);
			summaryPanel.GetComponent<LayoutElement>().preferredHeight = 62f;
			var summaryLayout = summaryPanel.AddComponent<VerticalLayoutGroup>();
			summaryLayout.spacing = 2;
			summaryLayout.padding = new RectOffset(10, 10, 8, 8);
			summaryLayout.childForceExpandWidth = true;
			summaryLayout.childControlWidth = true;
			summaryLayout.childForceExpandHeight = false;
			summaryLayout.childControlHeight = true;

			var summaryTitle = CreateText(summaryPanel.transform, "SummaryTitle", "BATTLE STATUS", 12, TextAlignmentOptions.MidlineLeft, mutedTextColor, false);
			summaryTitle.GetComponent<LayoutElement>().preferredHeight = 16f;
			statusText = CreateText(summaryPanel.transform, "SummaryStatus", "Waiting...", 13, TextAlignmentOptions.MidlineLeft, Color.white, true).GetComponentInChildren<TMP_Text>();

			var cardsPanel = CreatePanel(leftColumn.transform, "CardsPanel", panelColor);
			cardsPanel.GetComponent<LayoutElement>().flexibleHeight = 1f;
			var cardsLayout = cardsPanel.AddComponent<VerticalLayoutGroup>();
			cardsLayout.spacing = 10;
			cardsLayout.padding = new RectOffset(10, 10, 10, 10);
			cardsLayout.childForceExpandWidth = true;
			cardsLayout.childControlWidth = true;
			cardsLayout.childForceExpandHeight = false;
			cardsLayout.childControlHeight = true;

			var cardsHeader = CreateText(cardsPanel.transform, "CardsHeader", "UNITS", 12, TextAlignmentOptions.MidlineLeft, mutedTextColor, false);
			cardsHeader.GetComponent<LayoutElement>().preferredHeight = 16f;

			emptyStateObject = CreateText(cardsPanel.transform, "EmptyState", "Waiting for battle units...", 13, TextAlignmentOptions.Center, mutedTextColor, false);
			emptyStateObject.GetComponent<LayoutElement>().preferredHeight = 42f;
			emptyStateText = emptyStateObject.GetComponentInChildren<TMP_Text>();

			cardsContainer = CreateColumn(cardsPanel.transform, "CardsContainer", 8f).GetComponent<RectTransform>();
			cardsContainer.GetComponent<LayoutElement>().flexibleHeight = 1f;
			var cardsContainerLayout = cardsContainer.GetComponent<VerticalLayoutGroup>();
			cardsContainerLayout.childForceExpandHeight = false;
			cardsContainerLayout.childControlHeight = true;

			var hintPanel = CreatePanel(leftColumn.transform, "HintPanel", panelColor);
			hintPanel.GetComponent<LayoutElement>().preferredHeight = 38f;
			var hintText = CreateText(hintPanel.transform, "HintText", dockedLayout
				? "Use the battle UI buttons on the left to control the fight."
				: "Start the battle, then watch HP bars, buffs, debuffs, and speed changes update live.", 11, TextAlignmentOptions.MidlineLeft, mutedTextColor, true);
			hintText.GetComponent<LayoutElement>().preferredHeight = 24f;

			var logPanel = CreatePanel(rightColumn.transform, "LogPanel", panelColor);
			logPanel.GetComponent<LayoutElement>().flexibleHeight = 1f;
			var logLayout = logPanel.AddComponent<VerticalLayoutGroup>();
			logLayout.spacing = 6;
			logLayout.padding = new RectOffset(10, 10, 10, 10);
			logLayout.childForceExpandWidth = true;
			logLayout.childControlWidth = true;
			logLayout.childForceExpandHeight = false;
			logLayout.childControlHeight = true;

			var logHeader = CreateText(logPanel.transform, "LogHeader", "BATTLE LOG", 12, TextAlignmentOptions.MidlineLeft, mutedTextColor, false);
			logHeader.GetComponent<LayoutElement>().preferredHeight = 16f;

			var scrollObj = new GameObject("LogScroll", typeof(RectTransform));
			scrollObj.transform.SetParent(logPanel.transform, false);
			scrollObj.AddComponent<LayoutElement>().flexibleHeight = 1f;
			scrollObj.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.95f);

			var mask = scrollObj.AddComponent<Mask>();
			mask.showMaskGraphic = true;
			var scrollRect = scrollObj.AddComponent<ScrollRect>();
			scrollRect.horizontal = false;

			var content = new GameObject("Content", typeof(RectTransform));
			content.transform.SetParent(scrollObj.transform, false);
			var contentRT = content.GetComponent<RectTransform>();
			contentRT.anchorMin = new Vector2(0, 1);
			contentRT.anchorMax = new Vector2(1, 1);
			contentRT.pivot = new Vector2(0.5f, 1f);
			contentRT.anchoredPosition = Vector2.zero;

			var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
			contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			var logGo = new GameObject("LogText", typeof(RectTransform));
			logGo.transform.SetParent(content.transform, false);
			var logRT = logGo.GetComponent<RectTransform>();
			logRT.anchorMin = new Vector2(0, 1);
			logRT.anchorMax = new Vector2(1, 1);
			logRT.pivot = new Vector2(0.5f, 1f);
			logRT.anchoredPosition = Vector2.zero;
			logRT.sizeDelta = Vector2.zero;

			logText = logGo.AddComponent<TextMeshProUGUI>();
			logText.fontSize = 11;
			logText.color = mutedTextColor;
			logText.richText = true;
			logText.alignment = TextAlignmentOptions.TopLeft;
			logText.margin = new Vector4(6, 4, 6, 4);
			logText.enableWordWrapping = true;

			var logContentSizer = logGo.AddComponent<ContentSizeFitter>();
			logContentSizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			scrollRect.content = contentRT;
			scrollRect.viewport = scrollObj.GetComponent<RectTransform>();

			uiBuilt = true;
			dirty = true;
		}

		private void OnStartClicked()
		{
			battleDemo?.StartBattle();
			Log("Start button pressed.");
		}

		private void OnResetClicked()
		{
			battleDemo?.ResetBattle();
			Log("Reset button pressed.");
		}

		private void OnSpeedClicked()
		{
			battleDemo?.ToggleSpeed();
			var speed = battleDemo != null ? battleDemo.BattleSpeed : 0f;
			Log($"Speed toggled to {speed:0.#}x.");
		}

		private GameObject CreatePanel(Transform parent, string name, Color color)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			go.AddComponent<LayoutElement>();
			go.AddComponent<Image>().color = color;
			return go;
		}

		private GameObject CreateRow(Transform parent, string name, float height)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			var layout = go.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 8;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = true;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			var element = go.AddComponent<LayoutElement>();
			element.preferredHeight = height;
			return go;
		}

		private GameObject CreateColumn(Transform parent, string name, float spacing)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			var layout = go.AddComponent<VerticalLayoutGroup>();
			layout.spacing = spacing;
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = true;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			go.AddComponent<LayoutElement>().flexibleWidth = 1f;
			return go;
		}

		private GameObject CreateText(Transform parent, string objectName, string content, int fontSize, TextAlignmentOptions alignment, Color color, bool wordWrap)
		{
			var go = new GameObject(objectName, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			go.AddComponent<LayoutElement>();

			var text = go.AddComponent<TextMeshProUGUI>();
			text.text = content;
			text.fontSize = fontSize;
			text.alignment = alignment;
			text.color = color;
			text.richText = true;
			text.enableWordWrapping = wordWrap;

			return go;
		}

		private ButtonBinding CreateButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
		{
			var go = new GameObject($"Btn_{label}", typeof(RectTransform));
			go.transform.SetParent(parent, false);
			var element = go.AddComponent<LayoutElement>();
			element.flexibleWidth = 1f;

			var image = go.AddComponent<Image>();
			image.color = color;

			var shadow = go.AddComponent<Shadow>();
			shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
			shadow.effectDistance = new Vector2(1f, -1f);

			var outline = go.AddComponent<Outline>();
			outline.effectColor = new Color(1f, 1f, 1f, 0.08f);
			outline.effectDistance = new Vector2(1f, -1f);

			var button = go.AddComponent<Button>();
			button.targetGraphic = image;
			var colors = button.colors;
			colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
			colors.pressedColor = Color.Lerp(color, Color.black, 0.22f);
			colors.selectedColor = color;
			button.colors = colors;
			button.onClick.AddListener(onClick);

			var labelGo = CreateText(go.transform, "Label", label, 12, TextAlignmentOptions.Center, Color.white, false);
			var labelText = labelGo.GetComponentInChildren<TMP_Text>();
			labelText.fontStyle = FontStyles.Bold;
			labelText.enableAutoSizing = true;
			labelText.fontSizeMin = 10f;
			labelText.fontSizeMax = 12f;
			labelText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
			labelText.GetComponent<RectTransform>().anchorMax = Vector2.one;
			labelText.GetComponent<RectTransform>().offsetMin = Vector2.zero;
			labelText.GetComponent<RectTransform>().offsetMax = Vector2.zero;
			labelText.GetComponent<LayoutElement>().flexibleWidth = 1f;

			return new ButtonBinding
			{
				button = button,
				label = labelText
			};
		}

		private UnitCard CreateUnitCard(Transform parent, UnitStatController unit, Color accent)
		{
			var card = CreatePanel(parent, $"UnitCard_{unit.UnitName}", cardColor);
			var element = card.GetComponent<LayoutElement>();
			element.preferredHeight = 126f;

			var layout = card.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 4;
			layout.padding = new RectOffset(10, 10, 10, 10);
			layout.childForceExpandWidth = true;
			layout.childForceExpandHeight = false;
			layout.childControlWidth = true;
			layout.childControlHeight = true;

			var topRow = CreateRow(card.transform, "TopRow", 18f);
			var nameTextGo = CreateText(topRow.transform, "NameText", unit.UnitName, 16, TextAlignmentOptions.MidlineLeft, accent, false);
			nameTextGo.GetComponent<LayoutElement>().flexibleWidth = 1f;
			var levelTextGo = CreateText(topRow.transform, "LevelText", "Lv.1", 11, TextAlignmentOptions.MidlineRight, mutedTextColor, false);
			levelTextGo.GetComponent<LayoutElement>().preferredWidth = 120f;

			var hpRow = CreateRow(card.transform, "HpRow", 26f);
			var hpLabelGo = CreateText(hpRow.transform, "HpLabel", "HP", 11, TextAlignmentOptions.MidlineLeft, mutedTextColor, false);
			hpLabelGo.GetComponent<LayoutElement>().preferredWidth = 34f;

			var barGo = new GameObject("HpBar", typeof(RectTransform));
			barGo.transform.SetParent(hpRow.transform, false);
			barGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

			var slider = barGo.AddComponent<Slider>();
			slider.minValue = 0f;
			slider.maxValue = 1f;
			slider.value = 1f;
			slider.interactable = false;

			var bg = new GameObject("Background", typeof(RectTransform));
			bg.transform.SetParent(barGo.transform, false);
			var bgRT = bg.GetComponent<RectTransform>();
			bgRT.anchorMin = Vector2.zero;
			bgRT.anchorMax = Vector2.one;
			bgRT.sizeDelta = Vector2.zero;
			var bgImage = bg.AddComponent<Image>();
			bgImage.color = new Color(0.18f, 0.19f, 0.23f, 1f);

			var fillArea = new GameObject("FillArea", typeof(RectTransform));
			fillArea.transform.SetParent(barGo.transform, false);
			var fillAreaRT = fillArea.GetComponent<RectTransform>();
			fillAreaRT.anchorMin = Vector2.zero;
			fillAreaRT.anchorMax = Vector2.one;
			fillAreaRT.sizeDelta = new Vector2(-8f, 0f);
			fillAreaRT.anchoredPosition = Vector2.zero;

			var fill = new GameObject("Fill", typeof(RectTransform));
			fill.transform.SetParent(fillArea.transform, false);
			var fillRT = fill.GetComponent<RectTransform>();
			fillRT.anchorMin = Vector2.zero;
			fillRT.anchorMax = Vector2.one;
			fillRT.sizeDelta = Vector2.zero;
			var fillImage = fill.AddComponent<Image>();
			fillImage.color = hpGoodColor;

			slider.fillRect = fillRT;
			slider.targetGraphic = bgImage;
			slider.direction = Slider.Direction.LeftToRight;

			var hpTextGo = CreateText(hpRow.transform, "HpText", "100/100", 11, TextAlignmentOptions.MidlineRight, mutedTextColor, false);
			hpTextGo.GetComponent<LayoutElement>().preferredWidth = 80f;

			var resourceTextGo = CreateText(card.transform, "ResourceText", "MP 0/0   STA 0/0", 10, TextAlignmentOptions.MidlineLeft, mutedTextColor, false);
			resourceTextGo.GetComponent<LayoutElement>().preferredHeight = 16f;

			var combatTextGo = CreateText(card.transform, "CombatText", "ATK 0   DEF 0   SPD 0", 10, TextAlignmentOptions.MidlineLeft, mutedTextColor, false);
			combatTextGo.GetComponent<LayoutElement>().preferredHeight = 16f;

			return new UnitCard
			{
				unit = unit,
				root = card.GetComponent<RectTransform>(),
				background = card.GetComponent<Image>(),
				baseColor = cardColor,
				teamColor = accent,
				pulseSeed = Mathf.Abs(unit.GetInstanceID() % 1000) * 0.01f,
				nameText = nameTextGo.GetComponentInChildren<TMP_Text>(),
				levelText = levelTextGo.GetComponentInChildren<TMP_Text>(),
				hpSlider = slider,
				hpFill = fillImage,
				hpText = hpTextGo.GetComponentInChildren<TMP_Text>(),
				resourceText = resourceTextGo.GetComponentInChildren<TMP_Text>(),
				combatText = combatTextGo.GetComponentInChildren<TMP_Text>(),
			};
		}

		private sealed class UnitCard
		{
			public UnitStatController unit;
			public RectTransform root;
			public Image background;
			public Color baseColor;
			public Color teamColor;
			public TMP_Text nameText;
			public TMP_Text levelText;
			public Slider hpSlider;
			public Image hpFill;
			public TMP_Text hpText;
			public TMP_Text resourceText;
			public TMP_Text combatText;
			public float visualHpPercent;
			public float targetHpPercent;
			public float pulseSeed;
			public bool isActive;
			public bool isDead;
			public bool initialized;
		}

		private sealed class ButtonBinding
		{
			public Button button;
			public TMP_Text label;
		}
	}
}
