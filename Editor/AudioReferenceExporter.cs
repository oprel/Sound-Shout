//#define DEBUGGING

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AudioReferenceEditor
{
    public static class AudioReferenceExporter
    {
        private static SheetsService service;
        private static SheetsService Service
        {
            get
            {
                if (service == null)
                {
                    ConfigureGoogleCredentials();
                }

                return service;
            }
        }

        private enum UsedRows { EventName = 0, Is3D = 1, Looping = 2, Parameters = 3, Description = 4, Feedback = 5, ImplementStatus = 6 }
        private static readonly string[] scopes = { SheetsService.Scope.Spreadsheets };
        private const string OVERVIEW_TAB = "~Overview";
        private const string LAST_UPDATED_RANGE = OVERVIEW_TAB + "!H1";
        private const string START_RANGE = "A2";
        private const string END_RANGE = "G";
        private const string STANDARD_RANGE = START_RANGE + ":" + END_RANGE;
        private static int totalOperations, currentOperation;

        private static void ConfigureGoogleCredentials()
        {
            GoogleCredential credential;
            const string secretsPath = AudioReferenceExporterWindow.CLIENT_SECRET_PATH;
            using (var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
            }

            service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = AudioReferenceExporterWindow.APPLICATION_NAME,
            });
        }

        public static void FetchSpreadsheetChanges(string spreadSheetURL)
        {
            try
            {
                var ssRequest = Service.Spreadsheets.Get(spreadSheetURL);
                Spreadsheet ss = ssRequest.Execute();
                List<string> sheetTabs = new List<string>();
                foreach (Sheet sheet in ss.Sheets)
                {
                    if (sheet.Properties.Title == OVERVIEW_TAB)
                    {
                        continue;
                    }

                    sheetTabs.Add(sheet.Properties.Title);
                }

                var audioRefs = GetAllAudioReferences();
                ReadEntries(spreadSheetURL, ref audioRefs, ref sheetTabs);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Console.WriteLine(e);
                throw;
            }
        }

        public static void UploadLocalChanges(string spreadSheetURL)
        {
            var allAudioReferences = GetAllAudioReferences();
            CreateEntries(spreadSheetURL, ref allAudioReferences);
        }
        
        public static void UpdateAudioSpreadSheet(string spreadSheetURL)
        {
            try
            {
                // Progress bar
                totalOperations = 5; // Number of methods, used to calculate percentage for 

                currentOperation = 0;
                var audioRefs = GetAllAudioReferences();

                var tabCategories = GetAudioReferenceCategories(audioRefs);

                currentOperation++;
                ReadEntries(spreadSheetURL, ref audioRefs, ref tabCategories);

                currentOperation++;
                ClearAllSheetsRequest(spreadSheetURL, ref tabCategories);

                currentOperation++;
                CreateEntries(spreadSheetURL, ref audioRefs);

                currentOperation++;
                UpdateProgressBar("Cleaning up", 1);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();
                Debug.Log("AudioReferenceExporter: All AudioReference is up-to-date");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Console.WriteLine(e);
                throw;
            }
        }

        private static void UpdateProgressBar(string message, float progress)
        {
            EditorUtility.DisplayProgressBar("Updating AudioReferences", message, (progress + currentOperation) / totalOperations);
        }

        private static List<string> GetAudioReferenceCategories(IReadOnlyList<AudioReference> audioReferences)
        {
            var tabCategories = new List<string>();
            for (int i = 0; i < audioReferences.Count; i++)
            {
                var audioReference = audioReferences[i];
                if (!tabCategories.Contains(audioReference.category))
                {
                    tabCategories.Add(audioReference.category);
                }
            }

            return tabCategories;
        }

        private static void ReadEntries(string spreadsheetURL, ref AudioReference[] audioReferences, ref List<string> sheets)
        {
            List<AudioReference> newAudioRefsList = new List<AudioReference>(10);
            for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                var range = $"{sheets[sheetIndex]}!{STANDARD_RANGE}";
                var request = Service.Spreadsheets.Values.Get(spreadsheetURL, range);

                ValueRange response = request.Execute();
                IList<IList<object>> values = response.Values;
                if (values != null && values.Count > 0)
                {
                    // Go through each row and their data
                    foreach (var row in values)
                    {
                        string eventName = $"{sheets[sheetIndex]}/{(string)row[(int)UsedRows.EventName]}";
                        bool is3D = (string)row[(int)UsedRows.Is3D] == "3D";
                        bool isLooping = (string)row[(int)UsedRows.Looping] == "Loop";
                        string parameters = (string)row[(int)UsedRows.Parameters];
                        string description = (string)row[(int)UsedRows.Description];
                        string feedback = (string)row[(int)UsedRows.Feedback];

                        AudioReference.Status implementStatus = (AudioReference.Status)Enum.Parse(typeof(AudioReference.Status), (string)row[(int)UsedRows.ImplementStatus]);

                        bool newAudioReference = true;
                        string fullEventName = $"event:/{eventName}";
                        for (int i = 0; i < audioReferences.Length; i++)
                        {
                            if (audioReferences[i].fullEventPath == fullEventName)
                            {
                                audioReferences[i].ApplyChanges(is3D, isLooping, parameters, description, feedback, implementStatus);
                                newAudioReference = false;
                                break;
                            }
                        }

                        if (implementStatus == AudioReference.Status.Delete)
                        {
                            Debug.Log($"Skipped creating audio reference for \"{eventName}\" as it's marked as Delete!");
                        }
                        else if (newAudioReference)
                        {
                            var newSound = CreateNewAudioReferenceAsset(eventName, is3D, isLooping, parameters, description, feedback, implementStatus);
                            newAudioRefsList.Add(newSound);
                        }
#if DEBUGGING
                        Debug.Log($"Name: \"{eventName}\" " + $"3D: {is3D} - " + $"Loop: {isLooping} - " + $"Description: {description}" + $"Status: {implementStatus.ToString()}");
#endif
                    }
                }
                else
                {
                    Debug.Log($"No data was found in tab: \"{sheets[sheetIndex]}\"");
                }
            }

            int currentSize = audioReferences.Length;
            int newSize = currentSize + newAudioRefsList.Count;
            if (currentSize < newSize)
            {
                newAudioRefsList.AddRange(audioReferences);
                audioReferences = newAudioRefsList.ToArray();
            }
        }

        private static AudioReference CreateNewAudioReferenceAsset(string eventName, bool is3D, bool isLooping, string parameters, string description, string feedback, AudioReference.Status implementStatus)
        {
            AudioReference newAudioReference = ScriptableObject.CreateInstance<AudioReference>();
            Undo.RecordObject(newAudioReference, "Created new AudioReference");

            // Example eventName: "UI/Menus/Album_Open"
            string assetPath = $"Assets/Audio/{eventName}.asset";

            // Split assetPath so we get parent folder
            // Example Assets/Audio/UI/Menus <--
            int lastSlashIndex = assetPath.LastIndexOf('/');
            string unityAssetFolderPath = assetPath.Substring(0, lastSlashIndex);

            try
            {
                if (!AssetDatabase.IsValidFolder(unityAssetFolderPath))
                {
                    string unityProjectPath = Application.dataPath.Replace("Assets", "");
                    string absoluteAssetParentFolderPath = $"{unityProjectPath}{unityAssetFolderPath}";
                    Directory.CreateDirectory(absoluteAssetParentFolderPath);
                    AssetDatabase.Refresh();
                }

                AssetDatabase.CreateAsset(newAudioReference, assetPath);
                newAudioReference.SetupVariables(is3D, isLooping, parameters, description, feedback, implementStatus);
                newAudioReference.UpdateName();

                Debug.Log($"<color=cyan>Created new AudioReference: \"{eventName}\"</color>");
                return newAudioReference;
            }
            catch (Exception e)
            {
                AssetDatabase.DeleteAsset(assetPath);
                Object.DestroyImmediate(newAudioReference);
                throw new Exception($"Error creating new AudioReference Asset. ERROR: {e.Message}");
            }
        }

        private static AudioReference[] GetAllAudioReferences()
        {
            string[] audioReferences = AssetDatabase.FindAssets("t:AudioReference");
            AudioReference[] audioReferencesArray = new AudioReference[audioReferences.Length];

            for (int i = 0; i < audioReferences.Length; i++)
            {
                var audioReference = AssetDatabase.LoadAssetAtPath<AudioReference>(AssetDatabase.GUIDToAssetPath(audioReferences[i]));
                audioReferencesArray[i] = audioReference;
                audioReference.SetupVariablesIfNeeded();
            }

            return audioReferencesArray;
        }
        
        private static void ClearAllSheetsRequest(string spreadsheetID, ref List<string> sheets)
        {
            List<string> ranges = new List<string>();
            for (int i = 0; i < sheets.Count; i++)
            {
                ranges.Add($"{sheets[i]}!{STANDARD_RANGE}");
            }

            UpdateProgressBar("Clearing Spreadsheet", 0);
            BatchClearValuesRequest requestBody = new BatchClearValuesRequest { Ranges = ranges };

            SpreadsheetsResource.ValuesResource.BatchClearRequest request = Service.Spreadsheets.Values.BatchClear(requestBody, spreadsheetID);
            BatchClearValuesResponse response = request.Execute();

            UpdateProgressBar("Clearing Spreadsheet", 1);

#if DEBUGGING
        Debug.Log($"Cleared Sheets: {JsonConvert.SerializeObject(response)}");
#endif
        }

        private static void CreateEntries(string spreadsheetID, ref AudioReference[] audioReferences)
        {
            Dictionary<string, int> categories = new Dictionary<string, int>();

            List<ValueRange> data = new List<ValueRange>();
            for (int i = 0; i < audioReferences.Length; i++)
            {
                audioReferences[i].UpdateName();

                // If category don't exist, create it
                if (!categories.ContainsKey(audioReferences[i].category))
                {
                    // indention starts at index 2 in the spreadsheet
                    categories.Add(audioReferences[i].category, 2);
                }
                else
                {
                    // add indention per entry
                    categories[audioReferences[i].category]++;
                }

                var objectList = new List<object>
                {
                    audioReferences[i].eventName,
                    audioReferences[i].is3D ? "3D" : "2D",
                    audioReferences[i].looping ? "Loop" : "OneShot",
                    audioReferences[i].parameters,
                    audioReferences[i].description,
                    audioReferences[i].feedback,
                    audioReferences[i].implementStatus.ToString()
                };

                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { objectList },
                    Range = $"{audioReferences[i].category}!A{categories[audioReferences[i].category]}"
                };
                data.Add(valueRange);
            }

            // Last Updated Text
            var updateText = new ValueRange
            {
                Values = new List<IList<object>> { new object[] { "Updated\n" + DateTime.Now.ToString("g", CultureInfo.InvariantCulture) } },
                Range = LAST_UPDATED_RANGE
            };
            
            data.Add(updateText);

            BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest { ValueInputOption = "USER_ENTERED", Data = data };

            SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = Service.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetID);
            request.Execute();

#if DEBUGGING
        Debug.Log($"Added {audioReferences.Length} audio refs: {JsonConvert.SerializeObject(requestBody)}");
#endif
        }

       
    }
}