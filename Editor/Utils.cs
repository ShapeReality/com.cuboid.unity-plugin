using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
        /// Creates 
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static RealityAssetCollection CreateAssetCollection(List<GameObject> objects, string targetPath)
        {
            RealityAssetCollection collection = (RealityAssetCollection)ScriptableObject.CreateInstance(nameof(RealityAssetCollection));
            collection.Author = Application.companyName;
            collection.Assets = objects;

            targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath); // make sure there's no naming colision with a previously generated asset collection.  
            AssetDatabase.CreateAsset(collection, targetPath);
            AssetDatabase.SaveAssets();

            return collection;
        }
    }
}
