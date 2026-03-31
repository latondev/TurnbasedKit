using System.Collections.Generic;
using UnityEngine;

namespace GameSystems.Battle
{
    /// <summary>
    /// Status View - displays status effects.
    /// </summary>
    public class StatusView : MonoBehaviour
    {
        [SerializeField] private List<StatusIcon> activeStatusIcons = new List<StatusIcon>();

        public void AddStatus(StatusEffectType type, float duration)
        {
            Debug.Log($"StatusView: Added {type} for {duration}s");
        }

        public void RemoveStatus(StatusEffectType type)
        {
            Debug.Log($"StatusView: Removed {type}");
        }

        public void ClearAll()
        {
            activeStatusIcons.Clear();
        }
    }

    [System.Serializable]
    public class StatusIcon
    {
    }
}
