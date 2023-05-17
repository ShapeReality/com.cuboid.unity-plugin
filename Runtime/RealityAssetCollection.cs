using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cuboid.UnityPlugin
{
    [CreateAssetMenu(fileName = "Asset Collection", menuName = "Cuboid/Asset Collection")]
    public class RealityAssetCollection : ScriptableObject
    {
        public string Author;

        public DateTime CreationDate;

        public List<GameObject> Assets = new List<GameObject>();

        private static string _lastSelectedDirectoryPath;
        
        public void Build()
        {
            bool validPath = false;
            string targetPath = null;

            while (!validPath)
            {
                string directoryPath = EditorUtility.OpenFolderPanel("Select the folder where the Asset Collection will be built to", _lastSelectedDirectoryPath, _lastSelectedDirectoryPath);

                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException();
                }

                _lastSelectedDirectoryPath = directoryPath;

                string fileName = name + Constants.k_AssetCollectionFileExtension;
                targetPath = Path.Combine(directoryPath, fileName);

                if (File.Exists(targetPath))
                {
                    // 0 = ok, 1 = cancel, 2 = alt
                    int choice = EditorUtility.DisplayDialogComplex($"An Asset Collection with name {fileName} already exists at {directoryPath}.",
                        "Do you want to overwrite this Asset Collection? This cannot be undone. ",
                        "Overwrite", "Cancel", "Save As");

                    if (choice == 1) { Debug.LogWarning($"Asset Collection {name} Build Cancelled"); return; }
                    if (choice == 0)
                    {
                        File.Delete(targetPath);
                        validPath = true;
                    }
                }
                else
                {
                    validPath = true;
                }
            }

            File.WriteAllText(targetPath, "Dingetjes");
        }
    }
}