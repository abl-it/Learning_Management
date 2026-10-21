using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Training.Services
{
    /// <summary>
    /// A data row read from the participant Excel file.
    /// </summary>
    /// <param name="RowNumber">Excel row number (1-based, header is row 1).</param>
    /// <param name="EmployeeCode">Trimmed employee code; empty when the cell is blank.</param>
    /// <param name="HasOtherData">True when the code is blank but other cells in the row contain data.</param>
    internal sealed record ParticipantExcelRow(int RowNumber, string EmployeeCode, bool HasOtherData);

    /// <summary>
    /// Reads the "EmployeeCode" column of a participant Excel file (.xlsx) using Open XML.
    /// </summary>
    /// <remarks>
    /// Same conventions as the existing Excel imports: columns are mapped through the
    /// <c>CellReference</c> attribute (never by position, because Excel omits empty cells),
    /// and both SharedString and InlineString text cells are supported.
    /// </remarks>
    internal static class ParticipantExcelReader
    {
        /// <summary>Maximum number of non-empty data rows accepted per file.</summary>
        public const int MaxRows = 500;

        private const string PreferredSheetName = "Participants";

        /// <summary>
        /// Reads the employee codes from the workbook.
        /// </summary>
        /// <param name="stream">A seekable stream containing the .xlsx file.</param>
        /// <returns>Non-empty data rows in file order.</returns>
        /// <exception cref="ArgumentException">The file layout is not usable (user-fixable).</exception>
        public static List<ParticipantExcelRow> ReadEmployeeCodes(Stream stream)
        {
            using var document = SpreadsheetDocument.Open(stream, false);

            var workbookPart = document.WorkbookPart
                ?? throw new ArgumentException("The Excel file is not valid (workbook was not found).");

            var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>().ToList()
                ?? new List<Sheet>();

            // Prefer the sheet named "Participants"; otherwise use the first sheet.
            var sheet = sheets.FirstOrDefault(s =>
                    string.Equals(s.Name?.Value, PreferredSheetName, StringComparison.OrdinalIgnoreCase))
                ?? sheets.FirstOrDefault()
                ?? throw new ArgumentException("The Excel file does not contain any sheet.");

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);

            var rows = worksheetPart.Worksheet
                .Elements<SheetData>()
                .FirstOrDefault()
                ?.Elements<Row>()
                .ToList() ?? new List<Row>();

            if (rows.Count == 0)
            {
                throw new ArgumentException("The Excel file is empty.");
            }

            var sharedStrings = LoadSharedStrings(workbookPart);

            // ---- Header row: locate the EmployeeCode column --------------------------
            var headerRow = rows[0];
            var codeColumn = -1;

            foreach (var cell in headerRow.Elements<Cell>())
            {
                var text = GetCellText(cell, sharedStrings);

                if (NormalizeHeader(text) == "employeecode")
                {
                    codeColumn = GetColumnIndex(cell.CellReference?.Value);
                    break;
                }
            }

            if (codeColumn < 0)
            {
                throw new ArgumentException(
                    "Column 'EmployeeCode' was not found in the header row (row 1). " +
                    "Please use the provided template.");
            }

            // ---- Data rows -----------------------------------------------------------
            var result = new List<ParticipantExcelRow>();

            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = (int)(row.RowIndex?.Value ?? (uint)(i + 1));

                var code = string.Empty;
                var hasOtherData = false;

                foreach (var cell in row.Elements<Cell>())
                {
                    var column = GetColumnIndex(cell.CellReference?.Value);
                    var text = GetCellText(cell, sharedStrings);

                    if (column == codeColumn)
                    {
                        code = text;
                    }
                    else if (!string.IsNullOrWhiteSpace(text))
                    {
                        hasOtherData = true;
                    }
                }

                if (string.IsNullOrWhiteSpace(code) && !hasOtherData)
                {
                    continue; // blank row
                }

                result.Add(new ParticipantExcelRow(rowNumber, code, hasOtherData));
            }

            if (result.Count > MaxRows)
            {
                throw new ArgumentException(
                    $"The file contains {result.Count} rows. The maximum per upload is {MaxRows}.");
            }

            return result;
        }

        private static List<string> LoadSharedStrings(WorkbookPart workbookPart)
        {
            return workbookPart.SharedStringTablePart?.SharedStringTable
                       .Elements<SharedStringItem>()
                       .Select(item => item.InnerText)
                       .ToList()
                   ?? new List<string>();
        }

        /// <summary>
        /// Gets the cell text for SharedString (Excel), InlineString (openpyxl / LibreOffice),
        /// and plain numeric / formula-string cells.
        /// </summary>
        private static string GetCellText(Cell cell, List<string> sharedStrings)
        {
            string text;

            if (cell.DataType?.Value == CellValues.SharedString)
            {
                text = int.TryParse(cell.CellValue?.Text, out var index)
                       && index >= 0
                       && index < sharedStrings.Count
                    ? sharedStrings[index]
                    : string.Empty;
            }
            else if (cell.DataType?.Value == CellValues.InlineString)
            {
                text = cell.InlineString?.Text?.Text
                       ?? cell.InlineString?.InnerText
                       ?? string.Empty;
            }
            else
            {
                text = cell.CellValue?.Text ?? string.Empty;
            }

            return text.Trim();
        }

        private static string NormalizeHeader(string value)
        {
            return new string(value
                    .Where(c => !char.IsWhiteSpace(c) && c != '_' && c != '-')
                    .ToArray())
                .ToLowerInvariant();
        }

        private static int GetColumnIndex(string? cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
            {
                return -1;
            }

            var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());

            if (letters.Length == 0)
            {
                return -1;
            }

            var index = 0;

            foreach (var c in letters.ToUpperInvariant())
            {
                index = (index * 26) + (c - 'A' + 1);
            }

            return index - 1;
        }
    }
}
