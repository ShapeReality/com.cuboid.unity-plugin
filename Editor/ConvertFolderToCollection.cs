using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace Cuboid.UnityPlugin.Editor
{
    public static class FolderToCollection
    {
        private const string k_MenuItemName = "Assets/Convert to Asset Collection";

        /// <summary>
        /// Naive implementation (we shouldn't have to load the GameObjects into memory,
        /// rather, just add them manually to the .meta yaml file)
        /// </summary>
        [MenuItem(k_MenuItemName, priority = 0)]
        public static void ConvertFolderToCollection()
        {
            Debug.Assert(IsFolder(Selection.activeObject));

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            string projectDirectory = ProjectDirectoryPath;
            Debug.Log(projectDirectory);

            string fullPath = Path.Combine(projectDirectory, path);

            List<GameObject> gameObjects = new List<GameObject>();
            RealityAssetCollection collection = (RealityAssetCollection)ScriptableObject.CreateInstance(nameof(RealityAssetCollection));
            collection.Author = Application.companyName;

            // first get all files inside the folder
            foreach (string filePath in Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = filePath.Substring(projectDirectory.Length+1);
                Type type = AssetDatabase.GetMainAssetTypeAtPath(relativePath);

                if (type == typeof(GameObject))
                {
                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                    //Debug.Log(relativePath);
                    collection.Assets.Add(obj);
                }
            }

            string targetPath = AssetDatabase.GenerateUniqueAssetPath(path + Constants.k_AssetExtension);
            AssetDatabase.CreateAsset(collection, targetPath);

            AssetDatabase.SaveAssets();

            Selection.activeObject = collection;
        }

        private static string ProjectDirectoryPath
        {
            get
            {
                string dataPath = Application.dataPath;
                return dataPath.Substring(0, dataPath.Length - "/Assets".Length);
            }
        }

        [MenuItem(k_MenuItemName, validate = true)]
        public static bool ConvertFolderToCollectionValidate()
        {
            return IsFolder(Selection.activeObject);
        }

        private static bool IsFolder(Object obj)
        {
            DefaultAsset folder = obj as DefaultAsset;
            if (folder == null) { return false; }
            string path = AssetDatabase.GetAssetPath(folder);
            if (path == null) { return false; }
            return AssetDatabase.IsValidFolder(path);
        }
    }
}
