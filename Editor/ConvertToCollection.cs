using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

namespace Cuboid.UnityPlugin.Editor
{
    public static class ConvertToCollection
    {
        private const string k_MenuItemName = "Assets/Convert to Asset Collection";

        /// <summary>
        /// Naive implementation (we shouldn't have to load the GameObjects into memory,
        /// rather, just add them manually to the .meta yaml file)
        /// </summary>
        [MenuItem(k_MenuItemName, priority = 0)]
        public static void ConvertSelectionToCollection()
        {
            Object[] objects = Selection.objects;
            if (objects.Length == 0) { return; }

            // Determine conversion type
            ConversionType conversionType = GetConversionType();
            
            switch (conversionType)
            {
                case ConversionType.None:
                    return;
                case ConversionType.Batched:
                    ConvertBatched(objects);
                    break;
                case ConversionType.Combine:
                    ConvertCombined(objects);
                    break;
            }
        }

        [MenuItem(k_MenuItemName, validate = true)]
        public static bool ConvertSelectionToCollectionValidate()
        {
            Object[] selection = Selection.objects;

            if (selection.Length == 0) { return false; }

            bool valid = false; // selection contains

            for (int i = 0; i < selection.Length; i++)
            {
                if (GetAssetType(selection[i]) != AssetType.Other)
                {
                    valid = true;
                    break;
                }
            }

            return valid;
        }

        /// <summary>
        /// Converts an array of objects (assumed to be folders)
        /// </summary>
        private static void ConvertBatched(Object[] objects)
        {
            if (objects.Length == 0) { return; }

            // we assume all selected objects are folders, but if they're not, we skip them

            List<RealityAssetCollection> collections = new List<RealityAssetCollection>();

            for (int i = 0; i < objects.Length; i++)
            {
                Object obj = objects[i];
                if (!Utils.IsFolder(obj)) { continue; }
                RealityAssetCollection collection = ConvertFolder(obj);
                if (collection != null)
                {
                    collections.Add(collection);
                }
            }

            // set the selection to the collections
            if (collections.Count == 0) { return; }

            Object[] selection = new Object[collections.Count];
            for (int i = 0; i < collections.Count; i++)
            {
                selection[i] = collections[i];
            }
            Selection.objects = selection;
        }

        private static void ConvertCombined(Object[] objects)
        {
            if (objects.Length == 0) { return; }

            List<GameObject> prefabs = new List<GameObject>();

            Object lastValidObject = null;
            for (int i = 0; i < objects.Length; i++)
            {
                Object obj = objects[i];
                
                if (Utils.IsFolder(obj))
                {
                    List<GameObject> gameObjects = GetPrefabsInFolder(obj);
                    prefabs.AddRange(gameObjects);
                    if (gameObjects.Count > 0)
                    {
                        lastValidObject = obj;
                    }
                }
                else if (Utils.IsPrefab(obj))
                {
                    GameObject gameObject = obj as GameObject;
                    prefabs.Add(gameObject);
                    lastValidObject = obj;
                }
            }

            if (prefabs.Count == 0) { return; }

            // now create an asset collection, use the last object as the place where the collection will be
            // created
            string path = AssetDatabase.GetAssetPath(lastValidObject);
            int index = path.LastIndexOf('.');
            path = index != -1 ? path.Substring(0, index) : path; // remove the .prefab or .fbx from the file name
            string targetPath = path + Constants.k_AssetExtension;
            RealityAssetCollection collection = Utils.CreateAssetCollection(prefabs, targetPath);

            // set the selection
            if (collection != null)
            {
                Selection.activeObject = collection;
            }
        }

        /// <summary>
        /// Recursively find all GameObjects that are
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static List<GameObject> GetPrefabsInFolder(Object obj)
        {
            List<GameObject> objects = new List<GameObject>();
            if (!Utils.IsFolder(obj)) { return objects; }

            string path = AssetDatabase.GetAssetPath(obj);
            string projectDirectory = ProjectDirectoryPath;
            string fullPath = Path.Combine(projectDirectory, path);

            // first get all files inside the folder
            foreach (string filePath in Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = filePath.Substring(projectDirectory.Length + 1);
                Type type = AssetDatabase.GetMainAssetTypeAtPath(relativePath);

                if (type == typeof(GameObject))
                {
                    GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                    if (gameObject != null)
                    {
                        objects.Add(gameObject);
                    }
                }
            }

            return objects;
        }

        /// <summary>
        /// Converts a folder to an asset collection, which will be saved next to the folder location. 
        /// </summary>
        private static RealityAssetCollection ConvertFolder(Object obj)
        {
            if (obj == null || !Utils.IsFolder(obj)) { return null; }

            List<GameObject> gameObjects = GetPrefabsInFolder(obj);
            string targetPath = AssetDatabase.GetAssetPath(obj) + Constants.k_AssetExtension;

            RealityAssetCollection collection = Utils.CreateAssetCollection(gameObjects, targetPath);
            return collection;
        }

        private enum AssetType
        {
            Folder,
            Prefab,
            Other
        }

        private enum ConversionType
        {
            None,
            Batched,
            Combine
        }

        private static AssetType GetAssetType(Object obj)
        {
            if (Utils.IsFolder(obj))
            {
                return AssetType.Folder;
            }
            else if (Utils.IsPrefab(obj))
            {
                return AssetType.Prefab;
            }
            else
            {
                return AssetType.Other;
            }
        }

        private static ConversionType GetConversionType()
        {
            Object[] selection = Selection.objects;
            if (selection.Length == 0) { return ConversionType.None; }

            AssetType firstType = GetAssetType(selection[0]);
            bool valid = false;
            bool different = false;

            for (int i = 0; i < selection.Length; i++)
            {
                Object obj = selection[i];

                AssetType type = GetAssetType(obj);
                if (type == AssetType.Other) { continue; }
                valid = true;

                if (type != firstType)
                {
                    // this means we have to combine, since we can't do
                    different = true;
                }
            }

            if (valid)
            {
                return firstType == AssetType.Folder && !different ? ConversionType.Batched : ConversionType.Combine;
            }
            return ConversionType.None;
        }

        private static string ProjectDirectoryPath
        {
            get
            {
                string dataPath = Application.dataPath;
                return dataPath.Substring(0, dataPath.Length - "/Assets".Length);
            }
        }
    }
}
