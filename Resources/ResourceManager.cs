﻿using UnityEngine;
using System;
using System.Threading.Tasks;
using BJSYGameCore.UI;
using System.CodeDom;
using UnityEngine.Networking;

namespace BJSYGameCore
{
    public partial class ResourceManager : Manager, IDisposable
    {
        #region 字段
        [SerializeField]
        ResourcesInfo _resourcesInfo;
        public ResourcesInfo resourcesInfo
        {
            get { return _resourcesInfo; }
            set { _resourcesInfo = value; }
        }
        #endregion

        #region 公开接口
        /// <summary>
        /// 同步的加载一个资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="info">资源信息</param>
        /// <returns>加载的资源</returns>
        public T load<T>(ResourceInfo info) where T : UnityEngine.Object
        {
            switch (typeof(T).Name)
            {
                case nameof(ResourcesInfo):
                    if (resourcesInfo.resourceList.Contains(info))
                        return resourcesInfo as T;
                    else
                    {
                        Debug.LogError($"ResourceManager::resoucesInfo里面没有{info.path}");
                        return null;
                    }
                case nameof(AssetBundleManifest):
                    return loadAssetBundleManifest(info) as T;
                default:
                    switch (info.type)
                    {
                        case ResourceType.Assetbundle:
                            if (typeof(T).Name == nameof(AssetBundle))
                            {
                                return loadAssetBundle(info) as T;
                            }
                            else return loadFromAssetBundle(info.path) as T;
                        case ResourceType.Resources:
                            return loadFromResources(info.path) as T;
                        case ResourceType.File:
                            //using (UnityWebRequest req = UnityWebRequest.Get(Application.streamingAssetsPath + info.path)) {
                            //    req.SendWebRequest();
                            //    while (!req.isDone) {Debug.Log("loading"); }
                            //    req.downloadHandler.data;
                            //}
                            //todo  : 不知道该如何处理，先放着.....
                            return null;
                    }
                    return null;
            }

        }
        /// <summary>
        /// 异步的加载一个资源。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<T> loadAsync<T>(string path)
        {
            throw new NotImplementedException();
        }
        #endregion


        #region 废弃方法，不知道还用不用得着，先留着
        /// <summary>
        /// 同步的加载一个资源。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        [Obsolete]
        public T load<T>(string path)
        {
            T res;
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("路径不能为空", nameof(path));
            else if (loadFromCache<T>(path, out var cachedRes))//尝试从缓存中加载
            {
                return cachedRes;
            }
            else if (path.StartsWith("res:"))//尝试从资源中加载
            {
                var uRes = Resources.Load(path.Substring(4, path.Length - 4));
                if (uRes == null)
                    res = default;
                else if (uRes is T t)
                    res = t;
                else
                    throw new InvalidCastException("资源\"" + path + "\"" + uRes + "不是" + typeof(T).Name);
            }
            else if (path.StartsWith("ab:") && resourcesInfo != null)
            {
                res = loadFromBundle<T>(resourcesInfo, path.Substring(3, path.Length - 3));
            }
            else
                throw new InvalidOperationException("无法加载类型为" + typeof(T).Name + "的资源" + path);
            saveToCache(path, res);
            return res;
        }
        [Obsolete]
        public T loadFromBundle<T>(ResourcesInfo abInfo, string path)
        {
            if (loadFromAssetBundle(abInfo, path) is T t)
                return t;
            else
                return default;
        }
        #endregion

        public void Dispose()
        {
            cacheDic.Clear();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(gameObject);
            else
                Destroy(gameObject);
#else
            Destroy(gameObject);
#endif
        }

    }
}
