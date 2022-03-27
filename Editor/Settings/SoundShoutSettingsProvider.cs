using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace SoundShout.Editor
{
    internal static class SoundShoutSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSoundShoutSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Sound Shout Settings", SettingsScope.Project)
            {
                label = "Sound Shout Settings",
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = SoundShoutSettings.GetSerializedSettings();

                    rootElement.Add(SoundShoutWindow.CreateToolTitleVisualElement());

                    rootElement.Add(Utilities.CreateLabel("Spreadsheet URL")); 
                    
                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column
                        }
                    };
                    rootElement.Add(properties);

                    var urlField = Utilities.CreateTextField("");
                    urlField.bindingPath = "spreadsheetURL";
                    
                    properties.Add(urlField);
                    
                    var dialogueListView = new ListView();
                    dialogueListView.bindingPath = "colorSchemes";
                    dialogueListView.style.flexGrow = 1;

                    // We add all of our elements to the tree
                    // Then we bind once with at the common ancestor level
                    rootElement.Bind(settings);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                // keywords = new HashSet<string>(new[] {"Number", "Some String"})
            };

            return provider;
        }
    }
}