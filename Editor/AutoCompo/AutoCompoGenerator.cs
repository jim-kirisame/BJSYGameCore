﻿using System;
using UnityEngine;
using System.CodeDom;
using System.Text.RegularExpressions;
using NUnit.Framework.Interfaces;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace BJSYGameCore.AutoCompo
{
    public class AutoCompoGenerator
    {
        /// <summary>
        /// 为游戏物体生成编译单元。
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public CodeCompileUnit genScript4GO(GameObject gameObject, AutoCompoGenSetting setting)
        {
            _setting = setting;
            _rootGameObject = gameObject;
            CodeCompileUnit unit = new CodeCompileUnit();
            //命名空间，引用
            CodeNamespace nameSpace = new CodeNamespace(setting.Namespace);
            unit.Namespaces.Add(nameSpace);
            foreach (string import in setting.usings)
            {
                nameSpace.Imports.Add(new CodeNamespaceImport(import));
            }
            //类
            _type = new CodeTypeDeclaration();
            nameSpace.Types.Add(_type);
            genType();
            return unit;
        }
        /// <summary>
        /// 默认生成一个自动绑定方法。
        /// </summary>
        protected virtual void genType()
        {
            _type.CustomAttributes.Add(new CodeAttributeDeclaration(typeof(AutoCompoAttribute).Name,
                new CodeAttributeArgument(new CodePrimitiveExpression(_rootGameObject.GetInstanceID()))));
            _type.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            _type.IsPartial = true;
            _type.IsClass = true;
            _type.Name = genTypeName4GO(_rootGameObject);
            foreach (var baseType in _setting.baseTypes)
            {
                _type.BaseTypes.Add(baseType);
            }
            genMethods();
            genRootGameObject();
        }
        /// <summary>
        /// 默认生成autoBind方法。
        /// </summary>
        protected virtual void genMethods()
        {
            _autoBindMethod = new CodeMemberMethod();
            _type.Members.Add(_autoBindMethod);
            _autoBindMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            _autoBindMethod.ReturnType = new CodeTypeReference(typeof(void));
            _autoBindMethod.Name = "autoBind";
        }
        /// <summary>
        /// 默认对根物体和子物体进行递归处理生成。
        /// </summary>
        protected virtual void genRootGameObject()
        {
            genGameObject(_rootGameObject);
        }
        /// <summary>
        /// 默认生成该物体及组件和子物体及组件的字段，属性，初始化。
        /// </summary>
        /// <param name="gameObject"></param>
        protected virtual void genGameObject(GameObject gameObject)
        {
            //根物体组件引用
            string typeName;
            string[] compoTypes;
            if (tryParseGOName(gameObject.name, out typeName, out compoTypes))
            {
                foreach (var compoTypeName in compoTypes)
                {
                    if (compoTypeName == typeof(GameObject).Name)
                    {
                        genField(typeof(GameObject).Name, genFieldName4GO(gameObject));
                        continue;
                    }
                    Component component = gameObject.GetComponent(compoTypeName);
                    if (component == null)
                        continue;
                    genCompo(component);
                }
            }
            //处理子物体
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject childGO = gameObject.transform.GetChild(i).gameObject;
                genGameObject(childGO);
            }
        }
        /// <summary>
        /// 默认生成字段，属性，以及初始化语句。
        /// </summary>
        /// <param name="component"></param>
        protected virtual void genCompo(Component component)
        {
            if (component.gameObject == _rootGameObject)
                genFieldPropInit4Compo(component, genFieldName4Compo(component),
                    genPropName4Compo(component), new string[0]);
            else
                genFieldPropInit4Compo(component, genFieldName4Compo(component),
                    genPropName4Compo(component), getPath(_rootGameObject, component.gameObject));
        }
        void genFieldPropInit4Compo(Component component, string fieldName, string propName, string[] path)
        {
            genFieldWithInit4Compo(component, fieldName, path);
            genProp4Compo(component, propName, fieldName);
        }
        void genProp4Compo(Component component, string propName, string fieldName)
        {
            CodeMemberProperty prop = new CodeMemberProperty();
            _type.Members.Add(prop);
            prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            prop.Type = new CodeTypeReference(component.GetType().Name);
            prop.Name = propName;
            prop.HasGet = true;
            prop.GetStatements.Add(new CodeMethodReturnStatement(
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),
                fieldName)));
        }
        void genFieldWithInit4Compo(Component component, string fieldName, string[] path)
        {
            genField4Compo(component, fieldName);
            CodeAssignStatement assign = new CodeAssignStatement();
            _autoBindMethod.Statements.Add(assign);
            assign.Left = new CodeFieldReferenceExpression(
                new CodeThisReferenceExpression(), fieldName);
            CodeExpression target = new CodeThisReferenceExpression();
            for (int i = 0; i < path.Length; i++)
            {
                if (i == 0)
                    target = new CodePropertyReferenceExpression(target, NAME_OF_TRANSFORM);
                target = new CodeMethodInvokeExpression(target, NAME_OF_FIND,
                    new CodePrimitiveExpression(path[i]));
            }
            assign.Right = new CodeMethodInvokeExpression(
                new CodeMethodReferenceExpression(target, NAME_OF_GETCOMPO,
                new CodeTypeReference(component.GetType().Name)));
        }
        protected CodeMemberField genField4Compo(Component component, string fieldName)
        {
            return genField(component.GetType().Name, fieldName);
        }
        protected CodeMemberField genField(string typeName, string fieldName)
        {
            CodeMemberField field = new CodeMemberField();
            _type.Members.Add(field);
            foreach (var fieldAttName in _setting.fieldAttributes)
            {
                field.CustomAttributes.Add(new CodeAttributeDeclaration(fieldAttName));
            }
            field.Attributes = MemberAttributes.Private | MemberAttributes.Final;
            field.Type = new CodeTypeReference(typeName);
            field.Name = fieldName;
            return field;
        }

        protected virtual string genTypeName4GO(GameObject gameObject)
        {
            string typeName;
            string[] compoTypes;
            if (tryParseGOName(gameObject.name, out typeName, out compoTypes))
                return typeName;
            else
                throw new FormatException(gameObject.name + "不符合格式\\w.\\w*");
        }
        bool tryParseGOName(string name, out string typeName, out string[] compoTypes)
        {
            var match = Regex.Match(name, @"(?<name>.+)\.(?<args>\w+(,\w+)*)");
            if (match.Success)
            {
                typeName = match.Groups["name"].Value;
                compoTypes = match.Groups["args"].Value.Split(',');
                return true;
            }
            else
            {
                typeName = string.Empty;
                compoTypes = new string[0];
                return false;
            }
        }
        /// <summary>
        /// 默认自己就叫_gameObject，子物体叫_子物体名。
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        protected virtual string genFieldName4GO(GameObject gameObject)
        {
            if (gameObject == _rootGameObject)
                return "_gameObject";
            else
                return "_" + gameObject.name;
        }
        /// <summary>
        /// 默认如果是根组件，那么叫做_as类型名，如果是子组件，那么叫_子物体名类型名。
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        protected virtual string genFieldName4Compo(Component component)
        {
            if (component.gameObject == _rootGameObject)
                return "_as" + component.GetType().Name;
            string fieldName;
            string[] compoTypes;
            if (tryParseGOName(component.gameObject.name, out fieldName, out compoTypes))
            {
                return "_" + fieldName + component.GetType().Name;
            }
            else
                throw new FormatException();
        }
        string genPropName4Compo(Component component)
        {
            if (component.gameObject == _rootGameObject)
                return "as" + component.GetType().Name;
            string propName;
            string[] compoTypes;
            if (tryParseGOName(component.gameObject.name, out propName, out compoTypes))
                return propName + component.GetType().Name;
            else
                throw new FormatException();
        }
        string[] getPath(GameObject parent, GameObject child)
        {
            if (parent.transform == child.transform)
                return new string[0];
            List<string> pathList = new List<string>();
            for (Transform transform = child.transform; transform != null; transform = transform.parent)
            {
                if (transform.gameObject == parent)
                    break;
                pathList.Add(transform.gameObject.name);
            }
            return pathList.ToArray();
        }
        protected AutoCompoGenSetting _setting;
        protected GameObject _rootGameObject;
        protected CodeTypeDeclaration _type;
        protected CodeMemberMethod _autoBindMethod;
        const string NAME_OF_TRANSFORM = "transform";
        const string NAME_OF_FIND = "Find";
        const string NAME_OF_GETCOMPO = "GetComponent";
    }
    [Serializable]
    public class AutoCompoGenSetting
    {
        public void loadFromPrefs(string name)
        {
            if (!EditorPrefs.HasKey(name + ":" + USINGS_LENGTH))
                return;
            usings = new string[EditorPrefs.GetInt(name + ":" + USINGS_LENGTH)];
            for (int i = 0; i < usings.Length; i++)
            {
                usings[i] = EditorPrefs.GetString(name + ":" + USINGS + i);
            }
            Namespace = EditorPrefs.GetString(name + ":" + NAMESPACE);
            baseTypes = new string[EditorPrefs.GetInt(name + ":" + BASETYPES_LENGTH)];
            for (int i = 0; i < baseTypes.Length; i++)
            {
                baseTypes[i] = EditorPrefs.GetString(name + ":" + BASETYPES + i);
            }
            fieldAttributes = new string[EditorPrefs.GetInt(name + ":" + FIELDATTRIBUTES_LENGTH)];
            for (int i = 0; i < fieldAttributes.Length; i++)
            {
                fieldAttributes[i] = EditorPrefs.GetString(name + ":" + FIELDATTRIBUTES + i);
            }
        }
        public void saveToPrefs(string name)
        {
            EditorPrefs.SetInt(name + ":" + USINGS_LENGTH, usings.Length);
            for (int i = 0; i < usings.Length; i++)
            {
                EditorPrefs.SetString(name + ":" + USINGS + i, usings[i]);
            }
            EditorPrefs.SetString(name + ":" + NAMESPACE, Namespace);
            EditorPrefs.SetInt(name + ":" + BASETYPES_LENGTH, baseTypes.Length);
            for (int i = 0; i < baseTypes.Length; i++)
            {
                EditorPrefs.SetString(name + ":" + BASETYPES + i, baseTypes[i]);
            }
            EditorPrefs.SetInt(name + ":" + FIELDATTRIBUTES_LENGTH, fieldAttributes.Length);
            for (int i = 0; i < fieldAttributes.Length; i++)
            {
                EditorPrefs.SetString(name + ":" + FIELDATTRIBUTES + i, fieldAttributes[i]);
            }
        }
        public string[] usings;
        public string Namespace;
        public string[] baseTypes;
        public string[] fieldAttributes;
        const string USINGS = "AutoCompoGenSetting.usings";
        const string USINGS_LENGTH = "AutoCompoGenSetting.usings.Length";
        const string NAMESPACE = "AutoCompoGenSetting.Namespace";
        const string BASETYPES = "AutoCompoGenSetting.baseTypes";
        const string BASETYPES_LENGTH = "AutoCompoGenSetting.baseTypes.Length";
        const string FIELDATTRIBUTES = "AutoCompoGenSetting.fieldAttributes";
        const string FIELDATTRIBUTES_LENGTH = "AutoCompoGenSetting.fieldAttributes.Length";
    }
}