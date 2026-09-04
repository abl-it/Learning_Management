using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Training.Models.DTO.Event;
using Training.Services.IServices;

namespace Training.Services
{
    /// <summary>
    /// Renders the training attendance form ("Daftar Hadir") as a PDF document
    /// using QuestPDF. The layout mirrors the reference attendance spreadsheet:
    /// a header block with training/instructor/date information followed by a
    /// bordered table of No, Nama, Departemen and Tanda Tangan (signature) columns.
    /// </summary>
    /// <remarks>
    /// This service is intentionally free of any data-access or authorization
    /// concerns - it is a pure document composer. Fetching the training event
    /// and validating that the current user is allowed to view it remains the
    /// responsibility of <see cref="IEventService"/> and the calling controller.
    /// </remarks>
    public class AttendanceFormService : IAttendanceFormService
    {
        /// <summary>
        /// Number of participant rows sharing one signature "band". The reference
        /// spreadsheet pairs two participants per band so their signature boxes
        /// are tall enough to sign comfortably while keeping the sheet compact.
        /// </summary>
        private const int ParticipantsPerBand = 2;

        /// <summary>
        /// Fixed number of participant rows shown per page. Pages are always
        /// filled to a full multiple of this value with blank rows - e.g. 20
        /// participants still renders 36 rows (2 full pages of 18), not 20.
        /// </summary>
        private const int RowsPerPage = 18;

