using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace GameStudio.Engine.Workflow
{
    public class JsonAsset : ScriptableObject
    {
        public string rawtext;
        public List<JsonAsset> jAssetList;
        public JsonAsset()
        {
            rawtext = new JObject().ToString();
            jAssetList = new List<JsonAsset>();
        }
    }
}
