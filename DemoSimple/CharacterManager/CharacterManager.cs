using System;
using System.Collections;
using System.Collections.Generic;
using TapDBMiniJSON;
using TapDBMiniJSON.SimpleJSON;
using UnityEngine;

namespace MythydRpg
{
    [Serializable]
    public class PowerConfig
    {
        public float atkBonus;
        public float hpBonus;
        public float mdefBonus;
        public float pdefBonus;
        public float expBonus;
    }

    public class CharacterManager : Singleton<CharacterManager>
    {
        [SerializeField] private List<CharacterDataSO> characters;
        [SerializeField] private List<CharacterDataSO> enemies;


        [SerializeField] private List<CharacterDataSO> ownerCharacters;

        [SerializeField] PowerConfig powerConfig;


        public List<CharacterDataSO> Characters => characters;

        public List<CharacterDataSO> OwnerCharacters => ownerCharacters;


        void Start()
        {
        }

        public void GetConfigSever()
        {
            string url = ServerGateWay.GetApi().urlConfigCharacter;
            ServerGateWay.GetAPI(url, OnResponseGetConfig);
        }

        private void OnResponseGetConfig(string json)
        {
            var jsonArray = JSON.Parse(json);
            for (int i = 0; i < jsonArray.Count; i++)
            {
                var jsonObject = jsonArray[i];

                // Lấy các giá trị từ JSON
                int id = jsonObject["id"].AsInt;
                string nameHero = jsonObject["nameHero"].Value;
                string typeCharacter = jsonObject["type"].Value;

                string rarity = jsonObject["rarity"].Value;
                int level = jsonObject["level"].AsInt;
                CharacterDataSO characterData = GetDataHero(id);
                characterData.id = id;
                characterData.type = (TypeCharacter)Enum.Parse(typeof(TypeCharacter), typeCharacter);

                characterData.nameHero = nameHero;
                characterData.rarity = (Rarity)Enum.Parse(typeof(Rarity), rarity);
                characterData.level = level;
                var data = jsonObject["data"];
                characterData.data.hp = data["hp"].AsInt;
                characterData.data.mp = data["mp"].AsInt;
                characterData.data.atk = data["atk"].AsInt;
                characterData.data.pdef = data["pdef"].AsInt;
                characterData.data.mdef = data["mdef"].AsInt;
                characterData.data.critRes = jsonObject["critRes"].AsFloat;
                characterData.data.hitRate = jsonObject["hitRate"].AsFloat;
                characterData.data.dodgeRate = jsonObject["dodgeRate"].AsFloat;
                characterData.data.dmgIncr = jsonObject["dmgIncr"].AsFloat;
                characterData.data.dmgRes = jsonObject["dmgRes"].AsFloat;
                characterData.data.critDmg = jsonObject["critDmg"].AsFloat;
                characterData.data.fatedBond = jsonObject["fatedBond"].Value;
                characterData.data.clanStats = jsonObject["clanStats"].Value;
                characterData.data.antiDivine = jsonObject["antiDivine"].AsFloat;
                characterData.data.resDivine = jsonObject["resDivine"].AsFloat;
                characterData.data.antiBuddha = jsonObject["antiBuddha"].AsFloat;
                characterData.data.resBuddha = jsonObject["resBuddha"].AsFloat;
                characterData.data.antiSorcerer = jsonObject["antiSorcerer"].AsFloat;
                characterData.data.resSorcerer = jsonObject["resSorcerer"].AsFloat;
                characterData.data.antiDemon = jsonObject["antiDemon"].AsFloat;
                characterData.data.resDemon = jsonObject["resDemon"].AsFloat;
                characterData.data.acc = jsonObject["acc"].AsFloat;
                characterData.data.eva = jsonObject["eva"].AsFloat;

                // Gán các kỹ năng
                characterData.skillBasic.nameSkill = jsonObject["skillBasic"]["nameSkill"].Value;
                characterData.skillBasic.descriptionSkill = jsonObject["skillBasic"]["descriptionSkill"].Value;
                
                characterData.skillUltimate.nameSkill = jsonObject["skillUltimate"]["nameSkill"].Value;
                characterData.skillUltimate.descriptionSkill = jsonObject["skillUltimate"]["descriptionSkill"].Value;

                // Bạn có thể lưu hoặc xử lý dữ liệu này tiếp theo
                // Debug.Log("Character " + (i + 1) + ": " + characterData.nameHero);
            }
        }


        public CharacterDataSO GetDataHero(int id)
        {
            return characters.Find(x => x.id == id);
        }

        public CharacterDataSO GetDataEnemy(int id)
        {
            return enemies.Find(x => x.id == id);
        }

        public List<Sprite> GetBgCardAvatar(List<int> listId)
        {
            List<Sprite> sprites = new List<Sprite>();
            foreach (var id in listId)
            {
                var character = characters.Find(x => x.id == id);
                if (character != null)
                {
                    sprites.Add(character.bgCardAvatar);
                }
            }

            return sprites;
        }

        public List<Sprite> GetCardAvatar(List<int> listId)
        {
            List<Sprite> sprites = new List<Sprite>();
            foreach (var id in listId)
            {
                var character = characters.Find(x => x.id == id);
                if (character != null)
                {
                    sprites.Add(character.cardRewardAvatar);
                }
            }

            return sprites;
        }


        public void OnLoadOwner(List<int> heroesList)
        {
            foreach (var item in heroesList)
            {
                var character = characters.Find(x => x.id == item);
                if (character != null)
                {
                    character.isUnlock = true;
                    ownerCharacters.Add(character);
                }
            }
        }

        public float GetValueAtkUpgrade(float valueBase, int level)
        {
            return valueBase + ((powerConfig.atkBonus * valueBase) * level)/10;
        }

        public float GetValueHpUpgrade(float valueBase, int level)
        {
            return valueBase + ((powerConfig.hpBonus* valueBase) * level)/10;
        }

        public float GetValueMdefUpgrade(float valueBase, int level)
        {
            return valueBase + ((powerConfig.mdefBonus* valueBase) * level)/10;
        }

        public float GetValuePdefUpgrade(float valueBase, int level)
        {
            return valueBase + ((powerConfig.pdefBonus* valueBase) * level)/10;
        }

        public void UpgradeLevel(int idHero, int level)
        {
            Debug.Log("UpgradeLevel => "+level);
            var hero = GetDataHero(idHero);
            hero.level += level;
            hero.data.atk = (int)GetValueAtkUpgrade(hero.data.atk, hero.level);
            hero.data.hp = (int)GetValueHpUpgrade(hero.data.hp, hero.level);
            hero.data.mdef = (int)GetValueMdefUpgrade(hero.data.mdef, hero.level);
            hero.data.pdef = (int)GetValuePdefUpgrade(hero.data.pdef, hero.level);
        }
    }
}