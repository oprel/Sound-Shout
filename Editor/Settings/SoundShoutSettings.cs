using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SoundShout.Editor
{
    internal class SoundShoutSettings : ScriptableObject
    {
        private const string ROOT_PATH = "Packages/se.somethingwemade.soundshout/";
        private const string SETTINGS_ASSET_PATH = ROOT_PATH + "Editor/Settings/Sound Shout Settings.asset";

        internal static SoundShoutSettings GetSettings => GetOrCreateSettings();

        
        [SerializeField] public string spreadsheetURL;
        
        [SerializeField] internal List<ColorScheme> colorSchemes;
        [Serializable] internal class ColorScheme
        {
            public AudioReference.ImplementationStatus implementationStatus;
            public Color color;
        }

        internal const string CLIENT_SECRET_PATH = TOOL_PATH + "/client_secret.json";
        internal const string TOOL_PATH = ROOT_PATH + "/Editor/EditorWindow";
        internal static bool IsClientSecretsFileAvailable() { return File.Exists(CLIENT_SECRET_PATH); }

        internal static void SelectAsset()
        {
            Selection.SetActiveObjectWithContext(GetSettings, null);
        }
        
        private static SoundShoutSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SoundShoutSettings>(SETTINGS_ASSET_PATH);
            if (settings == null)
            {
                settings = CreateInstance<SoundShoutSettings>();
                AssetDatabase.CreateAsset(settings, SETTINGS_ASSET_PATH);
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