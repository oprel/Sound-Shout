using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using Color = Google.Apis.Sheets.v4.Data.Color;

namespace SoundShout.Editor
{
    public static class SheetsFormatting
    {
        #region Header

        internal static void ApplyHeaderFormatting(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            // Auto resize all headers
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {AutoResizeDimensions = new AutoResizeDimensionsRequest
            {
                Dimensions = new DimensionRange
                {
                    SheetId = sheetID,
                    Dimension = "COLUMNS"
                }
            }});

            //create the update request for cells from the first row
            var repeatCell = new RepeatCellRequest
            {
                Range = GetHeaderGridRange(sheetID),
                Cell = new CellData
                {
                    UserEnteredFormat = GetHeaderCellFormat()
                },
                Fields = "UserEnteredFormat(BackgroundColor,TextFormat,HorizontalAlignment)"
            };
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {RepeatCell = repeatCell});
        }

        internal static ValueRange GetSetHeaderTextUpdateRequest(string sheetTabName)
        {
            var textPerCell = new List<object>
            {
                "Event Name",
                "Is 3D",
                "Looping?",
                "Parameters",
                "Description",
                "Feedback",
                "Status"
            };
            
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>> {textPerCell},
                Range = $"{sheetTabName}!A1"
            };

            
            return valueRange;
        }

        private static CellFormat GetHeaderCellFormat()
        {
            return new CellFormat{
                BackgroundColor = new Color
                {
                    Blue = 1,
                    Red = 1,
                    Green = 1,
                    Alpha = 0
                },
                TextFormat = new TextFormat
                {
                    Bold = true,
                    FontSize = 14
                },
                HorizontalAlignment = "Center"
            };
        }
        
        private static GridRange GetHeaderGridRange(int sheetId)
        {
            return new GridRange
            {
                SheetId = sheetId,
                EndRowIndex = 1
                //StartRowIndex = 0,
                //StartColumnIndex = 0, // Leaving these out will make the whole row bolded!
                //EndColumnIndex = 6,
            };
        }

        #endregion
        
        #region Rows

        internal static void AddEmptyConditionalFormattingRequests(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            foreach (AudioReference.ImplementationStatus enumValue in Enum.GetValues(typeof(AudioReference.ImplementationStatus)))
            {
                batchUpdateSpreadsheetRequest.Requests.Add( new Request { AddConditionalFormatRule = new AddConditionalFormatRuleRequest { Rule = GetConditionFormatRule(sheetID, enumValue, new Color()) } });
            }
        }
        
        public static void ApplyRowFormatting(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetImplementationStatusValidationRequest(sheetID)});
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetIs3DValidationRequest(sheetID)});
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetIsLoopingValidationRequest(sheetID)});
            
            UpdateImplementationStatusConditionalFormatting(ref batchUpdateSpreadsheetRequest, sheetID);
        }

        private static SetDataValidationRequest GetImplementationStatusValidationRequest(int sheetID)
        {
            var statusValidation = new SetDataValidationRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetID,
                    StartRowIndex = 1,
                    StartColumnIndex = 6,
                    EndColumnIndex = 7
                },
                Rule = new DataValidationRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "ONE_OF_LIST",
                        Values = new List<ConditionValue>()
                    },
                    Strict = true,
                    ShowCustomUi = true
                }
            };

            // Dynamically fill the condition with values
            var enumNames = GetStatusEnumNameArray();
            foreach (var enumValue in enumNames)
            {
                statusValidation.Rule.Condition.Values.Add(new ConditionValue {UserEnteredValue = enumValue});
            }

            return statusValidation;
        }
        
        private static SetDataValidationRequest GetIs3DValidationRequest(int sheetID)
        {
            var statusValidation = new SetDataValidationRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetID,
                    StartRowIndex = 1,
                    StartColumnIndex = 1,
                    EndColumnIndex = 2
                },
                Rule = new DataValidationRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "ONE_OF_LIST",
                        Values = new List<ConditionValue>
                        {
                            new ConditionValue {UserEnteredValue = "2D"},
                            new ConditionValue {UserEnteredValue = "3D"},
                        }
                    },
                    Strict = true,
                    ShowCustomUi = true
                }
            };

            return statusValidation;
        }

        private static SetDataValidationRequest GetIsLoopingValidationRequest(int sheetID)
        {
            var statusValidation = new SetDataValidationRequest
            {
                Range = new GridRange
                {
                    SheetId = sheetID,
                    StartRowIndex = 1,
                    StartColumnIndex = 2,
                    EndColumnIndex = 3
                },
                Rule = new DataValidationRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "ONE_OF_LIST",
                        Values = new List<ConditionValue>
                        {
                            new ConditionValue {UserEnteredValue = "OneShot"},
                            new ConditionValue {UserEnteredValue = "Loop"},
                        }
                    },
                    Strict = true,
                    ShowCustomUi = true
                }
            };

            return statusValidation;
        }

        private static void UpdateImplementationStatusConditionalFormatting(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            batchUpdateSpreadsheetRequest.Requests.Add( new Request
            {
                UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, AudioReference.ImplementationStatus.Delete, new Color
                {
                    Red =   (float)255/255,
                    Green = 0,
                    Blue =  0
                })
            });
            
            batchUpdateSpreadsheetRequest.Requests.Add( new Request
            {
                UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, AudioReference.ImplementationStatus.TODO, new Color
                {
                    Red =   (float)162/255,
                    Green = (float)210/255,
                    Blue =  (float)234/255
                })
            });
            
            batchUpdateSpreadsheetRequest.Requests.Add( new Request
            {
                UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, AudioReference.ImplementationStatus.Created, new Color
                {
                    Red =   (float)255/255,
                    Green = (float)211/255,
                    Blue =  (float)245/255
                })
            });
            
            batchUpdateSpreadsheetRequest.Requests.Add( new Request
            {
                UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, AudioReference.ImplementationStatus.Implemented, new Color
                {
                    Red =   (float)255/255,
                    Green = (float)217/255,
                    Blue =  (float)102/255
                })
            });
            
            batchUpdateSpreadsheetRequest.Requests.Add( new Request
            {
                UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, AudioReference.ImplementationStatus.Feedback, new Color
                {
                    Red =   (float)249/255,
                    Green = (float)237/255,
                    Blue =  (float)174/255
                })
            });
            
            batchUpdateSpreadsheetRequest.Requests.Add( new Request
            {
                UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, AudioReference.ImplementationStatus.Iterate, new Color
                {
                    Red =   (float)180/255,
                    Green = (float)167/255,
                    Blue =  (float)214/255
                })
            });
            
            batchUpdateSpreadsheetRequest.Requests.Add( new Request
            {
                UpdateConditionalFormatRule = CreateConditionalFormat(sheetID, AudioReference.ImplementationStatus.Done, new Color
                {
                    Red =   (float)88/255,
                    Green = (float)239/255,
                    Blue =  (float)160/255
                })
            });
        }

        private static UpdateConditionalFormatRuleRequest CreateConditionalFormat(int sheetID, AudioReference.ImplementationStatus status, Color rowColor)
        {
            return new UpdateConditionalFormatRuleRequest
            {
                Index = (int)status,
                Rule = GetConditionFormatRule(sheetID, status, rowColor),
            };
        }

        private static ConditionalFormatRule GetConditionFormatRule(int sheetID, AudioReference.ImplementationStatus status, Color rowColor)
        {
            return new ConditionalFormatRule
            {
                Ranges = new List<GridRange>
                {
                    GetRowGridRange(sheetID)
                },
                BooleanRule = new BooleanRule
                {
                    Condition = new BooleanCondition
                    {
                        Type = "CUSTOM_FORMULA",
                        Values = new List<ConditionValue>
                        {
                            new ConditionValue
                            {
                                UserEnteredValue = $"=$G2=\"{status.ToString()}\""
                            }
                        }
                    },
                    Format = new CellFormat
                    {
                        BackgroundColor = rowColor
                    }
                }
            };
        }

        private static GridRange GetRowGridRange(int sheetId)
        {
            return new GridRange
            {
                SheetId = sheetId,
                StartColumnIndex = 0,
                StartRowIndex = 1
            };
        }
        
        private static string[] GetStatusEnumNameArray()
        {
            return Enum.GetNames(typeof(AudioReference.ImplementationStatus));
        }
        
        #endregion
    }
}