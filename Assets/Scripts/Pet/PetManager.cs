using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameSystems.Pet
{
    [Serializable]
    public class PetSaveData
    {
        public List<PetInstance> ownedPets = new List<PetInstance>();
        public string activePetInstanceId;
        public DateTime lastSaveUtc;
    }

    public class PetManager : MonoBehaviour
    {
        [Header("Config")]
        public PetDatabase database;

        [Header("Storage")]
        [SerializeField] private string saveKey = "PET_SAVE_V1";
        [SerializeField] private PetSaveData saveData = new PetSaveData();

        // Runtime
        private Dictionary<string, PetInstance> instances = new Dictionary<string, PetInstance>();
        public PetInstance ActivePet { get; private set; }

        // Events
        public event Action<PetInstance, PetData> OnPetAcquired;
        public event Action<PetInstance, PetData, int> OnPetLevelUp;
        public event Action<PetInstance, PetData> OnPetEvolved;
        public event Action<PetInstance, PetData> OnPetActivated;

        void Awake()
        {
            Load();
            IndexInstances();
            RestoreActivePet();
            
            // ✅ ADD: Auto summon starter pet nếu chưa có
            if (saveData.ownedPets.Count == 0 && database != null && database.allPets.Count > 0)
            {
                Debug.Log("[Pet] No pets found, summoning starter pet...");
                var starterPet = database.allPets[0]; // lấy pet đầu tiên làm starter
                var instance = SummonPet(starterPet.petId);
                if (instance != null)
                {
                    SetActivePet(instance.instanceId);
                    Debug.Log($"[Pet] Auto-summoned starter: {starterPet.petName}");
                }
            }
        }

        void OnDestroy()
        {
            Save();
        }

        private void IndexInstances()
        {
            instances.Clear();
            foreach (var pet in saveData.ownedPets)
                instances[pet.instanceId] = pet;
        }

        private void RestoreActivePet()
        {
            if (!string.IsNullOrEmpty(saveData.activePetInstanceId))
            {
                // ✅ FIX: Gán trực tiếp vào property thay vì tạo biến local
                instances.TryGetValue(saveData.activePetInstanceId, out PetInstance ActivePet);
                
                if (ActivePet != null)
                {
                    Debug.Log($"[Pet] Restored active pet: {ActivePet.petId}");
                }
            }
        }

        // === APIs ===
        public PetInstance SummonPet(string petId)
        {
            var data = database?.GetById(petId);
            if (data == null)
            {
                Debug.LogWarning($"[Pet] Pet {petId} not found in database.");
                return null;
            }

            var instance = new PetInstance(petId);
            saveData.ownedPets.Add(instance);
            instances[instance.instanceId] = instance;
            OnPetAcquired?.Invoke(instance, data);
            Save();
            Debug.Log($"[Pet] Summoned: {data.petName} (ID: {instance.instanceId})");
            return instance;
        }

        public void SetActivePet(string instanceId)
        {
            if (!instances.TryGetValue(instanceId, out var pet))
            {
                Debug.LogWarning($"[Pet] Instance {instanceId} not found.");
                return;
            }
            ActivePet = pet;
            saveData.activePetInstanceId = instanceId;
            var data = database?.GetById(pet.petId);
            OnPetActivated?.Invoke(pet, data);
            Save();
            Debug.Log($"[Pet] Set active: {data?.petName}");
        }

        public void AddExpToActivePet(long amount)
        {
            if (ActivePet == null) return;
            var data = database?.GetById(ActivePet.petId);
            if (data == null) return;

            int oldLevel = ActivePet.level;
            ActivePet.AddExp(amount, data);
            if (ActivePet.level > oldLevel)
                OnPetLevelUp?.Invoke(ActivePet, data, ActivePet.level);
            Save();
        }

        public void AddAffectionToActivePet(int amount)
        {
            if (ActivePet == null) return;
            var data = database?.GetById(ActivePet.petId);
            if (data == null) return;

            ActivePet.AddAffection(amount, data);
            Save();
        }

        public bool EvolvePet(string instanceId, Func<string, int, bool> consumeMaterial, Func<long, bool> spendGold = null)
        {
            if (!instances.TryGetValue(instanceId, out var pet))
                return false;

            var data = database?.GetById(pet.petId);
            if (data == null || data.evolvesTo == null)
                return false;
            
            if (!pet.CanEvolve(data, consumeMaterial))
                return false;

            // Consume material
            if (!string.IsNullOrEmpty(data.evolveMaterialId) && data.evolveMaterialQty > 0)
            {
                if (!(consumeMaterial?.Invoke(data.evolveMaterialId, data.evolveMaterialQty) ?? false))
                    return false;
            }

            // ✅ ADD: Gold cost check
            if (spendGold != null)
            {
                long goldCost = 10000 * (int)data.rarity;
                if (!spendGold(goldCost))
                    return false;
            }

            // Evolve
            pet.petId = data.evolvesTo.petId;
            pet.level = 1;
            pet.exp = 0;
            OnPetEvolved?.Invoke(pet, data.evolvesTo);
            Save();
            return true;
        }

        public IEnumerable<(PetInstance instance, PetData data)> GetAllPets()
        {
            foreach (var inst in saveData.ownedPets)
            {
                var data = database?.GetById(inst.petId);
                if (data != null)
                    yield return (inst, data);
            }
        }

        public PetStats GetActivePetBuffs()
        {
            if (ActivePet == null) return new PetStats();
            var data = database?.GetById(ActivePet.petId);
            return data != null ? ActivePet.GetCurrentStats(data) : new PetStats();
        }

        // === Persistence ===
        public void Save()
        {
            saveData.lastSaveUtc = DateTime.UtcNow;
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(saveKey, "");
            if (string.IsNullOrEmpty(json)) 
            { 
                saveData = new PetSaveData(); 
                return; 
            }
            try 
            { 
                saveData = JsonUtility.FromJson<PetSaveData>(json) ?? new PetSaveData(); 
                Debug.Log($"[Pet] Loaded {saveData.ownedPets.Count} pets from save");
            }
            catch (Exception e)
            { 
                Debug.LogError($"[Pet] Load error: {e.Message}");
                saveData = new PetSaveData(); 
            }
        }

        public void ResetAll()
        {
            saveData = new PetSaveData();
            instances.Clear();
            ActivePet = null;
            Save();
            Debug.Log("[Pet] All pet data reset");
        }
    }
}
