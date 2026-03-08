using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    [System.Serializable]
    public class PowerConfig
    {
        public float atkBonus = 0.1f;
        public float hpBonus = 0.1f;
        public float mdefBonus = 0.1f;
        public float pdefBonus = 0.1f;
        public float expBonus = 0.1f;
    }

    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        [SerializeField] private List<CharacterDataSO> characters = new List<CharacterDataSO>();
        [SerializeField] private List<CharacterDataSO> enemies = new List<CharacterDataSO>();
        [SerializeField] private List<CharacterDataSO> ownerCharacters = new List<CharacterDataSO>();

        [SerializeField] private PowerConfig powerConfig;

        public List<CharacterDataSO> Characters => characters;
        public List<CharacterDataSO> Enemies => enemies;
        public List<CharacterDataSO> OwnerCharacters => ownerCharacters;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
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
                if (character != null && character.cardAvatar != null)
                {
                    sprites.Add(character.cardAvatar);
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
                if (character != null && character.cardAvatar != null)
                {
                    sprites.Add(character.cardAvatar);
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
                    if (!ownerCharacters.Contains(character))
                    {
                        ownerCharacters.Add(character);
                    }
                }
            }
        }

        public float GetValueAtkUpgrade(float valueBase, int level)
        {
            if (powerConfig == null) return valueBase;
            return valueBase + ((powerConfig.atkBonus * valueBase) * level) / 10;
        }

        public float GetValueHpUpgrade(float valueBase, int level)
        {
            if (powerConfig == null) return valueBase;
            return valueBase + ((powerConfig.hpBonus * valueBase) * level) / 10;
        }

        public float GetValueMdefUpgrade(float valueBase, int level)
        {
            if (powerConfig == null) return valueBase;
            return valueBase + ((powerConfig.mdefBonus * valueBase) * level) / 10;
        }

        public float GetValuePdefUpgrade(float valueBase, int level)
        {
            if (powerConfig == null) return valueBase;
            return valueBase + ((powerConfig.pdefBonus * valueBase) * level) / 10;
        }

        public void UpgradeLevel(int idHero, int level)
        {
            Debug.Log($"UpgradeLevel => id:{idHero} level:{level}");
            var hero = GetDataHero(idHero);
            if (hero == null) return;

            hero.level += level;
            hero.stats.atk = (int)GetValueAtkUpgrade(hero.stats.atk, hero.level);
            hero.stats.hp = (int)GetValueHpUpgrade(hero.stats.hp, hero.level);
            hero.stats.mdef = (int)GetValueMdefUpgrade(hero.stats.mdef, hero.level);
            hero.stats.pdef = (int)GetValuePdefUpgrade(hero.stats.pdef, hero.level);
        }

        public void AddCharacter(CharacterDataSO character)
        {
            if (!characters.Contains(character))
            {
                characters.Add(character);
            }
        }

        public void AddEnemy(CharacterDataSO enemy)
        {
            if (!enemies.Contains(enemy))
            {
                enemies.Add(enemy);
            }
        }

        public void ClearCharacters()
        {
            characters.Clear();
            enemies.Clear();
            ownerCharacters.Clear();
        }
    }
}
