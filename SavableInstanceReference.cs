﻿using System;
using System.Linq;

using UnityEngine;

using TBSGameCore;

namespace TBSGameCore
{
    [Serializable]
    public class SavableInstanceReference
    {
        public int id;
        public string path;
        public SavableInstanceReference(int id, string path = "")
        {
            this.id = id;
            this.path = path;
        }
        public T to<T>(MonoBehaviour behaviour)
        {
            if (id > 0)
            {
                SavableInstance instance = behaviour.findInstance<SaveManager>().getInstanceById(id);
                if (string.IsNullOrEmpty(path))
                {
                    return instance.GetComponent<T>();
                }
                else
                {
                    string[] names = path.Split('/');
                    Transform child = instance.transform;
                    for (int i = 0; i < names.Length; i++)
                    {
                        child = child.Find(names[i]);
                    }
                    if (child != null)
                        return child.GetComponent<T>();
                    else
                        return default(T);
                }
            }
            else if (!string.IsNullOrEmpty(path))
            {
                string[] names = path.Split('/');
                GameObject root = behaviour.gameObject.scene.GetRootGameObjects().FirstOrDefault(e => { return e.name == names[0]; });
                if (root == null)
                    return default(T);
                Transform parent = root.transform;
                for (int i = 1; i < names.Length; i++)
                {
                    parent = parent.Find(names[i]);
                }
                if (parent != null)
                    return parent.GetComponent<T>();
                else
                    return default(T);
            }
            return default(T);
        }
    }
}