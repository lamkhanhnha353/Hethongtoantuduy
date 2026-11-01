using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IAttendanceService
    {
        /// <summary>
        /// Lấy thông tin buổi học và danh sách điểm danh toàn bộ học sinh trong lớp.
        /// </summary>
        Task<LessonAttendanceDetailResponse?> GetAttendanceByLessonIdAsync(int lessonId);

        /// <summary>
        /// Điểm danh cho danh sách nhiều học sinh trong một buổi học (batch attendance).
        /// Hỗ trợ cả tạo mới và cập nhật (upsert).
        /// </summary>
        Task<LessonAttendanceDetailResponse> SaveBatchAttendanceAsync(BatchAttendanceRequest request);

        /// <summary>
        /// Điểm danh / cập nhật điểm danh cho một học sinh lẻ trong buổi học.
        /// </summary>
        Task<StudentAttendanceItemResponse> SaveSingleAttendanceAsync(int lessonId, AttendanceItemDto dto);

        /// <summary>
        /// Lấy lịch sử điểm danh của một học sinh theo lớp học hoặc tất cả các lớp.
        /// </summary>
        Task<IEnumerable<StudentAttendanceHistoryResponse>> GetStudentAttendanceHistoryAsync(int studentId, int? classId = null);
    }
}
