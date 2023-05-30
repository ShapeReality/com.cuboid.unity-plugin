using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cuboid.UnityPlugin.Editor
{
    public static class SerializationUtils
    {
        /// <summary>
        /// Returns paths for a list of objects in the project's folder
        /// </summary>
        public static List<string> ToPaths<T>(this List<T> objects) where T : Object
        {
            List<string> paths = new List<string>();
            foreach (Object obj in objects)
            {
                paths.Add(AssetDatabase.GetAssetPath(obj));
            }
            return paths;
        }

        public static List<T> FromPaths<T>(this List<string> paths) where T : Object
        {
            List<T> objects = new List<T>();
            for (int i = 0; i < paths.Count; i++)
            {
                T obj = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
                if (obj != null) { objects.Add(obj); }
            }
            return objects;
        }

        public static string ToJson<T>(this List<T> values)
        {
            return JsonUtility.ToJson(new ListWrapper<T>(values));
        }

        public static List<T> FromJson<T>(this string json)
        {
            return JsonUtility.FromJson<ListWrapper<T>>(json).Value;
        }

        /// <summary>
        /// Utility class for allowing <see cref="EditorPrefs"/> to serialize a
        /// List of values using <see cref="JsonUtility"/>. 
        /// </summary>
        [System.Serializable]
        private class ListWrapper<T>
        {
            public List<T> Value;

            public ListWrapper(List<T> value)
            {
                Value = value;
            }
        }

    }
}
