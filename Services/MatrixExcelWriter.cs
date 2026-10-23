using System.Drawing;
using System.Globalization;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Training.Models.DTO.TrainingMatrix;

namespace Training.Services
{
    /// <summary>
    /// Membuat file .xlsx (satu sheet) berisi Training Matrix per Sub Department (EPPlus).
    /// Lisensi EPPlus diasumsikan sudah diatur di startup aplikasi (sama seperti fitur Import/Report yang ada).
    /// </summary>
    internal static class MatrixExcelWriter
    {
        private const int ColumnCount = 16;
        private const int HeaderRow = 5;

        /// <summary>Membuat isi file .xlsx.</summary>
        public static byte[] Build(MatrixExportDto data)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Training Matrix");

            // ---- judul & keterangan -------------------------------------------------
            ws.Cells[1, 1].Value = data.Title;
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;

            ws.Cells[2, 1].Value =
                "Mandatory = wajib, Recommended = disarankan. Layer berdasarkan JobGrade (1-3; Manager Operasional = Layer 3). Hanya matrix Approved yang diisi. Terpenuhi = PRESENT pada event Complete; bila belum, ditampilkan event terbaru (bila ada).";
            ws.Cells[2, 1].Style.Font.Color.SetColor(Rgb("#6C757D"));

            ws.Cells[3, 1].Value = $"Diekspor {data.GeneratedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}";
            ws.Cells[3, 1].Style.Font.Color.SetColor(Rgb("#6C757D"));

            // ---- header tabel ----------------------------------------------------------
            var headers = new[]
            {
                "Department", "Sub Department", "Kode Course", "Nama Course",
                "Layer 1", "Layer 2", "Layer 3",
                "Kode Karyawan", "Nama Karyawan", "Layer", "Requirement",
                "Status", "Event Code", "Tanggal Event", "Status Event", "Kehadiran"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cells[HeaderRow, i + 1].Value = headers[i];
            }

