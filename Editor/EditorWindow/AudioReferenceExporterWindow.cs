using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace AudioReferenceEditor
{
    public class AudioReferenceExporterWindow : EditorWindow
    {
        private const string ROOT_TOOL_PATH = "Packages/se.somethingwemade.audioreference/Editor/EditorWindow";
        private const string SETTINGS_PATH = ROOT_TOOL_PATH + "/Settings.txt";

        private const string MENU_ITEM_CATEGORY = "SWM/AudioReference/";

        public const string CLIENT_SECRET_PATH = ROOT_TOOL_PATH + "/client_secret.json";
        public const string APPLICATION_NAME = "TOEM";

        private TextField credentialsPathTextField;
        private TextField spreadsheetURLTextField;
        private Toolbar toolbar;
        
        public class ExporterSettings
        {
            public string spreadSheetURL;
        }

        private static void OpenGoogleSheetData()
        {
            Process.Start($"https://docs.google.com/spreadsheets/d/{LoadSettings().spreadSheetURL}");
        }

        [MenuItem(MENU_ITEM_CATEGORY + "Open Tool")]
        public static void OpenWindow()
        {
            AudioReferenceExporterWindow wnd = GetWindow<AudioReferenceExporterWindow>();
            wnd.titleContent = new GUIContent("AudioReference Tool");
            wnd.minSize = new Vector2(256, 256);
        }

        public void CreateGUI()
        {
            VisualElement rootContainer = new VisualElement();
            rootVisualElement.Add(rootContainer);

            // Create Title & Logo
            rootContainer.Add(CreateToolTitleVisualElement());

            toolbar = new Toolbar();
            rootContainer.Add(toolbar);

            AddNewToolbarButton("Open Google Console", () => Process.Start("https://console.developers.google.com"));
            AddNewToolbarButton("Open Spreadsheet", OpenGoogleSheetData);
            AddNewToolbarButton("Setup Video", () => Process.Start("https://www.youtube.com/watch?v=afTiNU6EoA8"));
             
            credentialsPathTextField = Utilities.CreateTextField("client_secret.json");
            credentialsPathTextField.value = HasClientSecretsFile() ? CLIENT_SECRET_PATH : "??";
            credentialsPathTextField.SetEnabled(false);
            rootContainer.Add(credentialsPathTextField);

            Button updateSpreadsheetBtn = Utilities.CreateButton("Update SpreadSheet", () => { AudioReferenceExporter.UpdateAudioSpreadSheet(LoadSettings().spreadSheetURL); });
            rootContainer.Add(updateSpreadsheetBtn);

            Button fetchChangesBtn = Utilities.CreateButton("Fetch SpreadSheet Changes", () => { AudioReferenceExporter.FetchSpreadSheetChanges(LoadSettings().spreadSheetURL); });
            rootContainer.Add(fetchChangesBtn);
            
            CreateSelectCredentialsButton(rootContainer);

            spreadsheetURLTextField = Utilities.CreateTextField("SpreadSheet URL");
            rootContainer.Add(spreadsheetURLTextField);

            rootContainer.Add(Utilities.CreateButton("Save Settings", SaveSettings));
            SetupValues();
        }

        private void AddNewToolbarButton(string text, Action onClicked)
        {
            var button = Utilities.CreateButton(text, onClicked);
            toolbar.Add(button);
        }
        
        private static VisualElement CreateToolTitleVisualElement()
        {
            VisualElement titleContainer = new VisualElement();
            
            titleContainer.Add(Utilities.CreateImage($"{ROOT_TOOL_PATH}/SWM_Logo.png"));
            
            return titleContainer;
        }

        bool HasClientSecretsFile() { return File.Exists(CLIENT_SECRET_PATH); }
        
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

        private void SaveSettings()
        {
            ExporterSettings exporterSettings = new ExporterSettings
            {
                spreadSheetURL = spreadsheetURLTextField.value
            };
            var content = JsonUtility.ToJson(exporterSettings, true);

            File.WriteAllText(SETTINGS_PATH, content);
            Debug.Log("Saved settings");
        }

        private static void CreateSelectCredentialsButton(VisualElement container)
        {
            var browseButton = Utilities.CreateButton("Locate \"client_secrets.json\"", () =>
            {
                string path = EditorUtility.OpenFilePanel("Select client_secrets.json file", ROOT_TOOL_PATH, "json");
                if (path.Length != 0)
                {
                    File.WriteAllText(CLIENT_SECRET_PATH, File.ReadAllText(path));
                }
            });

            container.Add(browseButton);
        }
    }
}