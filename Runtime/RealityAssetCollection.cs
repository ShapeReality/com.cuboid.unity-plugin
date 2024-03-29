using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Cuboid.UnityPlugin
{
    [CreateAssetMenu(fileName = DefaultFileName, menuName = "Cuboid/Asset Collection")]
    public class RealityAssetCollection : ScriptableObject
    {
        public const string DefaultFileName = "Asset Collection";

        public string Author;

        public DateTime CreationDate;

        public List<GameObject> Assets = new List<GameObject>();
    }

    [System.Serializable]
    public class SerializedRealityAssetCollection
    {
        public string Name;

        public string Author;

        public DateTime CreationDate;

        public List<string> AddressableNames = new List<string>();

        /// <summary>
        /// Identifier, composed of the name and author, in lower case
        /// </summary>
        public string Identifier => string.Join('-', Author, Name).ToLower();
    }
}