namespace Training.Models.DTO.TrainingMatrix
{
    /// <summary>
    /// Satu baris daftar Training Matrix: satu Department (dikelompokkan menurut ABRV),
    /// lengkap dengan status dokumen matrix bila sudah dibuat.
    /// </summary>
    public class MatrixListItemDto
    {
        /// <summary>Kunci Department (APP_Departments.ABRV).</summary>
        public string DepartmentAbrv { get; set; } = string.Empty;

        /// <summary>Kode department yang tergabung pada ABRV ini, mis. "C300, D300".</summary>
        public string DepartmentCodes { get; set; } = string.Empty;

        /// <summary>Jumlah Sub Department aktif di bawah ABRV ini.</summary>
        public int SubDepartmentCount { get; set; }

        /// <summary>Identifier dokumen matrix; null bila belum dibuat.</summary>
        public int? MatrixId { get; set; }

        /// <summary>Status workflow; null bila belum dibuat.</summary>
        public string? Status { get; set; }

        /// <summary>DocStatus (0 draft, 1 pending, 2 approved); null bila belum dibuat.</summary>
        public int? DocStatus { get; set; }

        /// <summary>Jumlah baris detail (Sub Dept x Layer x Course).</summary>
        public int DetailCount { get; set; }

        /// <summary>Tanggal dibuat.</summary>
        public DateTime? CreatedDate { get; set; }

        /// <summary>Pengubah terakhir.</summary>
        public string? ModifiedBy { get; set; }

        /// <summary>Tanggal ubah terakhir.</summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>Identifier Change Request yang sedang aktif; null bila tidak ada.</summary>
        public int? ActiveChangeRequestId { get; set; }

        /// <summary>Status Change Request aktif.</summary>
        public string? ActiveChangeRequestStatus { get; set; }
    }
}
