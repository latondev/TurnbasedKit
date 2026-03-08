using System.Collections;
using System.Collections.Generic;
using SaintsField;
using UnityEngine;
namespace MythydRpg
{
    [System.Serializable]
    public class CharacterDataStats
    {
        // Các chỉ số cơ bản
        public int hp; // Sức khỏe
        public int mp; // Sức khỏe

        public int atk; // Sức mạnh tấn công
        public float pdef; // Phòng thủ vật lý
        public float mdef; // Phòng thủ phép
        public float speed;

        // Chỉ số nâng cao
        [OverlayRichLabel("<color=grey>/%")] public float crit; // Tỷ lệ chí mạng

        public float critRes; // Kháng chí mạng

        public float hitRate; // Tỷ lệ trúng

        public float dodgeRate; // Tỷ lệ né tránh

        public float dmgIncr; // Tăng sát thương

        public float dmgRes; // Kháng sát thương

        public float critDmg; // Sát thương chí mạng

        // Chỉ số Fated Bond và Clan Stats
        public string fatedBond; // Liên kết định mệnh

        public string clanStats; // Thông tin Clan

        // Các chỉ số đặc biệt (Anti và Res các loại)
        public float antiDivine; // Kháng thần

        public float resDivine; // Hấp thụ thần

        public float antiBuddha; // Kháng Phật

        public float resBuddha; // Hấp thụ Phật

        public float antiSorcerer; // Kháng pháp sư

        public float resSorcerer; // Hấp thụ pháp sư

        public float antiDemon; // Kháng ma quái

        public float resDemon; // Hấp thụ ma quái
        public float acc = 1;
        public float eva = 1;
    }
}