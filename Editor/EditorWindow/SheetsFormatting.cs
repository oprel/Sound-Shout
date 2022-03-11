using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace SoundShout.Editor
{
    public static class SheetsFormatting
    {
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
            batchUpdateSpreadsheetRequest.Requests.Add( new Request {UpdateSheetProperties = freezeTopRowRequest});

            
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


            batchUpdateSpreadsheetRequest.Requests.Add( new Request {SetDataValidation = GetStatusValidationRequest(sheetID)});
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
            foreach (var enumValue in GetStatusEnumValues())
            {
                statusValidation.Rule.Condition.Values.Add(new ConditionValue {UserEnteredValue = enumValue});
            }

            return statusValidation;
        }
        
        private static IEnumerable<string> GetStatusEnumValues()
        {
            return Enum.GetNames(typeof(AudioReference.Status));
        }

        private static CellFormat GetHeaderCellFormat()
        {
            return new CellFormat{
                BackgroundColor = new Color
                {
                    Blue = 0,
                    Red = 0,
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
    }
}