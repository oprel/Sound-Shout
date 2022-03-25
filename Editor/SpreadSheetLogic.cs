﻿//#define DEBUGGING

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

namespace SoundShout.Editor
{
    public static class SpreadSheetLogic
    {
        private static SheetsService service;
        private static SheetsService Service => service ?? (service = GetSheetsService());

        private enum UsedRows { EventName = 0, Is3D = 1, Looping = 2, Parameters = 3, Description = 4, Feedback = 5, ImplementStatus = 6 }
        private static readonly string[] scopes = { SheetsService.Scope.Spreadsheets };
        private const string OVERVIEW_TAB = "~Overview";
        private const string LAST_UPDATED_RANGE = OVERVIEW_TAB + "!H1";
        private const string START_RANGE = "A2";
        private const string END_RANGE = "G";
        private const string STANDARD_RANGE = START_RANGE + ":" + END_RANGE;
        
        private static int totalOperations, currentOperation;

        private static Spreadsheet GetSheetData(string spreadSheetUrl)
        {
            return Service.Spreadsheets.Get(spreadSheetUrl).Execute();
        }

        private static List<string> GetSpreadsheetTabsList(Spreadsheet ss)
        {
            List<string> sheetTabs = new List<string>();
            foreach (Sheet sheet in ss.Sheets)
            {
                if (sheet.Properties.Title == OVERVIEW_TAB)
                {
                    continue;
                }

                sheetTabs.Add(sheet.Properties.Title);
            }

            return sheetTabs;
        }
        
