using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UnityPlugin
{
    [CreateAssetMenu(fileName = "Asset Collection", menuName = "Cuboid/Asset Collection")]
    public class RealityAssetCollection : ScriptableObject
    {
        public string Author;

        public DateTime CreationDate;

        public List<GameObject> Assets = new List<GameObject>();
    }
}