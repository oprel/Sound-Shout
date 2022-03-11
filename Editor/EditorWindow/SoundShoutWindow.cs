using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace SoundShout.Editor
{
    public class SoundShoutWindow : EditorWindow
    {
        private const string ROOT_TOOL_PATH = "Packages/se.somethingwemade.soundshout/Editor/EditorWindow";
        private const string SETTINGS_PATH = ROOT_TOOL_PATH + "/Settings.txt";
        private const string TOOL_LOGO_PATH = ROOT_TOOL_PATH + "/SS_Tool_Logo.png";
        private const string AUDIO_REFERENCE_ICON_PATH = ROOT_TOOL_PATH + "/SS_Asset_Logo.png";

        private const string MENU_ITEM_CATEGORY = "SWM/Sound Shout/";

        public const string CLIENT_SECRET_PATH = ROOT_TOOL_PATH + "/client_secret.json";
        public const string APPLICATION_NAME = "TOEM";

        private static TextField spreadsheetURLTextField;
        
        private static bool IsClientSecretsFileAvailable() { return File.Exists(CLIENT_SECRET_PATH); }

        private const int MIN_SIZE = 256;
        
        private class ExporterSettings
        {
            public string spreadSheetURL;
        }

        [MenuItem(MENU_ITEM_CATEGORY + "Open Tool")]
        public static void OpenWindow()
        {
            SoundShoutWindow wnd = GetWindow<SoundShoutWindow>();
            wnd.titleContent = new GUIContent("Sound Shout")
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>(AUDIO_REFERENCE_ICON_PATH),
            };
            wnd.minSize = new Vector2(MIN_SIZE, MIN_SIZE);
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(CreateToolTitleVisualElement());
            
            ScrollView rootContainer = new ScrollView();
            rootVisualElement.Add(rootContainer);
            
            rootContainer.Add(CreateSetupTools());
            rootContainer.Add(CreateUsageTools());

            SetupValues();
        }

        private static VisualElement CreateToolTitleVisualElement()
        {
            VisualElement titleContainer = new VisualElement
            {
                style =
                {
                    maxHeight = MIN_SIZE
                }
            };

            Image soundShoutLogo = Utilities.CreateImage(TOOL_LOGO_PATH);
            soundShoutLogo.scaleMode = ScaleMode.ScaleToFit;
            
            titleContainer.Add(soundShoutLogo);
            
            return titleContainer;
        }
        
        private static VisualElement CreateSetupTools()
        {
            Foldout setupFoldout = new Foldout
            {
                text = IsClientSecretsFileAvailable() ? "Initial Setup ✓" : "Initial Setup",
                style = { backgroundColor = new StyleColor(Color.black)}
            };

            var viewSetupVideoButton = Utilities.CreateButton("Setup Video", () => Process.Start("https://www.youtube.com/watch?v=afTiNU6EoA8"));
            setupFoldout.Add(viewSetupVideoButton);

            var openGoogleConsoleButton = Utilities.CreateButton("Open Google Console", () => Process.Start("https://console.developers.google.com"));
            setupFoldout.Add(openGoogleConsoleButton);

            setupFoldout.Add(CreateLocateClientSecretButton());

            spreadsheetURLTextField = Utilities.CreateTextField("Spreadsheet URL");
            setupFoldout.Add(spreadsheetURLTextField);

            setupFoldout.Add(Utilities.CreateButton("Save Settings", SaveSettings));

            return setupFoldout;
        }

        private static VisualElement CreateUsageTools()
        {
            Foldout setupFoldout = new Foldout
            {
                text = "Export/Import",
            };

            if (!IsClientSecretsFileAvailable())
            {
                setupFoldout.Add(Utilities.CreateLabel("Please finish the setup!"));
            }
            else
            {
                setupFoldout.Add(Utilities.CreateButton("Open Spreadsheet", OpenGoogleSheetData));
                setupFoldout.Add(Utilities.CreateButton("Update Spreadsheet", () => { SpreadSheetLogic.UpdateAudioSpreadSheet(LoadSettings().spreadSheetURL); }));
                setupFoldout.Add(Utilities.CreateButton("Fetch Spreadsheet Changes", () => { SpreadSheetLogic.FetchSpreadsheetChanges(LoadSettings().spreadSheetURL); }));
                setupFoldout.Add(Utilities.CreateButton("Apply Formatting", () => { SpreadSheetLogic.ApplyFormattingToTopRows(LoadSettings().spreadSheetURL); }));
                // setupFoldout.Add(Utilities.CreateButton("Upload Local Changes", () => { SpreadSheetLogic.UploadLocalChanges(LoadSettings().spreadSheetURL); }));
            }
            
            return setupFoldout;
        }

        
        private static void OpenGoogleSheetData() { Process.Start($"https://docs.google.com/spreadsheets/d/{LoadSettings().spreadSheetURL}"); }
        
        private static void AddNewToolbarButton(Toolbar toolbar, string text, Action onClicked)
        {
            var button = Utilities.CreateButton(text, onClicked);
            toolbar.Add(button);
        }
        
        
        
        private void SetupValues()
        {
            var settings = LoadSettings();
            if (settings != null)
            {
                spreadsheetURLTextField.value = settings.spreadSheetURL;
            }
        }

        private static ExporterSettings LoadSettings()
        {
            if (File.Exists(SETTINGS_PATH))
            {
                var content = File.ReadAllText(SETTINGS_PATH);
                var settings = JsonUtility.FromJson<ExporterSettings>(content);
                return settings;
            }

            return null;
        }

        private static void SaveSettings()
        {
            ExporterSettings exporterSettings = new ExporterSettings
            {
                spreadSheetURL = spreadsheetURLTextField.value
            };
            var content = JsonUtility.ToJson(exporterSettings, true);

            File.WriteAllText(SETTINGS_PATH, content);
            Debug.Log("Saved settings");
        }

        private static VisualElement CreateLocateClientSecretButton()
        {
            var browseButton = Utilities.CreateButton("Locate \"client_secrets.json\"", () =>
            {
                string path = EditorUtility.OpenFilePanel("Select client_secrets.json file", ROOT_TOOL_PATH, "json");
                if (path.Length != 0)
                {
                    File.WriteAllText(CLIENT_SECRET_PATH, File.ReadAllText(path));
                }
            });

            return browseButton;
        }
    }
}