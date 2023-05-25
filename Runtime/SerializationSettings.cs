using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Cuboid.UnityPlugin
{
    public static class SerializationSettings
    {
        public static JsonSerializerSettings RealityAssetCollectionJsonSerializationSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };
    }
}
