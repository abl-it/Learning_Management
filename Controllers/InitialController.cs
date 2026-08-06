using AspNetCoreGeneratedDocument;
using Azure;
using Training.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Net;
using Training.Filters;
using Training.Models;
using Training.Services.IServices;

namespace Training.Controllers
{
    [ServiceFilter(typeof(ProfileAttribute))]
    [Route("api/[controller]")]
    [ApiController]
    public class InitialController : ControllerBase
    {
        protected ApiResponse _response;
        private readonly IInitialService _initialService;
        private readonly ICurrentUserService _emp;

        public InitialController(IInitialService initialService, ICurrentUserService emp)
        {
            _initialService = initialService;
            _emp = emp;
            _response = new ApiResponse();
        }

        //[HttpPost("init_yearly_plan")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public async Task<ActionResult<ApiResponse>> InitialYearlyPlan(int year)
        //{
        //    try
        //    {
        //        int? result = await _initialService.InitYearlyPlanAsync(year, _emp.CurrentEmployee.EmployeeCode);
        //        if (result == null)
        //        {
        //            _response.Success = false;
        //            _response.Message = "Data not found";
        //            return NotFound(_response);
        //        }

        //        _response.Data = result;
        //        _response.Success = true;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.Success = false;
        //        _response.Message = ex.Message;

        //    }
        //    return _response;

        //}
        [HttpPost("init_yearly_plan")] // Full route: /api/Initial/init_yearly_plan
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> InitialYearlyPlan([FromBody] int year) // Mengharapkan objek JSON InitialYearlyPlanRequest
        {
            try
            {
                if (year == 0)
                {
                    _response.Success = false;
                    _response.Message = "Permintaan tidak valid: Tahun tidak boleh kosong.";
                    return BadRequest(_response); // Mengembalikan 400 Bad Request
                }

                // Pastikan CurrentEmployee dan EmployeeCode tersedia
                // Anda mungkin ingin menanganinya lebih awal dengan filter autentikasi/otorisasi
                string employeeCode = _emp.CurrentEmployee?.EmployeeCode;
                if (string.IsNullOrEmpty(employeeCode))
                {
                    _response.Success = false;
                    _response.Message = "Kode karyawan tidak ditemukan. Pastikan Anda sudah login.";
                    // Mengembalikan 401 Unauthorized atau 403 Forbidden mungkin lebih tepat
                    return StatusCode(StatusCodes.Status401Unauthorized, _response);
                }

                int? result = await _initialService.InitYearlyPlanAsync(year, employeeCode);
                if (result == null)
                {
                    // Ini adalah skenario 'not found' atau 'no change' dari service layer
                    // Lebih baik mengembalikan 200 OK dengan Success=false untuk logika bisnis ini
                    _response.Success = false;
                    _response.Message = $"Inisialisasi rencana pelatihan tahun {year} gagal atau tidak ada data yang diproses. Mohon periksa status data.";
                    return Ok(_response); // Mengembalikan 200 OK dengan status kegagalan bisnis
                }

                _response.Data = result;
                _response.Success = true;
                _response.Message = $"Rencana pelatihan tahun {year} berhasil diinisialisasi untuk {result} departemen.";
                return Ok(_response); // Mengembalikan 200 OK dengan status sukses
            }
            catch (Exception ex)
            {
                // Catat exception ke log (misalnya dengan ILogger)
                Console.WriteLine($"Error during InitialYearlyPlan: {ex.Message}"); // Contoh logging sederhana
                _response.Success = false;
                _response.Message = $"Terjadi kesalahan server saat inisialisasi: {ex.Message}";
                return StatusCode(StatusCodes.Status500InternalServerError, _response); // Mengembalikan 500 Internal Server Error
            }
        }


    }
}
