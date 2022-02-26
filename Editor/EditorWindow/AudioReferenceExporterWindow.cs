using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

public class AudioReferenceExporterWindow : EditorWindow
{
    private const string PACKAGE_PATH = "Packages/se.somethingwemade.audioreference/Editor/EditorWindow";
    private const string SETTINGS_PATH = PACKAGE_PATH + "/Settings.txt";
    
    public const string CLIENT_SECRET_PATH = PACKAGE_PATH + "/client_secret.json";
    public const string APPLICATION_NAME = "TOEM";

    public class ExporterSettings
    {
        public string spreadSheetURL;
    }
    
    [MenuItem("SWM/AudioReference")]
    public static void ShowExample()
    {
        AudioReferenceExporterWindow wnd = GetWindow<AudioReferenceExporterWindow>();
        wnd.titleContent = new GUIContent("AudioReference Exporter");
        wnd.minSize = new Vector2(256, 256);
    }

    private TextField credentialsPath;
    private TextField spreadsheetURLTextField;
    
    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        VisualElement container = new VisualElement();
        root.Add(container);

        Image swmLogo = new Image
        {
            image = AssetDatabase.LoadAssetAtPath<Texture>( $"{PACKAGE_PATH}/SWM_Logo.png"),
            focusable = false,
            scaleMode = ScaleMode.ScaleToFit
        };
        container.Add(swmLogo);

        CreateButton("Open Google Console", () => Process.Start("https://console.developers.google.com"));
        CreateButton("Setup Video", () => Process.Start("https://www.youtube.com/watch?v=afTiNU6EoA8"));
        
        CreateTextField("Name of game:");

        
        credentialsPath = CreateTextField("client_secret.json");
        credentialsPath.value = "??";
        credentialsPath.SetEnabled(false);
        CreateButton("Update SpreadSheet", () =>
        {
            AudioReferenceExporter.UpdateAudioSpreadSheet(LoadSettings());
        });
        
        root.Add(credentialsPath);
        
        SelectCredentialsButton();

        spreadsheetURLTextField = CreateTextField("SpreadSheet URL");
        spreadsheetURLTextField.isPasswordField = true;
        
        
        CreateButton("Save Settings", SaveSettings);
        SetupValues();
    }

    private void SetupValues()
    {
        var settings = LoadSettings();
        if (settings != null)
        {
            spreadsheetURLTextField.value = settings.spreadSheetURL;
        }
    }

    public static ExporterSettings LoadSettings()
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
    
    private void SelectCredentialsButton()
    {
        var browseButton = CreateButton("Locate \"client_secrets.json\"" , () =>
        {
            string path = EditorUtility.OpenFilePanel("Select client_secrets.json file", PACKAGE_PATH, "json");
            if (path.Length != 0)
            {
                File.WriteAllText( CLIENT_SECRET_PATH, File.ReadAllText(path));
            }
        });
        
        rootVisualElement.Add(browseButton);
    }

    private TextField CreateTextField(string label)
    {
        var field = new TextField
        {
            multiline = false,
            label = label
        };
        
        rootVisualElement.Add(field);
        return field;
    }
    
    private Button CreateButton(string buttonText, Action onClicked)
    {
        var button = new Button
        {
            text = buttonText,
        };

        button.clicked += onClicked;
        rootVisualElement.Add(button);
        return button;
    }
}