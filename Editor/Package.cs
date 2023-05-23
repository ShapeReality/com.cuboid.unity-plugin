using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cuboid.UnityPlugin
{
    public static class Package
    {
        private const string k_PackagePath = "Packages/com.cuboid.unity-plugin";

        public static string GetPath(string path)
        {
            return Path.Combine(k_PackagePath, path);
        }
    }
}
