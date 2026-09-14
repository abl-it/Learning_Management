using Dapper;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using System.Globalization;
using Training.Models.DTO.Event;
using Training.Models.DTO.Event.Import;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Import Training Event + Participant dari satu file Excel (sheet "Events" dan "Participants").
    /// </summary>
    /// <remarks>
    /// Desain mengikuti pola Import Master Course (row-by-row, continue-on-error), tetapi setiap
    /// "row" di sini adalah satu Event beserta seluruh Participant-nya, dan insert-nya me-reuse
    /// <see cref="IEventService.CreateAsync"/> yang sudah proven jalan (form Create manual) -
    /// bukan menulis ulang SQL/TVP di sini. Ini artinya satu event + peserta-peserta-nya diproses
    /// sebagai satu transaksi atomik di <c>dbo.usp_TrainingEvent_Create</c>, sama seperti create manual.
    /// </remarks>
    public class EventImportService : IEventImportService
    {
        private readonly string _connectionString;
        private readonly ILogger<EventImportService> _logger;
        private readonly IEventService _eventService;

        public EventImportService(
            IConfiguration configuration,
            ILogger<EventImportService> logger,
            IEventService eventService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
            _logger = logger;
            _eventService = eventService;
        }

        /// <inheritdoc />
        public async Task<EventImportResultDto> ImportEventsFromExcelAsync(
            string filePath,
            string createdBy,
            CancellationToken cancellationToken = default)
        {
            var result = new EventImportResultDto();

            List<EventImportRowDto> events;
            List<EventParticipantImportRowDto> participantRows;

            try
            {
                events = ParseEvents(filePath);
                participantRows = ParseParticipants(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading event import Excel file.");
                result.Message = $"Error saat membaca file Excel: {ex.Message}";
                return result;
            }

            if (events.Count == 0)
            {
                result.Message = "Sheet 'Events' kosong atau format tidak valid.";
                return result;
            }

            var participantsByEventNo = participantRows
                .GroupBy(p => p.EventNo)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Duplicate EventNo dalam batch file yang sama
            var seenEventNo = new HashSet<int>();

            foreach (var evt in events)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!seenEventNo.Add(evt.EventNo))
                    {
                        AddError(result, evt, $"Duplicate: EventNo {evt.EventNo} muncul lebih dari sekali di sheet Events.");
                        continue;
                    }

                    var (isValid, validationError) = ValidateEventRow(evt);
                    if (!isValid)
                    {
                        AddError(result, evt, validationError);
                        continue;
                    }

                    // Resolve DeptName dari CoCode+ABRV (bukan input manual dari Excel) - sekaligus
                    // jadi validasi tambahan bahwa kombinasi CoCode+ABRV benar-benar ada.
                    var deptName = await GetDepartmentNameAsync(evt.CoCode, evt.ABRV, cancellationToken);
                    if (deptName is null)
                    {
                        AddError(result, evt,
                            $"Department dengan CoCode '{evt.CoCode}' dan ABRV '{evt.ABRV}' tidak ditemukan di home.dbo.APP_Departments.");
                        continue;
                    }

                    // Resolve Course -> CourseId + CategoryId + TrainingTitle (single source of truth,
                    // bukan input manual dari Excel)
                    var course = await GetCourseByCodeAsync(evt.CourseCode, cancellationToken);
                    if (course is null)
                    {
                        AddError(result, evt, $"Course Code '{evt.CourseCode}' tidak ditemukan di Master Course.");
                        continue;
                    }

                    evt.TrainingTitle = course.Value.CourseName;

                    // Resolve trainer sesuai EventType
                    string? trainerId;
                    string? trainerName;

                    if (evt.EventType == "I")
                    {
                        trainerId = evt.TrainerId!.Trim();

                        var trainerFullName = await GetEmployeeFullNameAsync(trainerId, cancellationToken);
                        if (trainerFullName is null)
                        {
                            AddError(result, evt, $"Trainer Id '{trainerId}' tidak ditemukan / sudah resign di data karyawan.");
                            continue;
                        }

                        trainerName = trainerFullName;
                    }
                    else
                    {
                        trainerId = null;
                        trainerName = evt.TrainerName!.Trim();
                    }

                    // Resolve participants untuk EventNo ini
                    if (!participantsByEventNo.TryGetValue(evt.EventNo, out var participantRowsForEvent)
                        || participantRowsForEvent.Count == 0)
                    {
                        AddError(result, evt, $"Tidak ada peserta untuk EventNo {evt.EventNo} di sheet Participants (minimal 1 peserta).");
                        continue;
                    }

                    var (participants, participantError) =
                        await ResolveParticipantsAsync(evt.EventNo, participantRowsForEvent, cancellationToken);

                    if (participantError is not null)
                    {
                        AddError(result, evt, participantError);
                        continue;
                    }

                    var createDto = new TrainingEventCreateDto
                    {
                        CourseId = course.Value.CourseId,
                        TrainingCategoryId = course.Value.CategoryId,
                        TrainingTitle = evt.TrainingTitle.Trim(),
                        TrainingDescription = evt.TrainingDescription?.Trim(),
                        TrainingObjective = evt.TrainingObjective?.Trim(),
                        CoCode = evt.CoCode.Trim(),
                        ABRV = evt.ABRV.Trim(),
                        DeptName = deptName,
                        EventType = evt.EventType,
                        TrainerId = trainerId,
                        TrainerName = trainerName,
                        Quota = evt.ParticipantQuota,
                        Budget = evt.Budget,
                        Venue = evt.Venue?.Trim(),
                        EventStartDate = evt.EventStartDate!.Value,
                        EventEndDate = evt.EventEndDate!.Value,
                        SessionCount = evt.SessionCount,
                        DurationHours = evt.DurationHours,
                        Participants = participants
                    };

                    // Reuse alur Create yang sudah ada - satu call = insert event + semua peserta (atomic)
                    await _eventService.CreateAsync(createDto, createdBy, cancellationToken);

                    result.SuccessCount++;
                    _logger.LogInformation(
                        "Event imported: EventNo {EventNo}, CourseCode {CourseCode}, Participants {Count}",
                        evt.EventNo, evt.CourseCode, participants.Count);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Menangkap ArgumentException (validasi C# di EventService.CreateAsync) dan
                    // SqlException (THROW 500xx dari dbo.usp_TrainingEvent_Create) - message-nya
                    // sudah human-readable, langsung ditampilkan apa adanya.
                    _logger.LogError(ex, "EventNo {EventNo} import error.", evt.EventNo);
                    AddError(result, evt, ex.Message);
                }
            }

            if (result.SuccessCount > 0)
            {
                result.Message = $"✓ Import berhasil: {result.SuccessCount} event ditambah";
            }

            if (result.HasErrors)
            {
                result.Message += (string.IsNullOrEmpty(result.Message) ? string.Empty : "\n")
                    + $"✗ {result.Errors.Count} event error (lihat detail di bawah)";
            }

            return result;
        }

        /// <summary>
        /// Validasi field wajib satu baris Event. Menormalisasi <see cref="EventImportRowDto.EventType"/>
        /// ke "I"/"E" sebagai efek samping jika valid.
        /// </summary>
        private static (bool IsValid, string ErrorMessage) ValidateEventRow(EventImportRowDto evt)
        {
            if (evt.EventNo <= 0)
                return (false, "EventNo harus angka positif dan wajib diisi.");

            if (string.IsNullOrWhiteSpace(evt.CourseCode))
                return (false, "Course Code tidak boleh kosong.");

            var normalizedType = NormalizeEventType(evt.EventType);
            if (normalizedType is null)
                return (false, "Event Type harus 'I'/'Internal' atau 'E'/'External'.");
            evt.EventType = normalizedType;

            if (string.IsNullOrWhiteSpace(evt.CoCode))
                return (false, "CoCode tidak boleh kosong.");

            if (string.IsNullOrWhiteSpace(evt.ABRV))
                return (false, "ABRV (Department) tidak boleh kosong.");

            if (evt.EventType == "I" && string.IsNullOrWhiteSpace(evt.TrainerId))
                return (false, "Trainer Id wajib diisi untuk Event Type Internal.");

            if (evt.EventType == "E" && string.IsNullOrWhiteSpace(evt.TrainerName))
                return (false, "Trainer Name wajib diisi untuk Event Type External.");

            if (evt.ParticipantQuota is null or <= 0)
                return (false, "Participant Quota harus angka positif dan wajib diisi.");

            if (evt.EventStartDate is null)
                return (false, "Event Start Date kosong atau format tanggal tidak valid.");

            if (evt.EventEndDate is null)
                return (false, "Event End Date kosong atau format tanggal tidak valid.");

            if (evt.EventEndDate < evt.EventStartDate)
                return (false, "Event End Date tidak boleh lebih awal dari Event Start Date.");

            if (evt.Budget is < 0)
                return (false, "Budget tidak boleh negatif.");

            return (true, string.Empty);
        }

        private static string? NormalizeEventType(string raw)
        {
            return raw?.Trim().ToUpperInvariant() switch
            {
                "I" or "INTERNAL" => "I",
                "E" or "EXTERNAL" => "E",
                _ => null
            };
        }

        /// <summary>
        /// Resolve semua participant untuk satu EventNo. Employee code wajib valid dan aktif
        /// (ResignDate IS NULL) di <c>home.dbo.APP_Employees</c> - Name/ABRV/DeptName TIDAK diambil
        /// dari Excel, selalu di-resolve dari data karyawan supaya konsisten dan bebas typo.
        /// </summary>
        private async Task<(List<TrainingEventParticipantDto> Participants, string? ErrorMessage)> ResolveParticipantsAsync(
            int eventNo,
            List<EventParticipantImportRowDto> rows,
            CancellationToken cancellationToken)
        {
            var participants = new List<TrainingEventParticipantDto>();
            var seenEmployeeCode = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var employeeCode = row.EmployeeCode?.Trim();

                if (string.IsNullOrWhiteSpace(employeeCode))
                    return (participants, $"EventNo {eventNo}: ada baris peserta dengan Employee Code kosong.");

                if (!seenEmployeeCode.Add(employeeCode))
                    return (participants, $"EventNo {eventNo}: Employee Code '{employeeCode}' duplikat di sheet Participants.");

                var employee = await GetEmployeeAsync(employeeCode, cancellationToken);
                if (employee is null)
                {
                    return (participants,
                        $"EventNo {eventNo}: Employee Code '{employeeCode}' tidak ditemukan / sudah resign di data karyawan.");
                }

                participants.Add(new TrainingEventParticipantDto
                {
                    EmployeeCode = employee.Value.EmployeeCode,
                    Name = employee.Value.FullName,
                    ABRV = employee.Value.ABRV,
                    DeptName = employee.Value.DeptName
                });
            }

            return (participants, null);
        }

        /// <summary>
        /// Resolve DeptName dari kombinasi CoCode + ABRV di <c>home.dbo.APP_Departments</c>.
        /// Dipakai untuk event-level department, bukan diisi manual dari Excel supaya konsisten
        /// dengan data master dan sekaligus jadi validasi CoCode+ABRV valid.
        /// </summary>
        private async Task<string?> GetDepartmentNameAsync(
            string coCode,
            string abrv,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT DeptName
                FROM home.dbo.APP_Departments
                WHERE CoCode = @CoCode
                  AND ABRV = @ABRV
                """;

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                sql,
                new { CoCode = coCode.Trim(), ABRV = abrv.Trim() },
                cancellationToken: cancellationToken);

            return await connection.QueryFirstOrDefaultAsync<string>(command);
        }

        private async Task<(int CourseId, int CategoryId, string CourseName)?> GetCourseByCodeAsync(
            string courseCode,
            CancellationToken cancellationToken)
        {
            const string sql = "SELECT CourseId, CategoryId, CourseName FROM Master_Courses WHERE CourseCode = @CourseCode";

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                sql,
                new { CourseCode = courseCode.Trim() },
                cancellationToken: cancellationToken);

            var row = await connection.QueryFirstOrDefaultAsync<CourseLookupRow>(command);

            return row is null ? null : (row.CourseId, row.CategoryId, row.CourseName);
        }

        private async Task<string?> GetEmployeeFullNameAsync(string employeeCode, CancellationToken cancellationToken)
        {
            var employee = await GetEmployeeAsync(employeeCode, cancellationToken);
            return employee?.FullName;
        }

        private async Task<(string EmployeeCode, string FullName, string? ABRV, string? DeptName)?> GetEmployeeAsync(
            string employeeCode,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    e.EmployeeCode,
                    e.FullName,
                    d.ABRV,
                    d.DeptName
                FROM home.dbo.APP_Employees e
                LEFT JOIN home.dbo.APP_Departments d ON e.DepartmentID = d.DepartmentID
                WHERE e.EmployeeCode = @EmployeeCode
                  AND e.ResignDate IS NULL
                """;

            await using var connection = new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                sql,
                new { EmployeeCode = employeeCode.Trim() },
                cancellationToken: cancellationToken);

            var row = await connection.QueryFirstOrDefaultAsync<EmployeeLookupRow>(command);

            return row is null ? null : (row.EmployeeCode, row.FullName, row.ABRV, row.DeptName);
        }

        private static void AddError(EventImportResultDto result, EventImportRowDto evt, string message)
        {
            result.Errors.Add(new EventImportErrorDto
            {
                EventNo = evt.EventNo,
                CourseCode = evt.CourseCode,
                TrainingTitle = evt.TrainingTitle,
                ErrorMessage = message
            });
        }

        #region Excel parsing (sheet "Events" dan "Participants")

        /// <summary>
        /// Kolom sheet "Events" (0-based, mapping via CellReference - lihat catatan di
        /// <see cref="ParseEvents"/>):
        /// 0 EventNo, 1 CourseCode, 2 EventType, 3 CoCode, 4 ABRV,
        /// 5 TrainerId, 6 TrainerName, 7 ParticipantQuota, 8 Venue, 9 EventStartDate,
        /// 10 EventEndDate, 11 SessionCount, 12 DurationHours, 13 Budget, 14 TrainingDescription,
        /// 15 TrainingObjective.
        /// TrainingTitle dan DeptName TIDAK ada di template - TrainingTitle di-resolve dari
        /// <c>Master_Courses.CourseName</c> berdasarkan CourseCode (lihat <see cref="GetCourseByCodeAsync"/>),
        /// DeptName di-resolve dari CoCode+ABRV lewat <see cref="GetDepartmentNameAsync"/>, sama seperti
        /// Participant Name/ABRV/DeptName di-resolve dari EmployeeCode.
        /// </summary>
        /// <remarks>
        /// PENTING: Excel dapat menghilangkan elemen &lt;c&gt; untuk cell kosong sehingga urutan
        /// cell dalam row bisa "meloncat". Mapping kolom karena itu memakai CellReference (A2, B2,
        /// dst), BUKAN index posisi array - pola yang sama dengan <c>TrainingCourseService</c>.
        /// </remarks>
        private List<EventImportRowDto> ParseEvents(string filePath)
        {
            var events = new List<EventImportRowDto>();

            using var doc = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = doc.WorkbookPart
                ?? throw new InvalidOperationException("File Excel tidak valid (WorkbookPart tidak ditemukan).");

            var sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>()
                .FirstOrDefault(s => string.Equals(s.Name, "Events", StringComparison.OrdinalIgnoreCase));

            if (sheet is null)
                throw new InvalidOperationException("Sheet 'Events' tidak ditemukan di file Excel.");

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
            var rows = sheetData.Elements<Row>().ToList();

            for (int i = 1; i < rows.Count; i++) // skip header row 1
            {
                var cellMap = BuildCellMap(workbookPart, rows[i]);

                if (cellMap.Count == 0 || cellMap.Values.All(string.IsNullOrEmpty))
                    continue;

                string? GetCol(int idx) => cellMap.TryGetValue(idx, out var val) ? val?.Trim() : null;

                events.Add(new EventImportRowDto
                {
                    EventNo = int.TryParse(GetCol(0), out var eventNo) ? eventNo : 0,
                    CourseCode = GetCol(1) ?? string.Empty,
                    EventType = GetCol(2) ?? string.Empty,
                    CoCode = GetCol(3) ?? string.Empty,
                    ABRV = GetCol(4) ?? string.Empty,
                    TrainerId = GetCol(5),
                    TrainerName = GetCol(6),
                    ParticipantQuota = int.TryParse(GetCol(7), out var quota) ? quota : null,
                    Venue = GetCol(8),
                    EventStartDate = ParseExcelDate(GetCol(9)),
                    EventEndDate = ParseExcelDate(GetCol(10)),
                    SessionCount = int.TryParse(GetCol(11), out var sessionCount) ? sessionCount : null,
                    DurationHours = decimal.TryParse(GetCol(12), NumberStyles.Any, CultureInfo.InvariantCulture, out var duration) ? duration : null,
                    Budget = decimal.TryParse(GetCol(13), NumberStyles.Any, CultureInfo.InvariantCulture, out var budget) ? budget : null,
                    TrainingDescription = GetCol(14),
                    TrainingObjective = GetCol(15)
                });
            }

            return events;
        }

        /// <summary>
        /// Kolom sheet "Participants" (0-based): 0 EventNo, 1 EmployeeCode.
        /// Name/ABRV/DeptName sengaja TIDAK ada di template - selalu di-resolve dari
        /// <c>home.dbo.APP_Employees</c> (lihat <see cref="ResolveParticipantsAsync"/>).
        /// </summary>
        private List<EventParticipantImportRowDto> ParseParticipants(string filePath)
        {
            var participants = new List<EventParticipantImportRowDto>();

            using var doc = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = doc.WorkbookPart
                ?? throw new InvalidOperationException("File Excel tidak valid (WorkbookPart tidak ditemukan).");

            var sheet = workbookPart.Workbook.Sheets?.Elements<Sheet>()
                .FirstOrDefault(s => string.Equals(s.Name, "Participants", StringComparison.OrdinalIgnoreCase));

            if (sheet is null)
                throw new InvalidOperationException("Sheet 'Participants' tidak ditemukan di file Excel.");

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
            var rows = sheetData.Elements<Row>().ToList();

            for (int i = 1; i < rows.Count; i++) // skip header row 1
            {
                var cellMap = BuildCellMap(workbookPart, rows[i]);

                if (cellMap.Count == 0 || cellMap.Values.All(string.IsNullOrEmpty))
                    continue;

                string? GetCol(int idx) => cellMap.TryGetValue(idx, out var val) ? val?.Trim() : null;

                participants.Add(new EventParticipantImportRowDto
                {
                    EventNo = int.TryParse(GetCol(0), out var eventNo) ? eventNo : 0,
                    EmployeeCode = GetCol(1) ?? string.Empty
                });
            }

            return participants;
        }

        private static Dictionary<int, string> BuildCellMap(WorkbookPart workbookPart, Row row)
        {
            var cellMap = new Dictionary<int, string>();

            foreach (var cell in row.Elements<Cell>())
            {
                int colIndex = GetColumnIndexFromCellReference(cell.CellReference?.Value);
                if (colIndex >= 0)
                    cellMap[colIndex] = GetCellValue(workbookPart, cell);
            }

            return cellMap;
        }

        /// <summary>
        /// Ambil value cell, handle ketiga bentuk penyimpanan text Excel: SharedString (t="s",
        /// dipakai Microsoft Excel asli), InlineString (t="inlineStr", dipakai openpyxl/LibreOffice
        /// secara default - <see cref="Cell.CellValue"/> null untuk bentuk ini), dan angka biasa.
        /// Tanpa handle InlineString, semua kolom teks di file yang tidak dibuat oleh Excel asli
        /// akan ke-parse jadi string kosong tanpa error apa pun (silent failure).
        /// </summary>
        private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
        {
            if (cell is null)
                return string.Empty;

            if (cell.DataType?.Value == CellValues.SharedString)
            {
                if (cell.CellValue is null)
                    return string.Empty;

                int ssid = int.Parse(cell.CellValue.Text);
                return workbookPart.SharedStringTablePart!.SharedStringTable
                    .Elements<SharedStringItem>().ElementAt(ssid).InnerText;
            }

            if (cell.DataType?.Value == CellValues.InlineString)
            {
                return cell.InlineString?.Text?.Text ?? cell.InlineString?.InnerText ?? string.Empty;
            }

            return cell.CellValue?.Text ?? string.Empty;
        }

        private static int GetColumnIndexFromCellReference(string? cellReference)
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
        /// Excel menyimpan tanggal sebagai serial number OLE Automation (mis. "46655"), bukan
        /// teks tanggal, kecuali cell diformat sebagai Text. Coba serial number dulu, baru fallback
        /// ke parsing string tanggal biasa.
        /// </summary>
        private static DateTime? ParseExcelDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial))
                return DateTime.FromOADate(serial);

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;

            return null;
        }

        private sealed class CourseLookupRow
        {
            public int CourseId { get; set; }
            public int CategoryId { get; set; }
            public string CourseName { get; set; } = string.Empty;
        }

        private sealed class EmployeeLookupRow
        {
            public string EmployeeCode { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? ABRV { get; set; }
            public string? DeptName { get; set; }
        }

        #endregion
    }
}