        private static SheetsService GetSheetsService()
        {
            GoogleCredential credential;
            const string secretsPath = SoundShoutWindow.CLIENT_SECRET_PATH;
            using (var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(scopes);
            }

            return new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = SoundShoutWindow.APPLICATION_NAME,
            });
        }

        public static void FetchSpreadsheetChanges(string spreadSheetURL)
        {
            try
            {
                var data = GetSheetData(spreadSheetURL);
                var sheetTabs = GetSpreadsheetTabsList(data);
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
            UploadLocalAudioReferenceChanges(spreadSheetURL, ref allAudioReferences);
        }

        public static void UpdateAudioSpreadSheet(string spreadSheetURL)
        {
            try
            {
                // Progress bar
                totalOperations = 5; // Number of methods, used to calculate percentage for 

                currentOperation = 0;
                var audioRefs = GetAllAudioReferences();

                currentOperation++;
                FetchSpreadsheetChanges(spreadSheetURL);

                currentOperation++;
                ClearAllSheetsRequest(spreadSheetURL);

                currentOperation++;
                UploadLocalAudioReferenceChanges(spreadSheetURL, ref audioRefs);

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
            foreach (var audioReference in audioReferences)
            {
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
            for (int sheetIndex = 1; sheetIndex < sheets.Count; sheetIndex++)
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

                        AudioReference.ImplementationStatus implementImplementationStatus = (AudioReference.ImplementationStatus)Enum.Parse(typeof(AudioReference.ImplementationStatus), (string)row[(int)UsedRows.ImplementStatus]);

                        bool newAudioReference = true;
                        string fullEventName = $"event:/{eventName}";
                        foreach (var audioRef in audioReferences)
                        {
                            if (audioRef.fullEventPath == fullEventName)
                            {
                                audioRef.ApplyChanges(is3D, isLooping, parameters, description, feedback, implementImplementationStatus);
                                newAudioReference = false;
                                break;
                            }
                        }

                        if (implementImplementationStatus == AudioReference.ImplementationStatus.Delete)
                        {
                            Debug.Log($"Skipped creating audio reference for \"{eventName}\" as it's marked as Delete!");
                        }
                        else if (newAudioReference)
                        {
                            var newSound = CreateNewAudioReferenceAsset(eventName, is3D, isLooping, parameters, description, feedback, implementImplementationStatus);
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

        private static AudioReference CreateNewAudioReferenceAsset(string eventName, bool is3D, bool isLooping, string parameters, string description, string feedback, AudioReference.ImplementationStatus implementImplementationStatus)
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
                newAudioReference.SetupVariables(is3D, isLooping, parameters, description, feedback, implementImplementationStatus);
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

        private static void UploadLocalAudioReferenceChanges(string spreadsheetURL, ref AudioReference[] audioReferences)
        {
            Dictionary<string, int> categories = new Dictionary<string, int>();
            
            List<ValueRange> data = new List<ValueRange>();
            foreach (var audioRef in audioReferences)
            {
                audioRef.UpdateName();
            
                // If category don't exist, create it
                if (!categories.ContainsKey(audioRef.category))
                {
                    // indention starts at index 2 in the spreadsheet
                    categories.Add(audioRef.category, 2);
                }
                else
                {
                    // add indention per entry
                    categories[audioRef.category]++;
                }
            
                var objectList = new List<object>
                {
                    audioRef.eventName,
                    audioRef.is3D ? "3D" : "2D",
                    audioRef.looping ? "Loop" : "OneShot",
                    audioRef.parameters,
                    audioRef.description,
                    audioRef.feedback,
                    audioRef.implementImplementationStatus.ToString()
                };
            
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { objectList },
                    Range = $"{audioRef.category}!A{categories[audioRef.category]}"
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


            CreateMissingSheetTabs(spreadsheetURL, categories);

            BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest { ValueInputOption = "USER_ENTERED", Data = data };
            
            SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = Service.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetURL);
            request.Execute();

#if DEBUGGING
        Debug.Log($"Added {audioReferences.Length} audio refs: {JsonConvert.SerializeObject(requestBody)}");
#endif
        }

        private static void ClearAllSheetsRequest(string spreadsheetUrl)
        {
            var data = GetSheetData(spreadsheetUrl);
            var sheets = GetSpreadsheetTabsList(data);
            List<string> ranges = new List<string>();
            foreach (var sheetTab in sheets)
            {
                ranges.Add($"{sheetTab}!{STANDARD_RANGE}");
            }

            BatchClearValuesRequest requestBody = new BatchClearValuesRequest { Ranges = ranges };

            SpreadsheetsResource.ValuesResource.BatchClearRequest request = Service.Spreadsheets.Values.BatchClear(requestBody, spreadsheetUrl);
            request.Execute();
        }

        private static void CreateMissingSheetTabs(string spreadsheetURL, Dictionary<string, int> categories)
        {
            var data = GetSheetData(spreadsheetURL);
            var existingTabs = GetSpreadsheetTabsList(data);

            BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>()
            };

            bool addedNewTabs = false;
            foreach (var category in categories)
            {
                // Don't duplicate existing tabs
                if (existingTabs.Contains(category.Key))
                    continue;

                var addSheetRequest = new AddSheetRequest
                {
                    Properties = new SheetProperties
                    {
                        Title = category.Key,
                    }
                };

                batchUpdateSpreadsheetRequest.Requests.Add(new Request
                {
                    AddSheet = addSheetRequest
                });
            }

            if (batchUpdateSpreadsheetRequest.Requests.Count > 0)
            {
                var batchUpdateRequest =
                    Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetURL);
                batchUpdateRequest.Execute();
                addedNewTabs = true;
            }

            ApplyFormattingToTopRows(spreadsheetURL);
            if (addedNewTabs)
            {
            }
        }

        public static void ApplyFormattingToTopRows(string spreadsheetURL)
        {
            var batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>()
            };

            var data = GetSheetData(spreadsheetURL);
            var headerTextValueRanges = new List<ValueRange>();

            foreach (var sheet in data.Sheets)
            {
                string tabTitle = sheet.Properties.Title;
                if (tabTitle == "~Overview")
                    continue;

                // ReSharper disable once PossibleInvalidOperationException
                int sheetID = (int) sheet.Properties.SheetId;
                SheetsFormatting.ApplyHeaderFormatting(ref batchUpdateSpreadsheetRequest, sheetID);

                headerTextValueRanges.Add(SheetsFormatting.GetSetHeaderTextUpdateRequest(tabTitle));
            }

            // Apply formatting
            if (batchUpdateSpreadsheetRequest.Requests.Count > 0)
            {
                var batchUpdateRequest = Service.Spreadsheets.BatchUpdate(batchUpdateSpreadsheetRequest, spreadsheetURL); 
                batchUpdateRequest.Execute();
            }

            // Apply header text
            if (headerTextValueRanges.Count > 0)
            {
                BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest { ValueInputOption = "USER_ENTERED", Data = headerTextValueRanges };
                SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = Service.Spreadsheets.Values.BatchUpdate(requestBody, spreadsheetURL);
                request.Execute();
            }
        }
    }
}