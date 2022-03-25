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
            var freezeTopRowRequest = new UpdateSheetPropertiesRequest
            {
                Properties = new SheetProperties
                {
                    SheetId = sheetID,
                    GridProperties = new GridProperties
                    {
                        FrozenRowCount = 1
                    }
                },
                Fields = "gridProperties.frozenRowCount"
            };
            batchUpdateSpreadsheetRequest.Requests.Add(new Request {UpdateSheetProperties = freezeTopRowRequest});

            // Delete columns that are not used!
            batchUpdateSpreadsheetRequest.Requests.Add(new Request
            {
                DeleteDimension = new DeleteDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetID,
                        Dimension = "COLUMNS",
                        StartIndex = 7
                    }
                }
            });

            // Auto resize all headers
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {AutoResizeDimensions = new AutoResizeDimensionsRequest
            {
                Dimensions = new DimensionRange
                {
                    SheetId = sheetID,
                    Dimension = "COLUMNS",
                }
            }});

            //create the update request for cells from the first row
            var repeatCell = new RepeatCellRequest
            {
                Range = GetHeaderGridRange(sheetID),
                Cell = new CellData
                {
                    UserEnteredFormat = GetHeaderCellFormat(),
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
                EndRowIndex = 1,
                //StartRowIndex = 0,
                //StartColumnIndex = 0, // Leaving these out will make the whole row bolded!
                //EndColumnIndex = 6,
            };
        }

        #endregion
        
        #region Rows

        public static void ApplyRowFormatting(ref BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest, int sheetID)
        {
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetStatusValidationRequest(sheetID)});
            
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {UpdateConditionalFormatRule = GetEnumConditionalFormatting(sheetID)});
        }

        private static UpdateConditionalFormatRuleRequest GetEnumConditionalFormatting(int sheetID)
        {
            return new UpdateConditionalFormatRuleRequest
            {
                Rule = new ConditionalFormatRule
                {
                    Ranges = new List<GridRange>
                    {
                        GetRowGridRange(sheetID),
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
                                    UserEnteredValue = "=$G2=\"TODO\""
                                }
                            },
                        },
                        Format = GetRowEnumFormat()
                    },
                }
            };
        }

        private static CellFormat GetRowEnumFormat()
        {
            return new CellFormat{
                BackgroundColor = new Color()
                {
                    Red =   (float)162/255,
                    Green = (float)210/255,
                    Blue =  (float)234/255,
                    Alpha = 0
                },
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
        
        private static SetDataValidationRequest GetStatusValidationRequest(int sheetID)
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
                    ShowCustomUi = true,
                }
            };

            // Dynamically fill the condition with values
            var enumNames = Enum.GetNames(typeof(AudioReference.ImplementationStatus));
            foreach (var enumValue in enumNames)
            {
                statusValidation.Rule.Condition.Values.Add(new ConditionValue {UserEnteredValue = enumValue});
            }

            return statusValidation;
        }

        #endregion
    }
}