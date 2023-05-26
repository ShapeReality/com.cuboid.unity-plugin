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
    }
}
