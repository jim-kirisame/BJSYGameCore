﻿using System;
using System.Collections.Generic;

using UnityEngine;

namespace BJSYGameCore.SaveSystem
{
    [Serializable]
    public class SaveData
    {
        public string name = "本地游戏";
        public DateTime date = DateTime.Now;
        public List<SavableInstanceData> instances = null;
        public List<SaveObjectData> savedObjects = null;
    }
}