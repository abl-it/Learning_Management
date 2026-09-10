using Dapper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Training.Models.Dtos;
using Training.Services.Interfaces;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Training.Services.Implementations
{


    /// <summary>
    /// Service implementation untuk Training Course management
    /// Menggunakan Dapper ORM dan SQL Server stored procedures
    /// </summary>
    public class TrainingCourseService : ITrainingCourseService
    {
        private readonly string _connectionString;
        private readonly ILogger<TrainingCourseService> _logger;

        public TrainingCourseService(IConfiguration configuration, ILogger<TrainingCourseService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        /// <summary>
        /// Import Master Course dari Excel file dengan row-by-row processing
        /// </summary>
        /// <remarks>
        /// Strategy: Continue jika ada error, report semua error di akhir
        /// Setiap valid row akan di-insert langsung, sequence category otomatis increment
        /// </remarks>
        public async Task<CourseImportResultDto> ImportCoursesFromExcelAsync(string filePath, string createdBy)
        {
            var result = new CourseImportResultDto();

            try
            {
                // Parse Excel file
                var courses = ParseExcelFile(filePath);

                if (!courses.Any())
                {
                    result.Message = "File Excel kosong atau format tidak valid.";
                    return result;
                }

                // Process setiap row
                int rowNum = 2; // Start dari row 2 (row 1 header)
                // Track (CategoryId|CourseNameLower) yang sudah diproses dalam batch file ini,
                // untuk menangkap duplicate ANTAR baris di file yang sama (bukan hanya vs data lama di DB)
                var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var course in courses)
                {
                    try
                    {
                        // Step 1: Validasi data
                        var (isValid, errorMessage) = await ValidateCourseDataAsync(course);
                        if (!isValid)
                        {
                            result.Errors.Add(new CourseImportErrorDto
                            {
                                RowNumber = rowNum,
                                CategoryCode = course.CategoryCode,
                                CourseName = course.CourseName,
                                ErrorMessage = errorMessage
                            });
                            rowNum++;
                            continue;
                        }

                        // Step 2: Get Category ID
                        var categoryId = await GetCategoryIdByCodeAsync(course.CategoryCode);
                        if (!categoryId.HasValue)
                        {
                            result.Errors.Add(new CourseImportErrorDto
                            {
                                RowNumber = rowNum,
                                CategoryCode = course.CategoryCode,
                                CourseName = course.CourseName,
                                ErrorMessage = $"Category Code '{course.CategoryCode}' tidak ditemukan di Master Category."
                            });
                            rowNum++;
                            continue;
                        }

                        // Step 2.5: Duplicate check - dalam batch file yang sama
                        var batchKey = $"{categoryId.Value}|{course.CourseName.Trim()}";
                        if (seenInBatch.Contains(batchKey))
                        {
                            result.Errors.Add(new CourseImportErrorDto
                            {
                                RowNumber = rowNum,
                                CategoryCode = course.CategoryCode,
                                CourseName = course.CourseName,
                                ErrorMessage = $"Duplicate: Course '{course.CourseName}' muncul lebih dari sekali untuk kategori '{course.CategoryCode}' di file ini."
                            });
                            rowNum++;
                            continue;
                        }

                        // Step 2.6: Duplicate check - terhadap data yang sudah ada di database
                        var isDuplicateInDb = await IsDuplicateCourseNameAsync(categoryId.Value, course.CourseName);
                        if (isDuplicateInDb)
                        {
                            result.Errors.Add(new CourseImportErrorDto
                            {
                                RowNumber = rowNum,
                                CategoryCode = course.CategoryCode,
                                CourseName = course.CourseName,
                                ErrorMessage = $"Duplicate: Course '{course.CourseName}' sudah terdaftar untuk kategori '{course.CategoryCode}'."
                            });
                            rowNum++;
                            continue;
                        }

                        seenInBatch.Add(batchKey);

                        // Step 3: Convert IsActive (Yes/No -> 1/0)
                        bool isActive = course.IsActive?.Trim().ToLower() == "yes" ? true : false;

                        // Step 4: Create course (auto-generate course code)
                        var (success, courseId, courseCode, createError) = await CreateCourseAsync(
                            categoryId.Value,
                            course.CourseName,
                            course.Description,
                            course.Duration,
                            course.TrainerType,
                            isActive,
                            createdBy
                        );

                        if (success)
                        {
                            result.SuccessCount++;
                            _logger.LogInformation($"Course created: {courseCode} (ID: {courseId})");
                        }
                        else
                        {
                            result.Errors.Add(new CourseImportErrorDto
                            {
                                RowNumber = rowNum,
                                CategoryCode = course.CategoryCode,
                                CourseName = course.CourseName,
                                ErrorMessage = createError
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Row {rowNum} error: {ex.Message}");
                        result.Errors.Add(new CourseImportErrorDto
                        {
                            RowNumber = rowNum,
                            CategoryCode = course.CategoryCode,
                            CourseName = course.CourseName,
                            ErrorMessage = $"Exception: {ex.Message}"
                        });
                    }
                    finally
                    {
                        rowNum++;
                    }
                }

                // Generate message
                if (result.SuccessCount > 0)
                {
                    result.Message = $"✓ Import berhasil: {result.SuccessCount} course ditambah";
                }

                if (result.HasErrors)
                {
                    result.Message += $"\n✗ {result.Errors.Count} row error (lihat detail di bawah)";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Import process error: {ex.Message}");
                result.Message = $"Error saat membaca file Excel: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Parse Excel file menggunakan OpenXML
        /// </summary>
        /// <remarks>
        /// PENTING: Excel dapat menghilangkan elemen &lt;c&gt; untuk cell yang kosong,
        /// sehingga urutan cells dalam row bisa "meloncat" (mis. langsung dari A ke D).
        /// Karena itu mapping kolom menggunakan CellReference (A2, B2, dst), BUKAN index posisi array,
        /// supaya data tidak salah geser kolom saat ada cell kosong di tengah row.
        /// </remarks>
        private List<CourseImportDto> ParseExcelFile(string filePath)
        {
            var courses = new List<CourseImportDto>();

            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
            {
                WorkbookPart workbookPart = doc.WorkbookPart;
                Sheet sheet = workbookPart.Workbook.Sheets.Elements<Sheet>().FirstOrDefault();

                if (sheet == null)
                    return courses;

                WorksheetPart worksheetPart = workbookPart.GetPartById(sheet.Id) as WorksheetPart;
                SheetData sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();

                var rows = sheetData.Elements<Row>().ToList();

                // Skip header (row 1)
                for (int i = 1; i < rows.Count; i++)
                {
                    Row row = rows[i];

                    // Map: columnIndex (0-based: A=0, B=1, ...) -> cell value
                    var cellMap = new Dictionary<int, string>();
                    foreach (var cell in row.Elements<Cell>())
                    {
                        int colIndex = GetColumnIndexFromCellReference(cell.CellReference?.Value);
                        if (colIndex >= 0)
                        {
                            cellMap[colIndex] = GetCellValue(workbookPart, cell);
                        }
                    }

                    // Skip jika row kosong
                    if (cellMap.Count == 0 || cellMap.Values.All(v => string.IsNullOrEmpty(v)))
                        continue;

                    string GetCol(int idx) => cellMap.TryGetValue(idx, out var val) ? val?.Trim() : null;

                    var course = new CourseImportDto
                    {
                        CategoryCode = GetCol(0),
                        CourseName = GetCol(1),
                        Description = GetCol(2),
                        Duration = decimal.TryParse(GetCol(3), out var duration) ? duration : 0,
                        TrainerType = GetCol(4),
                        IsActive = GetCol(5)
                    };

                    courses.Add(course);
                }
            }

            return courses;
        }

        /// <summary>
        /// Ekstrak column index (0-based) dari cell reference Excel, mis. "A2" -> 0, "F86" -> 5.
        /// </summary>
        private int GetColumnIndexFromCellReference(string cellReference)
        {
            if (string.IsNullOrEmpty(cellReference))
                return -1;

            var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
            if (string.IsNullOrEmpty(letters))
                return -1;

            int columnIndex = 0;
            foreach (char c in letters.ToUpperInvariant())
            {
                columnIndex = columnIndex * 26 + (c - 'A' + 1);
            }

            return columnIndex - 1; // convert to 0-based
        }

        /// <summary>
        /// Get cell value dari Excel cell (handle both text dan numeric)
        /// </summary>
        private string GetCellValue(WorkbookPart workbookPart, Cell cell)
        {
            if (cell == null)
                return string.Empty;

            if (cell.DataType?.Value == CellValues.SharedString)
            {
                int ssid = int.Parse(cell.CellValue.Text);
                return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>()
                    .ElementAt(ssid).InnerText;
            }

            return cell.CellValue?.Text ?? string.Empty;
        }

        /// <summary>
        /// Validasi data course sebelum insert
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateCourseDataAsync(CourseImportDto course)
        {
            // Validate Category Code
            if (string.IsNullOrWhiteSpace(course.CategoryCode))
                return (false, "Category Code tidak boleh kosong.");

            if (course.CategoryCode.Length > 50)
                return (false, "Category Code terlalu panjang (max 50 karakter).");

            // Validate Course Name
            if (string.IsNullOrWhiteSpace(course.CourseName))
                return (false, "Course Name tidak boleh kosong.");

            if (course.CourseName.Length > 200)
                return (false, "Course Name terlalu panjang (max 200 karakter).");

            // Validate Description
            if (string.IsNullOrWhiteSpace(course.Description))
                return (false, "Description tidak boleh kosong.");

            if (course.Description.Length > 1000)
                return (false, "Description terlalu panjang (max 1000 karakter).");

            // Validate Duration
            if (course.Duration <= 0)
                return (false, "Duration harus angka positif (jam).");

            if (course.Duration > 999.99m)
                return (false, "Duration terlalu besar (max 999.99).");

            // Validate Trainer Type
            if (string.IsNullOrWhiteSpace(course.TrainerType))
                return (false, "Trainer Type tidak boleh kosong.");

            if (course.TrainerType.Length > 200)
                return (false, "Trainer Type terlalu panjang (max 200 karakter).");

            // Validate IsActive
            if (string.IsNullOrWhiteSpace(course.IsActive))
                return (false, "Is Active tidak boleh kosong.");

            var isActiveUpper = course.IsActive.Trim().ToLower();
            if (isActiveUpper != "yes" && isActiveUpper != "no")
                return (false, "Is Active harus 'Yes' atau 'No'.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Get Category ID dari Category Code
        /// </summary>
        public async Task<int?> GetCategoryIdByCodeAsync(string categoryCode)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                {
                    await conn.OpenAsync();

                    var query = @"
                        SELECT CategoryId 
                        FROM Master_TrainingCategories 
                        WHERE CategoryCode = @categoryCode";

                    var result = await conn.QueryFirstOrDefaultAsync<int?>(query, new { categoryCode });
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting category ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cek apakah course dengan nama yang sama (case-insensitive) sudah ada di kategori yang sama
        /// </summary>
        public async Task<bool> IsDuplicateCourseNameAsync(int categoryId, string courseName)
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var query = @"
                    SELECT COUNT(1)
                    FROM Master_Courses
                    WHERE CategoryId = @categoryId
                      AND LOWER(LTRIM(RTRIM(CourseName))) = LOWER(LTRIM(RTRIM(@courseName)))";

                var count = await conn.ExecuteScalarAsync<int>(query, new { categoryId, courseName });
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking duplicate course name: {ex.Message}");
                // Fail-safe: jika gagal cek, jangan block import (biarkan lanjut, error lain akan tertangkap di CreateCourseAsync)
                return false;
            }
        }

        /// <summary>
        /// Create course dengan stored procedure USP_Master_CreateCourse
        /// </summary>
        /// <remarks>
        /// Procedure akan:
        /// 1. Auto-increment sequence di Master_CategorySequence
        /// 2. Generate course code: {CategoryCode}-{Sequence}
        /// 3. Insert ke Master_Courses
        /// </remarks>
        public async Task<(bool Success, int CourseId, string CourseCode, string ErrorMessage)>
            CreateCourseAsync(int categoryId, string courseName, string description,
                decimal duration, string provider, bool isActive, string createdBy)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@courseName", courseName);
                    parameters.Add("@description", description);
                    parameters.Add("@categoryId", categoryId);
                    parameters.Add("@duration", duration);
                    parameters.Add("@provider", provider);
                    parameters.Add("@isActive", isActive ? 1 : 0);
                    parameters.Add("@createdBy", createdBy);
                    parameters.Add("@newCourseId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await conn.ExecuteAsync(
                        "dbo.USP_Master_CreateCourse",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    int newCourseId = parameters.Get<int>("@newCourseId");

                    // Query course code yang baru saja dibuat
                    var query = "SELECT CourseCode FROM Master_Courses WHERE CourseId = @courseId";
                    var courseCode = await conn.QueryFirstOrDefaultAsync<string>(query, new { courseId = newCourseId });

                    _logger.LogInformation($"Course created: {courseCode} (ID: {newCourseId})");

                    return (true, newCourseId, courseCode, string.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating course: {ex.Message}");
                return (false, 0, string.Empty, ex.Message);
            }
        }
    }
}
