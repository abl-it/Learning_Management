namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Ringkasan satu Course Group untuk tampilan daftar.
    /// </summary>
    public class CourseGroupListItemDto
    {
        /// <summary>Identifier Course Group.</summary>
        public int CourseGroupId { get; set; }

        /// <summary>Nama Course Group.</summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>Deskripsi (opsional).</summary>
        public string? Description { get; set; }

        /// <summary>Status aktif.</summary>
        public bool IsActive { get; set; }

        /// <summary>Jumlah course di dalam group.</summary>
        public int CourseCount { get; set; }

        /// <summary>Dibuat oleh (employee code).</summary>
        public string? CreatedBy { get; set; }

        /// <summary>Tanggal dibuat (local time).</summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>Diubah terakhir oleh (employee code).</summary>
        public string? ModifiedBy { get; set; }

        /// <summary>Tanggal diubah terakhir (local time).</summary>
        public DateTime? ModifiedDate { get; set; }
    }
}
