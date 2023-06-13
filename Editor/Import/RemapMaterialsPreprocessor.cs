using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Cuboid.UnityPlugin.Editor
{
    /// <summary>
    /// Sets the material import settings so that it instantly searches for the
    /// material using the name inside the folder
    /// (to circumvent "Material Editing is not supported on multiple selection"
    /// </summary>
    public class RemapMaterialsPreprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            ModelImporter modelImporter = assetImporter as ModelImporter;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
            modelImporter.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.RecursiveUp);
        }
    }
}
