﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SoundShout.Editor
{
    internal class SoundShoutSettings : ScriptableObject
    {
        public const string ROOT_PATH = "Packages/se.somethingwemade.soundshout/";
        private const string SETTINGS_ASSET_PATH = ROOT_PATH + "Editor/Settings/Sound Shout Settings.asset";

        [SerializeField] public string spreadsheetURL;

        [Serializable] internal class ColorScheme
        {
            public AudioReference.ImplementationStatus implementationStatus;
            public Color color;
        }

        [SerializeField] internal List<ColorScheme> colorSchemes;

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