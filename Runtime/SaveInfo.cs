using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Celezt.SaveSystem
{
    public readonly struct SaveInfo
    {
        public readonly int Version;
        public readonly int SceneIndex;

        public SaveInfo(int version, int sceneIndex)
        {
            Version = version;
            SceneIndex = sceneIndex;
        }
    }
}
