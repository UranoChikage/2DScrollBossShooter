using Core;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    /// <summary>AudioLibrary を Resources に作るメニュー</summary>
    public static class AudioLibraryMenu
    {
        private const string Folder = "Assets/Resources";
        private const string AssetPath = Folder + "/AudioLibrary.asset";

        [MenuItem("Tools/Audio/Create Audio Library")]
        private static void Create()
        {
            AudioLibrary existing = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }
            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);
            AudioLibrary asset = ScriptableObject.CreateInstance<AudioLibrary>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
