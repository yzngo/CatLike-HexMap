using System;
using UnityEngine;

namespace HexMap.Scripts
{
    [Serializable]
    public class HexFeatureCollection
    {
        public Transform[] prefabs;
        public Transform Pick(float choice)
        {
            return prefabs[(int)(choice * prefabs.Length)];
        }
    }
}