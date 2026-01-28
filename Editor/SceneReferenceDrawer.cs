using UnityEditor;
using UnityEngine;

namespace Bootstrapper.Editor {
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            
            var sceneAssetProp = property.FindPropertyRelative("SceneAsset");
            var sceneNameProp = property.FindPropertyRelative("SceneName");
            var scenePathProp = property.FindPropertyRelative("ScenePath");
            
            EditorGUI.BeginChangeCheck();
            var newScene = EditorGUI.ObjectField(position, label, sceneAssetProp.objectReferenceValue, typeof(SceneAsset), false);
            
            if (EditorGUI.EndChangeCheck()) {
                sceneAssetProp.objectReferenceValue = newScene;
                
                if (newScene != null) {
                    var path = AssetDatabase.GetAssetPath(newScene);
                    scenePathProp.stringValue = path;
                    sceneNameProp.stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
                } else {
                    scenePathProp.stringValue = "";
                    sceneNameProp.stringValue = "";
                }
            }
            
            EditorGUI.EndProperty();
        }
    }
}