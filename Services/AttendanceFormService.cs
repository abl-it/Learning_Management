using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Training.Models.DTO.Event;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Renders the training attendance form ("Daftar Hadir") as a PDF document
    /// using QuestPDF. The layout mirrors the controlled form
    /// <c>F-L&amp;D-DA-01</c> (No Rev 00): company letterhead, document control
    /// block (form number / revision / issue date), TOPIK - HARI/TANGGAL - JAM -
    /// INSTRUKTUR block, a bordered table of NO, Nama, Departemen, Jabatan and a
    /// two-column TANDATANGAN area, and a "Mengetahui (Instruktur)" box.
    /// </summary>
    /// <remarks>
    /// This service is intentionally free of any data-access or authorization
    /// concerns - it is a pure document composer. Fetching the training event
    /// and validating that the current user is allowed to view it remains the
    /// responsibility of <see cref="IEventService"/> and the calling controller.
    /// </remarks>
    public class AttendanceFormService : IAttendanceFormService
    {
        // ---------------------------------------------------------------
        // Controlled-document identity (No. Reg). Update here when the
        // form is revised.
        // ---------------------------------------------------------------
        private const string FormNumber = "F-L&D-DA-01";
        private const string FormRevision = "00";
        private const string FormIssueDate = "25 / 2 / 2026";

        private const string CompanyName = "PT. AGUNG BUMI LESTARI";
        private const string CompanyAddress =
            "Jl. Raya Pasir Putih Km 14 No. 8, Desa Baru, Kab. Kampar-Riau";

        /// <summary>
        /// Letterhead image, relative to the web root (wwwroot).
        /// If the file is missing a plain-text letterhead is printed instead,
        /// so printing never fails because of a missing image.
        /// </summary>
        private const string LetterheadRelativePath = "images/abl-letterhead.png";

        /// <summary>Printed width (points) of the letterhead image; keeps its aspect ratio.</summary>
        private const float LetterheadWidth = 380;

        /// <summary>Minimum height (points) of one participant row (27pt in the Excel form, printed at ~80%).</summary>
        private const float RowMinHeight = 21.6f;

        /// <summary>
        /// Number of participant rows sharing one signature "band". The form
        /// pairs two participants per band (signature boxes "1." and "2.").
        /// </summary>
        private const int ParticipantsPerBand = 2;

        /// <summary>
        /// Fixed number of participant rows per page (the form has 22 rows).
        /// Pages are always filled to a full multiple of this value with blank
        /// rows - e.g. 25 participants render 44 rows (2 pages of 22).
        /// </summary>
        private const int RowsPerPage = 22;

        private static readonly string[] DayNames =
        {
            "Minggu", "Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu"
        };

        private static readonly string[] MonthNames =
        {
            "Januari", "Februari", "Maret", "April", "Mei", "Juni",
            "Juli", "Agustus", "September", "Oktober", "November", "Desember"
        };

        private static byte[]? _letterheadCache;
        private static bool _letterheadLoaded;
        private static readonly object LetterheadLock = new();

        private readonly IWebHostEnvironment _env;

        public AttendanceFormService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <inheritdoc />
        public byte[] GenerateAttendanceFormPdf(TrainingEventDetailDto trainingEvent)
        {
            ArgumentNullException.ThrowIfNull(trainingEvent);

            var participants = trainingEvent.Participants ?? [];
            var letterhead = GetLetterhead();

            var pageCount = Math.Max(
                1,
                (int)Math.Ceiling(participants.Count / (double)RowsPerPage));

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(20);
                    page.MarginVertical(18);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content()
                        .Column(column =>
                        {
                            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
                            {
                                if (pageIndex > 0)
                                {
                                    // Every page is a complete, individually signed form
                                    // (letterhead, topic block, table and sign-off), so
                                    // each page holds exactly RowsPerPage rows.
                                    column.Item().PageBreak();
                                }

                                var startIndex = pageIndex * RowsPerPage;

                                column.Item().Element(c => ComposeFormPage(
                                    c,
                                    trainingEvent,
                                    participants,
                                    letterhead,
                                    startIndex));
                            }
                        });
                });
            });

            return document.GeneratePdf();
        }

        #region Page composition

        /// <summary>
        /// Composes one complete form page: letterhead, document control,
        /// title, topic block, participant table and the "Mengetahui" box.
        /// </summary>
        private static void ComposeFormPage(
            IContainer container,
            TrainingEventDetailDto trainingEvent,
            IReadOnlyList<TrainingEventParticipantDto> participants,
            byte[]? letterhead,
            int startIndex)
        {
            container.Column(column =>
            {
                column.Item().Element(c => ComposeLetterhead(c, letterhead));

                column.Item().Element(ComposeDocumentControl);

                column.Item()
                    .PaddingTop(4)
                    .AlignCenter()
                    .Text("DAFTAR HADIR")
                    .Bold()
                    .FontSize(16);

                column.Item()
                    .PaddingTop(6)
                    .PaddingBottom(5)
                    .Element(c => ComposeTopicBlock(c, trainingEvent));

                column.Item().Element(c => ComposeParticipantTable(
                    c,
                    participants,
                    startIndex,
                    RowsPerPage));

                column.Item()
                    .PaddingTop(10)
                    .AlignRight()
                    .Element(ComposeAcknowledgeBox);
            });
        }

        /// <summary>
        /// Company letterhead. Uses the letterhead image when available,
        /// otherwise falls back to a text-only header.
        /// </summary>
        private static void ComposeLetterhead(IContainer container, byte[]? letterhead)
        {
            // Same as the Excel form: logo/address block centered, then a
            // full-width rule underneath.
            container
                .BorderBottom(1.5f)
                .BorderColor(Colors.Black)
                .PaddingBottom(2)
                .Column(column =>
                {
                    if (letterhead is { Length: > 0 })
                    {
                        column.Item()
                            .AlignCenter()
                            .Width(LetterheadWidth)
                            .Image(letterhead)
                            .FitWidth();
                    }
                    else
                    {
                        column.Item().AlignCenter().Text(CompanyName).Bold().FontSize(16);
                        column.Item().AlignCenter().Text(CompanyAddress).FontSize(8);
                    }
                });
        }

        /// <summary>
        /// Right-aligned document control block: form number, revision, issue date.
        /// </summary>
        private static void ComposeDocumentControl(IContainer container)
        {
            container
                .PaddingTop(4)
                .AlignRight()
                .Column(column =>
                {
                    column.Spacing(1);

                    column.Item().AlignRight().Text(FormNumber).Bold().FontSize(8);
                    column.Item().AlignRight().Text($"No Rev : {FormRevision}").Bold().FontSize(8);
                    column.Item().AlignRight().Text($"Tanggal Terbit : {FormIssueDate}").Bold().FontSize(8);
                });
        }

        /// <summary>
        /// TOPIK / HARI/ TANGGAL / JAM / INSTRUKTUR block.
        /// </summary>
        private static void ComposeTopicBlock(
            IContainer container,
            TrainingEventDetailDto trainingEvent)
        {
            container.Column(column =>
            {
                column.Spacing(1);

                ComposeInfoField(column, "TOPIK", FormatTopic(trainingEvent));
                ComposeInfoField(column, "HARI/ TANGGAL", FormatEventDate(trainingEvent));
                ComposeInfoField(column, "JAM", FormatEventTime(trainingEvent));
                ComposeInfoField(column, "INSTRUKTUR", trainingEvent.TrainerName);
            });
        }

        /// <summary>
        /// Renders a single bold "LABEL : Value" line. The label sits in a
        /// fixed-width column so every colon lines up vertically.
        /// </summary>
        private static void ComposeInfoField(
            ColumnDescriptor column,
            string label,
            string? value)
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(105).Text(label).Bold().FontSize(10);
                row.ConstantItem(14).Text(":").Bold().FontSize(10);
                row.RelativeItem().Text(value ?? string.Empty).Bold().FontSize(10);
            });
        }

        /// <summary>
        /// Composes one page's bordered participant table: NO, Nama, Departemen,
        /// Jabatan and a two-column TANDATANGAN area, for exactly
        /// <paramref name="rowCount"/> rows starting at <paramref name="startIndex"/>.
        /// Row numbers are absolute (continuing across pages, e.g. 23-44 on the
        /// second page). Participants are laid out two per "band" so each pair
        /// shares a signature area; box "1." is labelled top-left and box "2."
        /// bottom-left, exactly as on the reference form.
        /// </summary>
        private static void ComposeParticipantTable(
            IContainer container,
            IReadOnlyList<TrainingEventParticipantDto> participants,
            int startIndex,
            int rowCount)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(28);       // NO
                    columns.RelativeColumn(32f);      // Nama
                    columns.RelativeColumn(37.5f);    // Departemen (cukup lebar agar "INFORMATION & TECHNOLOGY" 1 baris)
                    columns.RelativeColumn(23.4f);    // Jabatan
                    columns.RelativeColumn(16.1f);    // Tanda tangan (kiri)
                    columns.RelativeColumn(16.0f);    // Tanda tangan (kanan)
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("NO");
                    header.Cell().Element(HeaderCellStyle).Text("Nama");
                    header.Cell().Element(HeaderCellStyle).Text("Departemen");
                    header.Cell().Element(HeaderCellStyle).Text("Jabatan");
                    header.Cell().ColumnSpan(2).Element(HeaderCellStyle).Text("TANDATANGAN");
                });

                for (var bandStart = 0; bandStart < rowCount; bandStart += ParticipantsPerBand)
                {
                    var bandSize = Math.Min(
                        ParticipantsPerBand,
                        rowCount - bandStart);

                    for (var offset = 0; offset < bandSize; offset++)
                    {
                        var rowIndex = startIndex + bandStart + offset;

                        // Rows beyond the registered participant count are
                        // intentionally left blank (reserved seats / walk-ins).
                        var participant = rowIndex < participants.Count
                            ? participants[rowIndex]
                            : null;

                        table.Cell().Element(BodyCellStyle).AlignMiddle().AlignCenter().Text((rowIndex + 1).ToString());
                        table.Cell().Element(BodyCellStyle).AlignMiddle().Text(participant?.Name ?? string.Empty);
                        table.Cell().Element(BodyCellStyle).AlignMiddle().Text(participant?.DeptName ?? string.Empty);
                        // Jabatan sengaja dicetak kosong (diisi tangan oleh peserta).
                        table.Cell().Element(BodyCellStyle).AlignMiddle().Text(string.Empty);

                        /*
                         * Only the first row of the band adds the signature cells.
                         * They are given RowSpan = bandSize so they stretch across
                         * every row in this band; QuestPDF's table auto-flow then
                         * skips these already-occupied slots for the remaining row.
                         */
                        if (offset == 0)
                        {
                            var firstRowNo = startIndex + bandStart + 1;
                            var secondRowNo = bandSize == 2 ? startIndex + bandStart + 2 : (int?)null;

                            table.Cell()
                                .RowSpan((uint)bandSize)
                                .Element(SignatureCellStyle)
                                .AlignTop()
                                .Text($"{firstRowNo}.")
                                .FontSize(8);

                            table.Cell()
                                .RowSpan((uint)bandSize)
                                .Element(SignatureCellStyle)
                                .AlignBottom()
                                .Text(secondRowNo.HasValue ? $"{secondRowNo.Value}." : string.Empty)
                                .FontSize(8);
                        }
                    }
                }
            });

            static IContainer HeaderCellStyle(IContainer cellContainer) =>
                cellContainer
                    .Border(0.75f)
                    .BorderColor(Colors.Black)
                    .PaddingVertical(3)
                    .PaddingHorizontal(3)
                    .AlignMiddle()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.Bold().FontSize(10));

            static IContainer BodyCellStyle(IContainer cellContainer) =>
                cellContainer
                    .MinHeight(RowMinHeight)
                    .Border(0.75f)
                    .BorderColor(Colors.Black)
                    .PaddingVertical(1)
                    .PaddingHorizontal(4);

            static IContainer SignatureCellStyle(IContainer cellContainer) =>
                cellContainer
                    .Border(0.75f)
                    .BorderColor(Colors.Black)
                    .Padding(2);
        }

        /// <summary>
        /// "Mengetahui ... (Instruktur)" sign-off box at the bottom-right of the form.
        /// </summary>
        private static void ComposeAcknowledgeBox(IContainer container)
        {
            container
                .Width(150)
                .Border(0.75f)
                .BorderColor(Colors.Black)
                .Padding(4)
                .Column(column =>
                {
                    column.Item().AlignCenter().Text("Mengetahui").FontSize(9);

                    column.Item().Height(42);

                    column.Item()
                        .PaddingHorizontal(6)
                        .LineHorizontal(0.75f)
                        .LineColor(Colors.Grey.Darken1);

                    column.Item().AlignCenter().Text("(Instruktur)").FontSize(9);
                });
        }

        #endregion

        #region Formatting helpers

        /// <summary>
        /// "Selasa 29/September/2026". For multi-day events a range is shown:
        /// "Selasa 29/September/2026 s/d Kamis 1/Oktober/2026".
        /// </summary>
        private static string FormatEventDate(TrainingEventDetailDto trainingEvent)
        {
            var start = trainingEvent.EventStartDate;
            var end = trainingEvent.EventEndDate;

            return end.Date > start.Date
                ? $"{FormatIndonesianDate(start)} s/d {FormatIndonesianDate(end)}"
                : FormatIndonesianDate(start);
        }

        private static string FormatIndonesianDate(DateTime date) =>
            $"{DayNames[(int)date.DayOfWeek]} {date.Day:00}/{MonthNames[date.Month - 1]}/{date.Year}";

        /// <summary>"SETTING MIKROTIK DASAR (TTS-00078)" - judul training diikuti kode course dalam kurung.</summary>
        private static string FormatTopic(TrainingEventDetailDto trainingEvent)
        {
            var title = trainingEvent.TrainingTitle?.Trim() ?? string.Empty;
            var code = trainingEvent.CourseCode?.Trim();

            return string.IsNullOrWhiteSpace(code)
                ? title
                : $"{title} ({code})";
        }

        /// <summary>"13.00 - 16.00" (dot as the hour/minute separator, as on the form).</summary>
        private static string FormatEventTime(TrainingEventDetailDto trainingEvent) =>
            $"{trainingEvent.EventStartDate:HH\\.mm} - {trainingEvent.EventEndDate:HH\\.mm}";

        /// <summary>
        /// Reads the letterhead image once and caches it for the lifetime of the
        /// process. Returns <see langword="null"/> when the file is not found.
        /// </summary>
        private byte[]? GetLetterhead()
        {
            if (_letterheadLoaded)
            {
                return _letterheadCache;
            }

            lock (LetterheadLock)
            {
                if (_letterheadLoaded)
                {
                    return _letterheadCache;
                }

                try
                {
                    var path = Path.Combine(
                        _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
                        LetterheadRelativePath.Replace('/', Path.DirectorySeparatorChar));

                    _letterheadCache = File.Exists(path)
                        ? File.ReadAllBytes(path)
                        : null;
                }
                catch
                {
                    _letterheadCache = null;
                }

                // Only cache a successful load; if the file is missing, retry
                // on the next print so dropping the file in needs no restart.
                _letterheadLoaded = _letterheadCache is not null;

                return _letterheadCache;
            }
        }

        #endregion
    }
}
