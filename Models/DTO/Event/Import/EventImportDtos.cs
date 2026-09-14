namespace Training.Models.DTO.Event.Import
{
    /// <summary>
    /// Satu baris sheet "Events" dari file Import Training Event.
    /// </summary>
    /// <remarks>
    /// <see cref="EventNo"/> adalah key lokal di dalam file Excel saja (bukan EventCode di database -
    /// EventCode selalu di-generate otomatis oleh <c>dbo.usp_TrainingEvent_Create</c> dari
    /// <c>TrainingEventId</c> setelah insert). EventNo dipakai untuk menghubungkan baris di sheet
    /// "Events" dengan baris-baris peserta yang sesuai di sheet "Participants".
    /// </remarks>
    public sealed class EventImportRowDto
    {
        public int EventNo { get; set; }

        public string CourseCode { get; set; } = string.Empty;

        /// <summary>
        /// TIDAK diisi dari Excel - di-resolve dari <c>Master_Courses.CourseName</c> berdasarkan
        /// CourseCode setelah course ditemukan. Property ini dipertahankan (bukan CourseName
        /// langsung) supaya error reporting sebelum course di-resolve tetap konsisten.
        /// </summary>
        public string TrainingTitle { get; set; } = string.Empty;

        /// <summary>Diterima "I"/"E" atau "Internal"/"External", dinormalisasi ke "I"/"E".</summary>
        public string EventType { get; set; } = string.Empty;

        public string CoCode { get; set; } = string.Empty;

        public string ABRV { get; set; } = string.Empty;

        /// <summary>Wajib diisi jika EventType = Internal. Diabaikan jika External.</summary>
        public string? TrainerId { get; set; }

        /// <summary>
        /// Wajib diisi (dari Excel) jika EventType = External.
        /// Jika Internal, kolom ini diabaikan - TrainerName di-resolve otomatis dari
        /// <c>home.dbo.APP_Employees.FullName</c> berdasarkan TrainerId.
        /// </summary>
        public string? TrainerName { get; set; }

        public int? ParticipantQuota { get; set; }

        public string? Venue { get; set; }

        public DateTime? EventStartDate { get; set; }

        public DateTime? EventEndDate { get; set; }

        public int? SessionCount { get; set; }

        public decimal? DurationHours { get; set; }

        public decimal? Budget { get; set; }

        public string? TrainingDescription { get; set; }

        public string? TrainingObjective { get; set; }
    }

    /// <summary>
    /// Satu baris sheet "Participants" dari file Import Training Event.
    /// </summary>
    public sealed class EventParticipantImportRowDto
    {
        /// <summary>EventNo yang menghubungkan baris ini ke salah satu baris di sheet "Events".</summary>
        public int EventNo { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hasil import Training Event + Participant.
    /// </summary>
    public sealed class EventImportResultDto
    {
        public int SuccessCount { get; set; }

        public List<EventImportErrorDto> Errors { get; set; } = new();

        public string Message { get; set; } = string.Empty;

        public bool HasErrors => Errors.Count > 0;
    }

    /// <summary>
    /// Detail error untuk satu EventNo (event batal dibuat - event dan seluruh peserta di baris itu
    /// diproses sebagai satu unit, sesuai transaksi <c>dbo.usp_TrainingEvent_Create</c>).
    /// </summary>
    public sealed class EventImportErrorDto
    {
        public int EventNo { get; set; }

        public string? CourseCode { get; set; }

        public string? TrainingTitle { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
