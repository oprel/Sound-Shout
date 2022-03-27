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
        private static string SpreedSheetURL => SoundShoutSettings.GetSettings.spreadsheetURL;
        private const string MENU_ITEM_CATEGORY = "SWM/Sound Shout";
        private const int MIN_SIZE = 256;
        
        [MenuItem(MENU_ITEM_CATEGORY)]
        public static void OpenWindow()
        {
            SoundShoutWindow wnd = GetWindow<SoundShoutWindow>();
            wnd.titleContent = new GUIContent("Sound Shout")
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>(SoundShoutPaths.AUDIO_REFERENCE_ICON_PATH),
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
        }

        public static VisualElement CreateToolTitleVisualElement()
        {
            VisualElement titleContainer = new VisualElement
            {
                style =
                {
                    maxHeight = MIN_SIZE
                }
            };

            Image soundShoutLogo = Utilities.CreateImage(SoundShoutPaths.TOOL_LOGO_PATH);
            soundShoutLogo.scaleMode = ScaleMode.ScaleToFit;
            
            titleContainer.Add(soundShoutLogo);
            
            return titleContainer;
        }
        
        private static VisualElement CreateSetupTools()
        {
            Foldout setupFoldout = new Foldout
            {
                text = SoundShoutSettings.IsClientSecretsFileAvailable() ? "Initial Setup ✓" : "Initial Setup",
                style = { backgroundColor = new StyleColor(Color.black)}
            };

            var viewSetupVideoButton = Utilities.CreateButton("Setup Video", () => Process.Start("https://www.youtube.com/watch?v=afTiNU6EoA8"));
            setupFoldout.Add(viewSetupVideoButton);

            var openGoogleConsoleButton = Utilities.CreateButton("Open Google Console", () => Process.Start("https://console.developers.google.com"));
            setupFoldout.Add(openGoogleConsoleButton);

            setupFoldout.Add(CreateLocateClientSecretButton());

            var tweakSettingsButton = Utilities.CreateButton("Tweak Settings", SoundShoutSettings.SelectAsset);
            setupFoldout.Add(tweakSettingsButton);
            
            return setupFoldout;
        }

        private static VisualElement CreateUsageTools()
        {
            Foldout setupFoldout = new Foldout
            {
                text = "Export/Import",
            };

            if (!SoundShoutSettings.IsClientSecretsFileAvailable())
            {
                setupFoldout.Add(Utilities.CreateLabel("Please finish the setup!"));
            }
            else
            {
                setupFoldout.Add(Utilities.CreateButton("Open Spreadsheet", OpenGoogleSheetData));
                setupFoldout.Add(Utilities.CreateButton("Update Spreadsheet", () => { SpreadSheetLogic.UpdateAudioSpreadSheet(SpreedSheetURL); }));
                setupFoldout.Add(Utilities.CreateButton("Fetch Spreadsheet Changes", () => { SpreadSheetLogic.FetchSpreadsheetChanges(SpreedSheetURL); }));
                setupFoldout.Add(Utilities.CreateButton("Apply Formatting", () => { SpreadSheetLogic.ApplyFormatting(SpreedSheetURL); }));
                // setupFoldout.Add(Utilities.CreateButton("Upload Local Changes", () => { SpreadSheetLogic.UploadLocalChanges(SpreedSheetURL); }));
            }
            
            return setupFoldout;
        }

        
        private static void OpenGoogleSheetData() { Process.Start($"https://docs.google.com/spreadsheets/d/{SpreedSheetURL}"); }
        
        private static void AddNewToolbarButton(Toolbar toolbar, string text, Action onClicked)
        {
            var button = Utilities.CreateButton(text, onClicked);
            toolbar.Add(button);
        }
        
        private static VisualElement CreateLocateClientSecretButton()
        {
            var browseButton = Utilities.CreateButton("Locate \"client_secrets.json\"", () =>
            {
                string path = EditorUtility.OpenFilePanel("Select client_secrets.json file", SoundShoutPaths.TOOL_PATH, "json");
                if (path.Length != 0)
                {
                    File.WriteAllText(SoundShoutPaths.CLIENT_SECRET_PATH, File.ReadAllText(path));
                }
            });

            return browseButton;
        }
    }
}