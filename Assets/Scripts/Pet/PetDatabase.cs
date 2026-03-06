using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Pet
{
    [CreateAssetMenu(fileName = "PetDatabase", menuName = "Game/Pet/Pet Database")]
    public class PetDatabase : ScriptableObject
    {
        public List<PetData> allPets = new List<PetData>();

        public PetData GetById(string id)
        {
            return allPets.Find(p => p && p.petId == id);
        }

        public List<PetData> GetByRarity(PetRarity rarity)
        {
            return allPets.FindAll(p => p && p.rarity == rarity);
        }
    }
}