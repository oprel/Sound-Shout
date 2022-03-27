using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoundShout.Editor
{
    internal class SoundShoutSettings : ScriptableObject
    {
        internal static SoundShoutSettings GetSettings => GetOrCreateSettings();

        
        [SerializeField] public string spreadsheetURL;
        
        [SerializeField] internal List<ColorScheme> colorSchemes;
        [Serializable] internal class ColorScheme
        {
            public AudioReference.ImplementationStatus implementationStatus;
            public Color color;
        }

        internal static bool IsClientSecretsFileAvailable() { return File.Exists(SoundShoutPaths.CLIENT_SECRET_PATH); }

        internal static void SelectAsset()
        {
            Selection.SetActiveObjectWithContext(GetSettings, null);
        }
        
        private static SoundShoutSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SoundShoutSettings>(SoundShoutPaths.SETTINGS_ASSET_PATH);
            if (settings == null)
            {
                settings = CreateInstance<SoundShoutSettings>();
                AssetDatabase.CreateAsset(settings, SoundShoutPaths.SETTINGS_ASSET_PATH);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}