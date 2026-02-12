using System.IO;
using System;
using UnityEngine;
using System.Collections.Generic;
using DistantLands.Cozy.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DistantLands.Cozy
{

#if UNITY_EDITOR

    [UnityEditor.CustomPropertyDrawer(typeof(BlocksAttribute))]
    public class BlocksAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {


            float height = EditorGUIUtility.singleLineHeight;
            var unitARect = new Rect(position.x, position.y, position.width, height);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(unitARect, property, GUIContent.none);

            position = new Rect(position.x, position.y, position.width, position.height);

            if (property.objectReferenceValue != null)
            {
                BlocksBlendable profile = (BlocksBlendable)property.objectReferenceValue;
                (Editor.CreateEditor(profile) as E_BlocksBlendable).RenderInWindow(position);

            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineCount = 1;

            if (property.objectReferenceValue != null)
            {
                lineCount += 1;

            }
            return EditorGUIUtility.singleLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing * (lineCount - 1);
        }

    }
#endif


    public class BlocksAttribute : PropertyAttribute { }



}