using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UnityPlugin
{
    public static class SerializationUtils
    {
        public static string ToJson(this List<int> values)
        {
            return JsonUtility.ToJson(new IntList(values));
        }

        public static List<int> ToIntList(this string json)
        {
            return JsonUtility.FromJson<IntList>(json).Value;
        }

        /// <summary>
        /// Utility class for allowing <see cref="EditorPrefs"/> to serialize a
        /// List of ints using <see cref="JsonUtility"/>. 
        /// </summary>
        [System.Serializable]
        private class IntList
        {
            public List<int> Value;

            public IntList(List<int> value)
            {
                Value = value;
            }
        }

    }
}
