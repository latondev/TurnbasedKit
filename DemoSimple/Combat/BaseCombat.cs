using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MythydRpg
{
    public abstract class BaseCombat : MonoBehaviour
    {
        public List<CharaterTurnbase> _teamAtk;
        public List<CharaterTurnbase> _teamDef;

        public List<StatData> _teamAtkStat;
        public List<StatData> _teamDefStat;


        [SerializeField] protected Transform posSkill;
        [SerializeField] protected SpriteRenderer fadeScreen;

        [SerializeField] protected CharaterTurnbase character;

        protected List<CharaterTurnbase> targets;

        [SerializeField] protected long totalHpTeamAtk = 0;
        [SerializeField] protected long totalDefTeamAtk = 0;

        protected CombatResult combatResult;
        protected float speed=1;

        public abstract void StartCombat();
        protected abstract void Initialized();
        protected abstract void CombatEnd();

        public void StopCombat()
        {
            StopCoroutine(Combat());
        }

        public void ChangeSpeed(float speed)
        {
            this.speed = speed;
            foreach (var item in _teamAtk)
            {
                item.ChangeSpeed(speed);
            }

            foreach (var item in _teamDef)
            {
                item.ChangeSpeed(speed);
            }
        }

        protected abstract CombatResult GetCombatResult();

        protected IEnumerator Combat()
        {
            Debug.Log($"value => <b><color=green> Start Combat </color></b>");

            // Kiểm tra điều kiện kết thúc trận đấu
            if (HandleResult())
            {
                yield break;
            }

            var combatResult = GetCombatResult();

            foreach (var combatInfo in combatResult.combatDataResults)
            {
                character = GetCharacterById(combatInfo.idLocationSelf, combatInfo.idTeamSelf);

                if (character == null || character.IsDie)
                {
                    continue; // Bỏ qua nhân vật đã chết
                }

                foreach (var hit in combatInfo.hitDataResults)
                {
                    // Xóa danh sách mục tiêu trước mỗi lượt tấn công
                    targets.Clear();
                    Debug.Log(" count =" + hit.hitInfos.Count);

                    foreach (var info in hit.hitInfos)
                    {
                        Debug.Log($"Atk id = {character.IdLocation} --- team = {character.TeamType}  ->> Def id = {info.idLocationTarget} --- team = {info.idTeamTarget}");

                        CharaterTurnbase cha = GetCharacterById(info.idLocationTarget, info.idTeamTarget);

                        if (cha != null)
                        {
                            targets.Add(cha);
                        }
                    }

                    // Tạo danh sách mục tiêu tạm thời
                    var tempTarget = new List<CharaterTurnbase>();
                    tempTarget.AddRange(targets);

                    // Xử lý tấn công
                    HandleAttack(hit, character, tempTarget);

                    // Đợi hành động của nhân vật kết thúc
                    yield return new WaitUntil(() => character.endAction);

                    if (HandleResult())
                    {
                        yield break;
                    }
                }
            }

            StartCoroutine(Combat());
            // Kiểm tra lại sau khi hoàn thành lượt tấn công
        }

        protected bool HandleResult()
        {
            bool endCombat = CheckCombatOver();
            endCombat = CheckCombatOver();
            if (endCombat)
            {
                Debug.Log("Combat ended");

                if (isTeamAtkLose)
                {
                    Debug.Log("Team ATK lost");
                    foreach (var item in _teamDef)
                    {
                        item.ShowWin();
                    }
                    // Xử lý khi đội ATK thua
                }
                else if (isTeamDefLose)
                {
                    Debug.Log("Team DEF lost");
                    // Hiển thị chiến thắng cho đội ATK
                    foreach (var item in _teamAtk)
                    {
                        item.ShowWin();
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }


        protected void ResetFadeScreen()
        {
            fadeScreen.enabled = false;
            character?.ResetView();
            foreach (var item in targets)
            {
                item.ResetView();
            }
        }


        protected void HandleAttack(HitDataResult hit, CharaterTurnbase attacker, List<CharaterTurnbase> targets)
        {
            switch (hit.attackType)
            {
                case AttackType.NORMAL:
                    Debug.Log($"<b><color=green>_Log </color></b> : " + attacker.name);

                    attacker.Attack(hit.hitInfos, targets);
                    break;
                case AttackType.SKILL:
                    fadeScreen.enabled = true;
                    attacker.ActiveFadeView();
                    foreach (var item in targets)
                    {
                        item.ActiveFadeView();
                    }

                    Vector3 posTemp = hit.attackType == AttackType.SKILL ? posSkill.position : targets[0].transform.position;

                    attacker.Skill(hit.hitInfos, this.targets, posTemp);
                    break;
                case AttackType.BUFF:
                    break;
                case AttackType.DEBUFF:
                    break;
            }
        }

        protected bool isTeamAtkLose;
        protected bool isTeamDefLose;

        protected bool CheckCombatOver()
        {
            isTeamAtkLose = _teamAtk.TrueForAll(c => c.IsDie);
            isTeamDefLose = _teamDef.TrueForAll(c => c.IsDie);
            bool isCombatOver = isTeamAtkLose || isTeamDefLose;
            //Debug.Log("isCombatOver " + isCombatOver);
            return isCombatOver;
        }


        protected CharaterTurnbase GetCharacterById(int id, TeamType teamType)
        {
            List<CharaterTurnbase> team = teamType == TeamType.ATK ? _teamAtk : _teamDef;

            // Kiểm tra danh sách null hoặc rỗng
            if (team == null || team.Count == 0)
            {
                return null;
            }

            CharaterTurnbase character = team
                .Where(c => c != null) // Bỏ qua các phần tử null
                .FirstOrDefault(c =>
                    c.IdLocation == id && // Điều kiện 1: ID khớp
                    !c.IsDie && // Điều kiện 2: Nhân vật chưa chết
                    c.IsActive); // Điều kiện 3: Nhân vật đang hoạt động


            return character;
        }
    }
}