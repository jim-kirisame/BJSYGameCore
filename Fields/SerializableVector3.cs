﻿using System;

using UnityEngine;

namespace BJSYGameCore
{
    [Serializable]
    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;
        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public SerializableVector3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }
        public static implicit operator Vector3(SerializableVector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
        public static implicit operator SerializableVector3(Vector3 v)
        {
            return new SerializableVector3(v);
        }
    }
}