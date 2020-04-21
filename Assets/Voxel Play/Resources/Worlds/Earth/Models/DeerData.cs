using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

namespace VoxelPlay
{

    public class DeerData : MonoBehaviour
    {

        public string animal = "Deer";
        public int hitPoints = 20;

        //void Start ()
        //{
        //    VoxelPlaySaveThis save = GetComponent<VoxelPlaySaveThis> ();
        //    save.OnSaveGame += OnSaveGame;
        //    save.OnLoadGame += OnLoadGame;
        //}

        public void OnSaveGame (Dictionary<string, string> data)
        {
            data ["Animal"] = animal;
            data ["HitPoints"] = hitPoints.ToString(CultureInfo.InvariantCulture);
        }

        public void OnLoadGame (Dictionary<string, string> data)
        {
            data.TryGetValue ("Animal", out animal);
            string s;
            data.TryGetValue ("HitPoints", out s);
            int.TryParse (s, out hitPoints);
        }
    }

}