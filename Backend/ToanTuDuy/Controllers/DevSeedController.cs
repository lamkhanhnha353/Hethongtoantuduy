using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToanTuDuy.Data;

namespace ToanTuDuy.Controllers
{
    [ApiController]
    [Route("api/dev/seed-data")]
    public class DevSeedController : ControllerBase
    {
        private readonly BogusDataSeeder _seeder;

        public DevSeedController(BogusDataSeeder seeder)
        {
            _seeder = seeder;
        }

        /// <summary>
        /// Nạp dữ liệu mẫu Bogus trực tiếp vào Database (Mặc định 1000 dòng/bảng).
        /// </summary>
        /// <param name="rowsPerTable">Số dòng sinh trên mỗi bảng (mặc định 1000)</param>
        /// <param name="cleanExisting">Có xóa dữ liệu cũ trước khi nạp hay không (mặc định true)</param>
        [HttpPost("bogus")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedBogusData([FromQuery] int rowsPerTable = 1000, [FromQuery] bool cleanExisting = true)
        {
            if (rowsPerTable <= 0 || rowsPerTable > 10000)
            {
                return BadRequest(new { message = "rowsPerTable phải nằm trong khoảng từ 1 đến 10,000." });
            }

            var result = await _seeder.SeedAsync(rowsPerTable, cleanExisting);

            return Ok(new
            {
                message = $"Đã nạp thành công dữ liệu mẫu Bogus với quy mô ~{rowsPerTable} dòng/bảng!",
                elapsedSeconds = result.ElapsedSeconds,
                totalTables = result.TableCounts.Count,
                tableCounts = result.TableCounts
            });
        }

        /// <summary>
        /// Xuất dữ liệu mẫu Bogus ra file SQL script (mặc định lưu tại thư mục gốc backend).
        /// </summary>
        [HttpPost("bogus/export-sql")]
        [AllowAnonymous]
        public async Task<IActionResult> ExportBogusSql([FromQuery] int rowsPerTable = 1000, [FromQuery] string? fileName = null)
        {
            if (rowsPerTable <= 0 || rowsPerTable > 10000)
            {
                return BadRequest(new { message = "rowsPerTable phải nằm trong khoảng từ 1 đến 10,000." });
            }

            string targetFileName = string.IsNullOrWhiteSpace(fileName) ? $"DataSeed_Bogus_{rowsPerTable}.sql" : fileName;
            string outputPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, targetFileName);

            await _seeder.GenerateSqlScriptAsync(outputPath, rowsPerTable);

            return Ok(new
            {
                message = "Xuất script SQL thành công!",
                filePath = outputPath,
                rowsPerTable = rowsPerTable
            });
        }
    }
}
