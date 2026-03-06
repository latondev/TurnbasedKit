using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GameSystems.AutoBattle.Visuals
{
    public class BattleDemoSpawner : MonoBehaviour
    {
        public AutoBattleController controller;
        public BattleVisualManager visualManager;
        public Transform canvasTransform;

        [Header("Spawn Positions")]
        public Vector3 playerStartPos = new Vector3(-5, 0.5f, 0);
        public Vector3 enemyStartPos = new Vector3(5, 0.5f, 0);
        public float spacing = 2f;

        private void Start()
        {
            StartCoroutine(SpawnVisualsRoutine());
        }

        private System.Collections.IEnumerator SpawnVisualsRoutine()
        {
            // wait 1 frame for AutoBattleController to initialize its mock units
            yield return null; 

            SpawnTeam(controller.PlayerUnits, playerStartPos, true);
            SpawnTeam(controller.EnemyUnits, enemyStartPos, false);

            CreateStartButton();
        }

        private void CreateStartButton()
        {
            var btnObj = new GameObject("StartButton");
            btnObj.transform.SetParent(canvasTransform, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 100);
            rect.sizeDelta = new Vector2(200, 60);

            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.2f);

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.text = "START BATTLE";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 24;
            
            var txtRect = txtObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                controller.StartBattle();
                btnObj.SetActive(false);
            });
        }

        private void SpawnTeam(List<BattleUnit> units, Vector3 startPos, bool isPlayerSide)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                
                // Melee = Capsule (close range fighter), Ranged = Cube (archer/mage)
                PrimitiveType shape = unit.Range == AttackRange.Melee 
                    ? PrimitiveType.Capsule 
                    : PrimitiveType.Cube;
                
                GameObject obj = GameObject.CreatePrimitive(shape);
                
                // Position: spread along Z
                Vector3 pos;
                if (isPlayerSide)
                    pos = startPos + new Vector3(0, 0, -i * spacing);
                else
                    pos = startPos + new Vector3(0, 0, i * spacing);
                
                // Ranged units stand slightly further back from the front line
                if (unit.Range == AttackRange.Ranged)
                {
                    pos.x += isPlayerSide ? -1.5f : 1.5f;
                }
                
                obj.transform.position = pos;
                
                // Scale cubes a bit smaller for visual distinction
                if (shape == PrimitiveType.Cube)
                {
                    obj.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
                }
                
                var view = obj.AddComponent<BattleUnitView>();
                visualManager.RegisterUnitView(view);

                // UI Health Bar
                GameObject hpBarObj = new GameObject($"HPBar_{unit.UnitName}");
                hpBarObj.transform.SetParent(canvasTransform, false);
                var hpSlider = hpBarObj.AddComponent<Slider>();
                
                var hpRect = hpBarObj.GetComponent<RectTransform>();
                hpRect.sizeDelta = new Vector2(100, 15);

                var follower = hpBarObj.AddComponent<UIFollowTarget>();
                follower.target = obj.transform;
                follower.offset = new Vector3(0, 1.5f, 0);
                
                // Build visual slider
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(hpBarObj.transform, false);
                var bgImg = bgObj.AddComponent<Image>();
                bgImg.color = new Color(0, 0, 0, 0.5f);
                var bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;

                GameObject fillAreaObj = new GameObject("Fill Area");
                fillAreaObj.transform.SetParent(hpBarObj.transform, false);
                var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.sizeDelta = Vector2.zero;

                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(fillAreaObj.transform, false);
                var fillImg = fillObj.AddComponent<Image>();
                // Player = green, Enemy = red
                fillImg.color = isPlayerSide ? Color.green : Color.red;
                var fillRect = fillObj.GetComponent<RectTransform>();
                fillRect.sizeDelta = Vector2.zero;
                
                hpSlider.targetGraphic = fillImg;
                hpSlider.fillRect = fillRect;
                hpSlider.minValue = 0;
                hpSlider.maxValue = 1;

                view.HPBar = hpSlider;
                view.Setup(unit);
            }
        }
    }
}