        /// <inheritdoc />
        public byte[] GenerateAttendanceFormPdf(TrainingEventDetailDto trainingEvent)
        {
            ArgumentNullException.ThrowIfNull(trainingEvent);

            var participants = trainingEvent.Participants ?? [];
            var pageCount = Math.Max(
                1,
                (int)Math.Ceiling(participants.Count / (double)RowsPerPage));

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    page.Content()
                        .Column(column =>
                        {
                            column.Item().Element(c => ComposeHeader(c, trainingEvent));

                            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
                            {
                                if (pageIndex > 0)
                                {
                                    // Force a new page instead of relying on the table's
                                    // natural overflow, so every page - including the
                                    // header-less continuation pages - holds exactly
                                    // RowsPerPage rows rather than however many
                                    // physically happen to fit.
                                    column.Item().PageBreak();
                                }

                                var startIndex = pageIndex * RowsPerPage;

                                column.Item()
                                    .PaddingTop(8)
                                    .Element(c => ComposeParticipantTable(
                                        c,
                                        participants,
                                        startIndex,
                                        RowsPerPage));
                            }
                        });

                    page.Footer()
                        .PaddingTop(5)
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Dicetak pada ").FontSize(7).FontColor(Colors.Grey.Darken1);
                            text.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Darken1);
                        });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Composes the bordered title and event-information block at the top of the form.
        /// </summary>
        private static void ComposeHeader(
            IContainer container,
            TrainingEventDetailDto trainingEvent)
        {
            container
                .Border(1)
                .BorderColor(Colors.Black)
                .Column(column =>
                {
                    column.Item()
                        .Padding(8)
                        .AlignCenter()
                        .Text("DAFTAR HADIR TRAINING")
                        .Bold()
                        .FontSize(14);

                    column.Item().LineHorizontal(1).LineColor(Colors.Black);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Padding(8)
                            .Column(info =>
                            {
                                ComposeInfoField(info, "Judul Training", trainingEvent.TrainingTitle, 80);
                                ComposeInfoField(info, "Instruktur", trainingEvent.TrainerName, 80);
                                ComposeInfoField(info, "Tempat", trainingEvent.Venue, 80);
                                ComposeInfoField(info, "Departemen", trainingEvent.DeptName, 80);
                            });

                        row.ConstantItem(150)
                            .Padding(8)
                            .Column(info =>
                            {
                                ComposeInfoField(
                                    info,
                                    "Tanggal",
                                    trainingEvent.EventStartDate.ToString("dd/MM/yyyy"),
                                    55,
                                    alignValueRight: false);

                                ComposeInfoField(
                                    info,
                                    "Jam",
                                    $"{trainingEvent.EventStartDate:HH:mm} - {trainingEvent.EventEndDate:HH:mm}",
                                    55,
                                    alignValueRight: false);
                            });
                    });
                });
        }

        /// <summary>
        /// Renders a single "Label : Value" line inside an information box.
        /// The label sits in a fixed-width, right-aligned column so the colon
        /// lines up vertically with every other field in the same box,
        /// regardless of how long each label's text is.
        /// </summary>
        /// <param name="column">Column the field is appended to.</param>
        /// <param name="label">Field label, e.g. "Judul Training".</param>
        /// <param name="value">Field value to display after the colon.</param>
        /// <param name="labelWidth">
        /// Fixed width, in points, reserved for the label so colons align.
        /// Should be sized to fit the longest label used within the same box.
        /// </param>
        /// <param name="alignValueRight">
        /// When <see langword="true"/>, the value is right-aligned within its
        /// remaining space instead of hugging the label - used for the
        /// Tanggal / Jam fields so they line up against the right edge.
        /// </param>
        private static void ComposeInfoField(
            ColumnDescriptor column,
            string label,
            string? value,
            float labelWidth,
            bool alignValueRight = false)
        {
            column.Item().Row(row =>
            {
                row.ConstantItem(labelWidth)
                    .AlignRight()
                    .Text($"{label} :");

                var valueContainer = row.RelativeItem().PaddingLeft(4);

                if (alignValueRight)
                {
                    valueContainer = valueContainer.AlignRight();
                }

                valueContainer.Text(value ?? string.Empty);
            });
        }

        /// <summary>
        /// Composes one page's bordered participant table: No, Nama, Departemen
        /// and a two-column Tanda Tangan (signature) area, for exactly
        /// <paramref name="rowCount"/> rows starting at <paramref name="startIndex"/>.
        /// Row numbers are absolute (continuing across pages, e.g. 19-36 on the
        /// second page) rather than restarting at 1. Participants are laid out
        /// two per "band" so each pair shares a signature area tall enough to
        /// sign in, matching the reference spreadsheet.
        /// </summary>
        /// <param name="container">Container this page's table is rendered into.</param>
        /// <param name="participants">The full list of registered participants, in display order.</param>
        /// <param name="startIndex">
        /// Zero-based index into <paramref name="participants"/> of the first
        /// row on this page.
        /// </param>
        /// <param name="rowCount">
        /// Number of rows to render on this page (equal to <see cref="RowsPerPage"/>).
        /// Rows whose index falls beyond <paramref name="participants"/>'s length
        /// are left blank (number and signature box only), so every page -
        /// including the last - is fully filled.
        /// </param>
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
                    columns.ConstantColumn(30);    // No.
                    columns.RelativeColumn(3);     // Nama
                    columns.RelativeColumn(2);     // Departemen
                    columns.RelativeColumn(2);     // Tanda Tangan (kiri)
                    columns.RelativeColumn(2);     // Tanda Tangan (kanan)
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("No.");
                    header.Cell().Element(HeaderCellStyle).Text("Nama");
                    header.Cell().Element(HeaderCellStyle).Text("Departemen");
                    header.Cell().ColumnSpan(2).Element(HeaderCellStyle).Text("Tanda Tangan");
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

                        var participantNo = rowIndex + 1;

                        table.Cell().Element(BodyCellStyle).AlignMiddle().Text(participantNo.ToString());
                        table.Cell().Element(BodyCellStyle).AlignMiddle().Text(participant?.Name ?? string.Empty);
                        table.Cell().Element(BodyCellStyle).AlignMiddle().Text(participant?.DeptName ?? string.Empty);

                        /*
                         * Only the first row of the band adds the signature cells.
                         * They are given RowSpan = bandSize so they stretch across
                         * every row in this band; QuestPDF's table auto-flow then
                         * skips these already-occupied slots for the remaining
                         * row(s) in the band. A small row number is placed in the
                         * top-left corner of each box, matching the reference
                         * spreadsheet, whether or not that slot has a participant
                         * assigned yet.
                         */
                        if (offset == 0)
                        {
                            var firstRowNo = startIndex + bandStart + 1;
                            var secondRowNo = bandSize == 2 ? startIndex + bandStart + 2 : (int?)null;

                            table.Cell()
                                .RowSpan((uint)bandSize)
                                .Element(SignatureCellStyle)
                                .Column(sig =>
                                    sig.Item()
                                        .AlignLeft()
                                        .Text(firstRowNo.ToString())
                                        .FontSize(6)
                                        .FontColor(Colors.Grey.Darken2));

                            table.Cell()
                                .RowSpan((uint)bandSize)
                                .Element(SignatureCellStyle)
                                .Column(sig =>
                                {
                                    if (secondRowNo.HasValue)
                                    {
                                        sig.Item()
                                            .AlignLeft()
                                            .Text(secondRowNo.Value.ToString())
                                            .FontSize(6)
                                            .FontColor(Colors.Grey.Darken2);
                                    }
                                });
                        }
                    }
                }
            });

            static IContainer HeaderCellStyle(IContainer cellContainer) =>
                cellContainer
                    .Background(Colors.Grey.Lighten2)
                    .Border(1)
                    .BorderColor(Colors.Black)
                    .Padding(5)
                    .AlignMiddle()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.Bold());

            static IContainer BodyCellStyle(IContainer cellContainer) =>
                cellContainer
                    .Border(1)
                    .BorderColor(Colors.Black)
                    .Padding(5)
                    .MinHeight(22);

            static IContainer SignatureCellStyle(IContainer cellContainer) =>
                cellContainer
                    .Border(1)
                    .BorderColor(Colors.Black)
                    .Padding(5)
                    .MinHeight(44);
        }
    }
}
