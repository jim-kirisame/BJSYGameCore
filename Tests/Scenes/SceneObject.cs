//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UI
{
    using BJSYGameCore.UI;
    using BJSYGameCore.AutoCompo;
    using System;
    using UnityEngine;
    
    [AutoCompoAttribute(24514)]
    public partial class SceneObject : UIObject
    {
        public void init()
        {
            this._asRect = this.transform.findByPath("./").GetComponent<RectTransform>();
        }
        [SerializeField()]
        [AutoCompoAttribute(24516, "./")]
        private RectTransform _asRect;
        public RectTransform asRect
        {
            get
            {
                return this._asRect;
            }
        }
    }
}