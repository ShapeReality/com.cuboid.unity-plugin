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
        public static void ConvertSelectionToCollection()
        {
            // First determine conversion type


            //Debug.Assert(Utils.IsFolder(Selection.activeObject));

            //string path = AssetDatabase.GetAssetPath(Selection.activeObject);

            //string projectDirectory = ProjectDirectoryPath;
            ////Debug.Log(projectDirectory);

            //string fullPath = Path.Combine(projectDirectory, path);

            //List<GameObject> gameObjects = new List<GameObject>();
            //RealityAssetCollection collection = (RealityAssetCollection)ScriptableObject.CreateInstance(nameof(RealityAssetCollection));
            //collection.Author = Application.companyName;

            //// first get all files inside the folder
            //foreach (string filePath in Directory.EnumerateFiles(fullPath, "*.*", SearchOption.AllDirectories))
            //{
            //    string relativePath = filePath.Substring(projectDirectory.Length+1);
            //    Type type = AssetDatabase.GetMainAssetTypeAtPath(relativePath);

            //    if (type == typeof(GameObject))
            //    {
            //        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
            //        //Debug.Log(relativePath);
            //        collection.Assets.Add(obj);
            //    }
            //}

            //string targetPath = AssetDatabase.GenerateUniqueAssetPath(path + Constants.k_AssetExtension);
            //AssetDatabase.CreateAsset(collection, targetPath);

            //AssetDatabase.SaveAssets();

            //Selection.activeObject = collection;
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

        [MenuItem(k_MenuItemName, validate = true)]
        public static bool ConvertSelectionToCollectionValidate()
        {
            // can convert folders or separate GameObjects, but not mixed.
            // because folders will be done using batching (each folder getting their own
            // asset collection), and GameObjects will be combined. 

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

        
    }
}
