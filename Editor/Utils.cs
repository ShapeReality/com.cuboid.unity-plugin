using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cuboid.UnityPlugin.Editor
{
    public static class Utils
    {
        public static bool IsPrefab(Object obj)
        {
            GameObject gameObject = obj as GameObject;
            return gameObject != null && gameObject.scene.name == null && gameObject.scene.rootCount == 0;
        }

        public static bool IsFolder(Object obj)
        {
            DefaultAsset folder = obj as DefaultAsset;
            if (folder == null) { return false; }
            string path = AssetDatabase.GetAssetPath(folder);
            if (path == null) { return false; }
            return AssetDatabase.IsValidFolder(path);
        }

        /// <summary>
        /// Creates a new asset collection in the project's Assets folder at the given targetPath
        /// Will use a unique path name if the one provided already exists.
        /// Sets the author to the company name. 
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static RealityAssetCollection CreateAssetCollection(List<GameObject> objects, string targetPath)
        {
            RealityAssetCollection collection = ScriptableObject.CreateInstance<RealityAssetCollection>();
            collection.Author = Application.companyName;
            collection.Assets = objects;

            targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath); // make sure there's no naming colision with a previously generated asset collection.  
            AssetDatabase.CreateAsset(collection, targetPath);
            AssetDatabase.SaveAssets();

            return collection;
        }

        /// <summary>
        /// Gets all asset collections of type <see cref="RealityAssetCollection"/>
        /// that exist in the Assets folder. 
        /// </summary>
        public static List<RealityAssetCollection> GetAssetCollectionsInProject()
        {
            List<RealityAssetCollection> collections = new List<RealityAssetCollection>();
            string[] guids = AssetDatabase.FindAssets("t:RealityAssetCollection");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RealityAssetCollection asset = AssetDatabase.LoadAssetAtPath<RealityAssetCollection>(path);
                collections.Add(asset);
            }

            return collections;
        }

        /// <summary>
        /// 
        /// </summary>
        public static Texture2D GetCollectionThumbnail(RealityAssetCollection collection)
        {
            return ThumbnailProvider.GetThumbnail((collection != null && collection.Assets.Count > 0) ? collection.Assets[0] : null);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Delete(List<RealityAssetCollection> collections)
        {
            foreach (RealityAssetCollection collection in collections)
            {
                DeleteAssetCollectionInternal(collection);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Delete(RealityAssetCollection collection)
        {
            DeleteAssetCollectionInternal(collection);

            AssetDatabase.Refresh();
        }

        private static void DeleteAssetCollectionInternal(RealityAssetCollection collection)
        {
            if (collection == null) { return; }
            string path = AssetDatabase.GetAssetPath(collection);
            AssetDatabase.DeleteAsset(path);
        }

        /// <summary>
        /// Duplicate a list of collections and set the active Selection to the duplicated collections
        /// </summary>
        /// <param name="collections"></param>
        public static void Duplicate(List<RealityAssetCollection> collections)
        {
            List<RealityAssetCollection> duplicatedCollections = new List<RealityAssetCollection>();
            foreach (RealityAssetCollection collection in collections)
            {
                RealityAssetCollection duplicatedCollection = DuplicateAssetCollectionInternal(collection);
                if (duplicatedCollection == null) { continue; }
            }

            AssetDatabase.Refresh();

            SetSelection(duplicatedCollections);
        }

        /// <summary>
        /// Duplicate a single collection and set the active Selection to the duplicated collection
        /// </summary>
        /// <param name="collection"></param>
        public static void Duplicate(RealityAssetCollection collection)
        {
            RealityAssetCollection duplicatedCollection = DuplicateAssetCollectionInternal(collection);
            if (duplicatedCollection == null) { return; }

            AssetDatabase.Refresh();

            Debug.Log("duplicated");

            SetSelection(duplicatedCollection);
        }

        private static RealityAssetCollection DuplicateAssetCollectionInternal(RealityAssetCollection collection)
        {
            if (collection == null) { return null; }

            string path = AssetDatabase.GetAssetPath(collection);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
            bool success = AssetDatabase.CopyAsset(path, newPath);
            if (!success)
            {
                throw new Exception($"Failed to duplicate selected Asset Collection from {path} to {newPath}");
            }
            RealityAssetCollection duplicatedCollection = AssetDatabase.LoadAssetAtPath<RealityAssetCollection>(newPath);
            return duplicatedCollection;
        }

        public static void SetSelection<T>(T selection) where T : Object
        {
            Selection.activeObject = selection;
        }

        public static void SetSelection<T>(List<T> selection) where T : Object
        {
            if (selection == null) { return; }
            Object[] objects = new Object[selection.Count];
            for (int i = 0; i < selection.Count; i++)
            {
                objects[i] = selection[i];
            }
            Selection.objects = objects;
        }

        public static bool ContainsSameItemsAs<T>(this List<T> values1, List<T> values2) where T : Object
        {
            if (values1 == null || values2 == null) { return false; }

            HashSet<T> values1HashSet = values1.ToHashSet();
            HashSet<T> values2HashSet = values2.ToHashSet();

            values2HashSet.SymmetricExceptWith(values1HashSet);
            return values2HashSet.Count == 0;
        }

        public static List<T> Filter<T>(IEnumerable<Object> objects, Func<Object, bool> valid = null) where T : Object
        {
            List<T> values = new List<T>();
            foreach (Object obj in objects)
            {
                if (valid == null || (valid != null && valid.Invoke(obj)))
                {
                    T value = obj as T;
                    if (value != null) { values.Add(value); }
                }
            }
            return values;
        }

        public static List<GameObject> GetPrefabsInObjects(IEnumerable<Object> objects)
        {
            return Filter<GameObject>(objects, (o) => Utils.IsPrefab(o));
        }
    }
}
