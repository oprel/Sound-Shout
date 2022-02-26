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

namespace AudioReferenceEditor
{
    public static class AudioReferenceExporter
    {
        private enum UsedRows
        {
            EventName = 0,
            Is3D = 1,
            Looping = 2,
            Parameters = 3,
            Description = 4,
            Feedback = 5,
            ImplementStatus = 6
        }

        private static SheetsService service;
        private static SheetsService Service
        {
            get
            {
                if (service == null)
                {
                    SetupCredentials();
                }
                return service;
            }
        }
        
        private static readonly string[] scopes = { SheetsService.Scope.Spreadsheets };

        private const string LAST_UPDATED_RANGE = "~Overview!I1";
        private const string START_RANGE = "A2";
        private const string END_RANGE = "G";
        private const string STANDARD_RANGE = START_RANGE + ":" + END_RANGE;

        // Progress bar things
        private static int totalOperations, currentOperation;

        

        public static void FetchSpreadSheetChanges(string spreadSheetURL)
        {
            var audioRefs = FindAllAudioReferences(out var tabCategories);
            tabCategories.Add("Generic");
            ReadEntries(spreadSheetURL, ref audioRefs, ref tabCategories);
        }
        
        public static void UpdateAudioSpreadSheet(string spreadSheetURL)
        {
            try
            {
                // Progress bar
                totalOperations = 5; // Number of methods, used to calculate percentage for 

                currentOperation = 0;
                var audioRefs = FindAllAudioReferences(out var tabCategories);

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

        private static void SetupCredentials()
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

        private static void UpdateProgressBar(string message, float progress)
        {
            EditorUtility.DisplayProgressBar("Updating AudioReferences", message, (progress + currentOperation) / totalOperations);
        }

        private static AudioReference[] FindAllAudioReferences(out List<string> tabCategories)
        {
            string[] audioReferences = AssetDatabase.FindAssets("t:AudioReference");
            AudioReference[] audioReferencesArray = new AudioReference[audioReferences.Length];
            tabCategories = new List<string>();

            for (int i = 0; i < audioReferences.Length; i++)
            {
                var audioReference = AssetDatabase.LoadAssetAtPath<AudioReference>(AssetDatabase.GUIDToAssetPath(audioReferences[i]));
                audioReferencesArray[i] = audioReference;

                if (string.IsNullOrEmpty(audioReference.fullEventPath))
                {
                    Debug.LogError($"AudioReference \"{audioReference.name}\" didn't have it's FMOD setup. Fixing that, make sure its correct.", audioReference);
                    audioReference.UpdateName();
                }

                if (!tabCategories.Contains(audioReference.category))
                {
                    tabCategories.Add(audioReference.category);
                }

                UpdateProgressBar("Finding all audio references", (float)i / audioReferences.Length);
            }

            return audioReferencesArray;
        }


        private static void ReadEntries(string spreadsheetID, ref AudioReference[] audioReferences, ref List<string> sheets)
        {
            List<AudioReference> newAudioRefsList = new List<AudioReference>(10);

            // Loop through all tabs inside the
            for (int i = 0; i < sheets.Count; i++)
            {
                var range = $"{sheets[i]}!{STANDARD_RANGE}";
                var request = Service.Spreadsheets.Values.Get(spreadsheetID, range);

                ValueRange response = request.Execute();
                IList<IList<object>> values = response.Values;
                if (values != null && values.Count > 0)
                {
                    // Go through each row and their data
                    foreach (var row in values)
                    {
                        string eventName = (string)row[(int)UsedRows.EventName];
                        bool is3D = (string)row[(int)UsedRows.Is3D] == "3D";
                        bool isLooping = (string)row[(int)UsedRows.Looping] == "Loop";
                        string parameters = (string)row[(int)UsedRows.Parameters];
                        string description = (string)row[(int)UsedRows.Description];
                        string feedback = (string)row[(int)UsedRows.Feedback];

                        AudioReference.Status implementStatus = (AudioReference.Status)Enum.Parse(typeof(AudioReference.Status), (string)row[(int)UsedRows.ImplementStatus]);

                        UpdateProgressBar($"Updating AudioReference: {eventName}", (float)i / values.Count);

                        bool foundReference = false;
                        for (int j = 0; j < audioReferences.Length; j++)
                        {
                            if ("event:/" + eventName == audioReferences[j].fullEventPath)
                            {
                                audioReferences[j].ApplyChanges(is3D, isLooping, parameters, description, feedback, implementStatus);
                                foundReference = true;
                                break;
                            }
                        }

                        // If false means that the AudioReference we're looking for has only been created inside the spreadsheet
                        // Rumsklang probably created a cool sound and want it implemented.
                        if (implementStatus == AudioReference.Status.Delete)
                        {
                            Debug.Log($"Skipped creating audio reference for \"{eventName}\" as it's marked as Delete!");
                        }
                        else if (!foundReference)
                        {
                            // Let's say the one we need to create has a eventName value of "Harbor/CoolNPC/Elias_Talk"
                            string assetPath = "Assets/Audio/" + eventName + ".asset"; // Assets/Audio/Harbor/CoolNPC/Elias_Talk.asset

                            int lastSlashIndex = assetPath.LastIndexOf('/');
                            string fullAssetFolderPath = assetPath.Substring(0, lastSlashIndex); // Assets/Audio/Harbor/CoolNPC

                            // Get the folder where we should put the file
                            lastSlashIndex = fullAssetFolderPath.LastIndexOf('/');
                            string folderToCreate = fullAssetFolderPath.Substring(lastSlashIndex + 1); // Only the folder part "CoolNPC"
                            fullAssetFolderPath = assetPath.Substring(0, lastSlashIndex);

                            // Create folder if it doesn't exist
                            if (!AssetDatabase.IsValidFolder($"{fullAssetFolderPath}/{folderToCreate}"))
                            {
                                Debug.Log($"Creating folder: \"{folderToCreate}\" - fullAssetFolderPath: \"{fullAssetFolderPath}\"");
                                AssetDatabase.CreateFolder(fullAssetFolderPath, folderToCreate);
                            }

                            // // Create a new AudioReference and place it in it's correct folder
                            AudioReference newAudioReference = ScriptableObject.CreateInstance<AudioReference>();
                            Undo.RecordObject(newAudioReference, "Created new AudioReference");

                            // Setup all variables 
                            newAudioReference.SetupVariables(is3D, isLooping, parameters, description, feedback, implementStatus);
                            AssetDatabase.CreateAsset(newAudioReference, assetPath);

                            newAudioReference.UpdateName(); // Make sure the asset has correct FmodName set to it

                            newAudioRefsList.Add(newAudioReference);

                            Debug.Log($"<color=cyan>AudioReferenceExporter: Created new AudioReference from spreadsheet: {eventName}</color>");
                        }

#if DEBUGGING
                    Debug.Log($"Name: \"{eventName}\" " + $"3D: {is3D} - " + $"Loop: {isLooping} - " + $"Description: {description}" + $"Status: {implementStatus.ToString()}");
#endif
                    }
                }
                else
                {
                    Debug.Log($"No data was found in tab: \"{sheets[i]}\"");
                }
            }

            // Add potential new audio refs to AudioRef array by resizing the array 
            
            int currentSize = audioReferences.Length;
            int newSize = currentSize + newAudioRefsList.Count; 
            if (currentSize < newSize)
            {
                newAudioRefsList.AddRange(audioReferences);
                audioReferences = newAudioRefsList.ToArray();
            }
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
            // Check if fmod event exists
            //RuntimeManager.StudioSystem.getEvent(id, out desc);

            Dictionary<string, int> categories = new Dictionary<string, int>();

            // Go though all audio refs and add them as their own value range
            // It's like a class with values.
            // Doing this adds all values in one go instead of doing multiple individual requests
            List<ValueRange> data = new List<ValueRange>();
            for (int i = 0; i < audioReferences.Length; i++)
            {
                UpdateProgressBar($"Uploading AudioReference: {audioReferences[i]}", (float)i / audioReferences.Length);

                // Update the audio reference's info
                // To make sure it's correct before uploading
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
                    Range = $"{audioReferences[i].category}!A" + categories[audioReferences[i].category],
                };
                data.Add(valueRange);
            }

            // Add "Updated" text to spreadsheet
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