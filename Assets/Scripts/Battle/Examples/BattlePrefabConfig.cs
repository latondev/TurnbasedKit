using UnityEngine;

namespace GameSystems.Battle.Demo
{
    /// <summary>
    /// ScriptableObject config mapping unit → prefab.
    /// Dùng direct references thay vì Resources.Load vì prefabs nằm ngoài Resources folder.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Prefab Config", fileName = "BattlePrefabConfig")]
    public class BattlePrefabConfig : ScriptableObject
    {
        [Header("Player Team Prefabs")]
        [Tooltip("Prefabs cho player units (kéo thả từ Assets/AssetGame/ArtWork/Prefab/Role/)")]
        [SerializeField] private GameObject[] _playerPrefabs;

        [Header("Enemy Team Prefabs")]
        [Tooltip("Prefabs cho enemy units")]
        [SerializeField] private GameObject[] _enemyPrefabs;

        [Header("Formation")]
        [Tooltip("Vị trí X cơ sở cho player team")]
        [SerializeField] private float _playerBaseX = -3.5f;
        [Tooltip("Vị trí X cơ sở cho enemy team")]
        [SerializeField] private float _enemyBaseX = 3.5f;
        [Tooltip("Khoảng cách Y giữa các unit")]
        [SerializeField] private float _ySpacing = 1.5f;
        [Tooltip("Offset X xen kẽ (hàng trước/sau)")]
        [SerializeField] private float _xStagger = 0.5f;

        // ─── Properties ───
        public GameObject[] playerPrefabs => _playerPrefabs;
        public GameObject[] enemyPrefabs => _enemyPrefabs;
        public float playerBaseX => _playerBaseX;
        public float enemyBaseX => _enemyBaseX;
        public float ySpacing => _ySpacing;
        public float xStagger => _xStagger;

        /// <summary>
        /// Lấy prefab theo index (cycle nếu có ít prefab hơn units)
        /// </summary>
        public GameObject GetPlayerPrefab(int index)
        {
            if (_playerPrefabs == null || _playerPrefabs.Length == 0) return null;
            return _playerPrefabs[index % _playerPrefabs.Length];
        }

        /// <summary>
        /// Lấy prefab theo index (cycle nếu có ít prefab hơn units)
        /// </summary>
        public GameObject GetEnemyPrefab(int index)
        {
            if (_enemyPrefabs == null || _enemyPrefabs.Length == 0) return null;
            return _enemyPrefabs[index % _enemyPrefabs.Length];
        }

        /// <summary>
        /// Tính vị trí formation cho 1 unit trong team
        /// </summary>
        public Vector2 GetFormationPosition(int index, int teamSize, bool isPlayer)
        {
            float baseX = isPlayer ? _playerBaseX : _enemyBaseX;
            float stagger = (index % 2 == 0) ? 0 : _xStagger;
            float x = isPlayer ? baseX + stagger : baseX - stagger;

            // Centered vertically
            float totalHeight = (teamSize - 1) * _ySpacing;
            float y = (totalHeight / 2f) - (index * _ySpacing);

            return new Vector2(x, y);
        }
    }
}
