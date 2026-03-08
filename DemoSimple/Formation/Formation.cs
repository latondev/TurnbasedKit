using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MythydRpg
{
    public enum LineUpMode
    {
        PVE,
        PVP
    }

    public class Formation : MonoBehaviour
    {
        [SerializeField] LineUpMode _lineUpMode;
        [SerializeField] private List<int> _lineUp;


        public LineUpMode LineUpMode => _lineUpMode;
        public List<int> LineUp => _lineUp;

        public void ChangeLineUp(List<int> lineUp)
        {
            Debug.Log("loadd =");

            foreach (var item in lineUp)
            {
                Debug.Log("i = " + item);

            }

            _lineUp = lineUp;
        }


    }
}