            var header = ws.Cells[HeaderRow, 1, HeaderRow, ColumnCount];
            header.Style.Font.Bold = true;
            header.Style.Font.Color.SetColor(Color.White);
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(Rgb("#495057"));
            header.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            ws.Cells[HeaderRow, 5, HeaderRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[HeaderRow, 10, HeaderRow, 12].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[HeaderRow, 14, HeaderRow, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // ---- isi ---------------------------------------------------------------------
            var row = HeaderRow + 1;

            foreach (var dept in data.Departments)
            {
                if (!dept.IsApproved)
                {
                    WriteBand(
                        ws, row,
                        $"{dept.Name} - matrix {dept.MatrixStatus.ToLowerInvariant()} (belum Approved, belum ada acuan course)",
                        "#FFF3CD");
                    row++;
                    continue;
                }

                foreach (var sub in dept.SubDepartments)
                {
                    WriteBand(
                        ws, row,
                        $"{dept.Name}  |  {sub.Name}  |  Karyawan: Layer 1 = {sub.Layer1Employees}, Layer 2 = {sub.Layer2Employees}, Layer 3 = {sub.Layer3Employees}, Unmapped = {sub.UnmappedEmployees}",
                        "#DEE2E6");
                    row++;

                    if (sub.Courses.Count == 0)
                    {
                        ws.Cells[row, 1].Value = dept.Name;
                        ws.Cells[row, 2].Value = sub.Name;
                        ws.Cells[row, 4].Value = "(belum ada course pada matrix)";
                        ws.Cells[row, 4].Style.Font.Italic = true;
                        ws.Cells[row, 4].Style.Font.Color.SetColor(Rgb("#6C757D"));
                        row++;

                        row = WriteUnmapped(ws, row, dept, sub);
                        continue;
                    }

                    foreach (var course in sub.Courses)
                    {
                        if (course.Employees.Count == 0)
                        {
                            WriteCourse(ws, row, dept, sub, course);
                            ws.Cells[row, 8].Value = "(tidak ada karyawan pada Layer ini)";
                            ws.Cells[row, 8].Style.Font.Italic = true;
                            ws.Cells[row, 8].Style.Font.Color.SetColor(Rgb("#6C757D"));
                            row++;
                            continue;
                        }

                        foreach (var person in course.Employees)
                        {
                            WriteCourse(ws, row, dept, sub, course);
                            WriteEmployee(ws, row, person);
                            row++;
                        }
                    }

                    row = WriteUnmapped(ws, row, dept, sub);
                }
            }

            // ---- format ---------------------------------------------------------------------
            var last = Math.Max(row - 1, HeaderRow);
            var table = ws.Cells[HeaderRow, 1, last, ColumnCount];
            var border = Rgb("#ADB5BD");

            table.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Top.Color.SetColor(border);
            table.Style.Border.Bottom.Color.SetColor(border);
            table.Style.Border.Left.Color.SetColor(border);
            table.Style.Border.Right.Color.SetColor(border);

            ws.Column(1).Width = 30;
            ws.Column(2).Width = 34;
            ws.Column(3).Width = 14;
            ws.Column(4).Width = 50;
            ws.Column(5).Width = 14;
            ws.Column(6).Width = 14;
            ws.Column(7).Width = 14;
            ws.Column(8).Width = 14;
            ws.Column(9).Width = 30;
            ws.Column(10).Width = 8;
            ws.Column(11).Width = 14;
            ws.Column(12).Width = 18;
            ws.Column(13).Width = 14;
            ws.Column(14).Width = 15;
            ws.Column(15).Width = 14;
            ws.Column(16).Width = 12;

            ws.Cells[HeaderRow + 1, 10, last, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[HeaderRow + 1, 14, last, 16].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.View.FreezePanes(HeaderRow + 1, 1);

            BuildEmployeeSheet(package, data);

            return package.GetAsByteArray();
        }

        // =====================================================================
        // Sheet 2: perspektif karyawan (karyawan dahulu, baru course matrix-nya)
        // =====================================================================

        private const int EmployeeColumnCount = 13;

        private static void BuildEmployeeSheet(ExcelPackage package, MatrixExportDto data)
        {
            var ws = package.Workbook.Worksheets.Add("Per Employee");

            ws.Cells[1, 1].Value = $"{data.Title} - per Karyawan";
            ws.Cells[1, 1].Style.Font.Bold = true;
            ws.Cells[1, 1].Style.Font.Size = 14;

            ws.Cells[2, 1].Value =
                "Per karyawan: course yang harus dilakukan menurut matrix (Layer-nya) dan training aktualnya. Terpenuhi = PRESENT pada event Complete; bila belum, ditampilkan event terbaru (bila ada).";
            ws.Cells[2, 1].Style.Font.Color.SetColor(Rgb("#6C757D"));

            ws.Cells[3, 1].Value = $"Diekspor {data.GeneratedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}";
            ws.Cells[3, 1].Style.Font.Color.SetColor(Rgb("#6C757D"));

            var headers = new[]
            {
                "Department", "Sub Department", "Kode Karyawan", "Nama Karyawan", "Layer",
                "Kode Course", "Nama Course", "Requirement",
                "Status", "Event Code", "Tanggal Event", "Status Event", "Kehadiran"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cells[HeaderRow, i + 1].Value = headers[i];
            }

            var header = ws.Cells[HeaderRow, 1, HeaderRow, EmployeeColumnCount];
            header.Style.Font.Bold = true;
            header.Style.Font.Color.SetColor(Color.White);
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(Rgb("#495057"));
            header.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            var row = HeaderRow + 1;

            foreach (var dept in data.Departments)
            {
                if (!dept.IsApproved)
                {
                    WriteBand(
                        ws, row,
                        $"{dept.Name} - matrix {dept.MatrixStatus.ToLowerInvariant()} (belum Approved, belum ada acuan course)",
                        "#FFF3CD", EmployeeColumnCount);
                    row++;
                    continue;
                }

                foreach (var sub in dept.SubDepartments)
                {
                    WriteBand(ws, row, $"{dept.Name}  |  {sub.Name}", "#DEE2E6", EmployeeColumnCount);
                    row++;

                    // Course per karyawan (dari sisi course -> dibalik menjadi sisi karyawan)
                    var byPerson = sub.Courses
                        .SelectMany(c => c.Employees.Select(e => (Course: c, Person: e)))
                        .GroupBy(x => x.Person.EmployeeCode, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                    foreach (var person in sub.People.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        byPerson.TryGetValue(person.EmployeeCode, out var items);

                        var courses = (items ?? new())
                            .OrderBy(x => string.Equals(x.Person.Requirement, "Mandatory", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                            .ThenBy(x => x.Course.CourseCode, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var mandatory = courses.Where(x => IsMandatory(x.Person.Requirement)).ToList();
                        var recommended = courses.Where(x => !IsMandatory(x.Person.Requirement)).ToList();

                        WriteBand(
                            ws, row,
                            $"{person.EmployeeCode} - {person.Name}  |  Layer {person.Layer}  |  Mandatory terpenuhi {mandatory.Count(x => x.Person.Fulfilled)}/{mandatory.Count}  |  Recommended terpenuhi {recommended.Count(x => x.Person.Fulfilled)}/{recommended.Count}",
                            "#E9ECEF", EmployeeColumnCount);
                        row++;

                        if (courses.Count == 0)
                        {
                            WritePerson(ws, row, dept, sub, person.EmployeeCode, person.Name, person.Layer);
                            ws.Cells[row, 6].Value = "(tidak ada course pada matrix untuk Layer ini)";
                            ws.Cells[row, 6].Style.Font.Italic = true;
                            ws.Cells[row, 6].Style.Font.Color.SetColor(Rgb("#6C757D"));
                            row++;
                            continue;
                        }

                        foreach (var item in courses)
                        {
                            WritePerson(ws, row, dept, sub, person.EmployeeCode, person.Name, person.Layer);

                            ws.Cells[row, 6].Value = item.Course.CourseCode;
                            ws.Cells[row, 7].Value = item.Course.CourseName;
                            ws.Cells[row, 8].Value = item.Person.Requirement;
                            ws.Cells[row, 8].Style.Font.Bold = IsMandatory(item.Person.Requirement);

                            WriteStatus(ws.Cells[row, 9], item.Person.Fulfilled);

                            ws.Cells[row, 10].Value = item.Person.EventCode;

                            if (item.Person.EventDate is not null)
                            {
                                ws.Cells[row, 11].Value = item.Person.EventDate.Value.Date;
                                ws.Cells[row, 11].Style.Numberformat.Format = "dd/mm/yyyy";
                            }

                            ws.Cells[row, 12].Value = item.Person.EventStatus;
                            ws.Cells[row, 13].Value = item.Person.Attendance;

                            row++;
                        }
                    }

                    foreach (var person in sub.Unmapped)
                    {
                        WritePerson(ws, row, dept, sub, person.EmployeeCode, person.Name, null);

                        var status = ws.Cells[row, 9];
                        status.Value = $"Unmapped - {person.Reason}";
                        status.Style.Font.Color.SetColor(Rgb("#664D03"));
                        status.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        status.Style.Fill.BackgroundColor.SetColor(Rgb("#FFF3CD"));

                        row++;
                    }
                }
            }

            var last = Math.Max(row - 1, HeaderRow);
            var table = ws.Cells[HeaderRow, 1, last, EmployeeColumnCount];
            var border = Rgb("#ADB5BD");

            table.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            table.Style.Border.Top.Color.SetColor(border);
            table.Style.Border.Bottom.Color.SetColor(border);
            table.Style.Border.Left.Color.SetColor(border);
            table.Style.Border.Right.Color.SetColor(border);

            var widths = new double[] { 30, 34, 14, 30, 8, 14, 46, 14, 18, 14, 15, 14, 12 };

            for (var i = 0; i < widths.Length; i++)
            {
                ws.Column(i + 1).Width = widths[i];
            }

            ws.Cells[HeaderRow + 1, 5, last, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[HeaderRow + 1, 11, last, 13].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.View.FreezePanes(HeaderRow + 1, 1);
        }

        private static void WritePerson(
            ExcelWorksheet ws,
            int row,
            MatrixExportDepartmentDto dept,
            MatrixExportSubDepartmentDto sub,
            string employeeCode,
            string name,
            int? layer)
        {
            ws.Cells[row, 1].Value = dept.Name;
            ws.Cells[row, 2].Value = sub.Name;
            ws.Cells[row, 3].Value = employeeCode;
            ws.Cells[row, 4].Value = name;

            if (layer is not null)
            {
                ws.Cells[row, 5].Value = layer.Value;
            }
        }

        private static void WriteStatus(ExcelRange cell, bool fulfilled)
        {
            cell.Value = fulfilled ? "Terpenuhi" : "Belum";
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

            if (fulfilled)
            {
                cell.Style.Fill.BackgroundColor.SetColor(Rgb("#D1E7DD"));
                cell.Style.Font.Color.SetColor(Rgb("#0F5132"));
            }
            else
            {
                cell.Style.Fill.BackgroundColor.SetColor(Rgb("#FFF3CD"));
                cell.Style.Font.Color.SetColor(Rgb("#664D03"));
            }
        }

        private static bool IsMandatory(string requirement) =>
            string.Equals(requirement, "Mandatory", StringComparison.OrdinalIgnoreCase);

        private static int WriteUnmapped(
            ExcelWorksheet ws,
            int row,
            MatrixExportDepartmentDto dept,
            MatrixExportSubDepartmentDto sub)
        {
            foreach (var person in sub.Unmapped)
            {
                ws.Cells[row, 1].Value = dept.Name;
                ws.Cells[row, 2].Value = sub.Name;
                ws.Cells[row, 8].Value = person.EmployeeCode;
                ws.Cells[row, 9].Value = person.Name;

                var status = ws.Cells[row, 12];
                status.Value = $"Unmapped - {person.Reason}";
                status.Style.Font.Color.SetColor(Rgb("#664D03"));
                status.Style.Fill.PatternType = ExcelFillStyle.Solid;
                status.Style.Fill.BackgroundColor.SetColor(Rgb("#FFF3CD"));

                row++;
            }

            return row;
        }

        private static void WriteCourse(
            ExcelWorksheet ws,
            int row,
            MatrixExportDepartmentDto dept,
            MatrixExportSubDepartmentDto sub,
            MatrixExportCourseDto course)
        {
            ws.Cells[row, 1].Value = dept.Name;
            ws.Cells[row, 2].Value = sub.Name;
            ws.Cells[row, 3].Value = course.CourseCode;
            ws.Cells[row, 4].Value = course.CourseName;

            WriteRequirement(ws.Cells[row, 5], course.Layer1);
            WriteRequirement(ws.Cells[row, 6], course.Layer2);
            WriteRequirement(ws.Cells[row, 7], course.Layer3);
        }

        private static void WriteEmployee(ExcelWorksheet ws, int row, MatrixExportEmployeeDto person)
        {
            ws.Cells[row, 8].Value = person.EmployeeCode;
            ws.Cells[row, 9].Value = person.Name;
            ws.Cells[row, 10].Value = person.Layer;

            ws.Cells[row, 11].Value = person.Requirement;
            ws.Cells[row, 11].Style.Font.Bold =
                string.Equals(person.Requirement, "Mandatory", StringComparison.OrdinalIgnoreCase);

            WriteStatus(ws.Cells[row, 12], person.Fulfilled);

            ws.Cells[row, 13].Value = person.EventCode;

            if (person.EventDate is not null)
            {
                ws.Cells[row, 14].Value = person.EventDate.Value.Date;
                ws.Cells[row, 14].Style.Numberformat.Format = "dd/mm/yyyy";
            }

            ws.Cells[row, 15].Value = person.EventStatus;
            ws.Cells[row, 16].Value = person.Attendance;
        }

        private static void WriteBand(
            ExcelWorksheet ws,
            int row,
            string text,
            string color,
            int columns = ColumnCount)
        {
            var range = ws.Cells[row, 1, row, columns];
            range.Merge = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Rgb(color));
            range.Style.Font.Bold = true;

            ws.Cells[row, 1].Value = text;
        }

        private static void WriteRequirement(ExcelRange cell, string? requirement)
        {
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            if (string.IsNullOrEmpty(requirement))
            {
                return;
            }

            cell.Value = requirement;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;

            if (string.Equals(requirement, "Mandatory", StringComparison.OrdinalIgnoreCase))
            {
                cell.Style.Fill.BackgroundColor.SetColor(Rgb("#F8D7DA"));
                cell.Style.Font.Color.SetColor(Rgb("#842029"));
            }
            else
            {
                cell.Style.Fill.BackgroundColor.SetColor(Rgb("#CFF4FC"));
                cell.Style.Font.Color.SetColor(Rgb("#055160"));
            }
        }

        private static Color Rgb(string hex)
        {
            var h = hex.TrimStart('#');

            return Color.FromArgb(
                int.Parse(h.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(h.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                int.Parse(h.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }
    }
}
