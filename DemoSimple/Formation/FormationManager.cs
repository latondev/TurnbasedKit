using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TapDBMiniJSON;
using UnityEngine;


namespace MythydRpg
{
    public class FormationManager : Singleton<FormationManager>
    {
        public Action<List<int>> OnUpdateLineUp;
        [SerializeField] private List<Formation> _formations;
        [SerializeField] FormationPanel _formationPanel;


        private void OnValidate()
        {
            _formations = GetComponentsInChildren<Formation>().ToList();
        }
        private void Awake()
        {
            if (_formationPanel != null)
            {
                _formationPanel.OnChangeFormation += OnChangeLineUp;
                _formationPanel.OnBack += OnBack;
            }
        }

        private void OnBack()
        {

            OnUpdateLineUp?.Invoke(GetFormation(LineUpMode.PVE));
        }

        void Start()
        {
        }

        public List<int> GetFormation(LineUpMode mode)
        {
            return _formations.Find(x => x.LineUpMode == mode).LineUp;
        }
        Formation GetFormationMode(LineUpMode mode)
        {
            return _formations.Find(x => x.LineUpMode == mode);
        }

        private void OnChangeLineUp(List<int> list)
        {

            var lineUp = GetFormationMode(LineUpMode.PVE);
            lineUp.ChangeLineUp(list);

            string url = ServerGateWay.GetApi().urlSaveLineUp;
            Dictionary<string, Dictionary<string, object>> dataJson = new Dictionary<string, Dictionary<string, object>>();

            var userData = new Dictionary<string, object>();

            userData.Add("id", ClientData.ID); 
            userData.Add("lineUp", list);
            dataJson.Add("user", userData);

            // Chuyển đổi sang JSON
            string dataSend = JsonHelper.Serialize(dataJson);

            ServerGateWay.PostAPI(url, dataSend.ToString());
        }

        public void ShowPanel(List<CharacterDataSO> ownerSO)
        {
            Debug.Log("vao k");
            var lineUps = GetFormation(LineUpMode.PVE);
            _formationPanel.gameObject.Show();
            _formationPanel.ShowPanel(lineUps, ownerSO);

        }

        public void LoadFormation(List<int> formation)
        {
            var pve = GetFormationMode(LineUpMode.PVE);
            pve.ChangeLineUp(formation);
        }
    }
}