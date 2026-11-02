using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IAssignmentService
    {
        /// <summary>
        /// Tạo bài kiểm tra cho một buổi học, kèm danh sách câu hỏi và tự động giao cho cả lớp hoặc học sinh chỉ định.
        /// </summary>
        Task<AssignmentDetailResponse> CreateAssignmentAsync(CreateAssignmentRequest request);

        /// <summary>
        /// Nhân bản / Đính kèm bài kiểm tra có sẵn sang một buổi học (Lesson) khác.
        /// </summary>
        Task<AssignmentDetailResponse> CloneAssignmentToLessonAsync(CloneAssignmentRequest request);

        /// <summary>
        /// Lấy danh sách bài kiểm tra của một buổi học.
        /// </summary>
        Task<IEnumerable<AssignmentResponse>> GetAssignmentsByLessonIdAsync(int lessonId);

        /// <summary>
        /// Lấy chi tiết bài kiểm tra kèm danh sách câu hỏi và danh sách học sinh được giao bài.
        /// </summary>
        Task<AssignmentDetailResponse?> GetAssignmentByIdAsync(int id);

        /// <summary>
        /// Cập nhật thông tin bài kiểm tra và cơ cấu câu hỏi.
        /// </summary>
        Task<bool> UpdateAssignmentAsync(int id, UpdateAssignmentRequest request);

        /// <summary>
        /// Xóa bài kiểm tra.
        /// </summary>
        Task<bool> DeleteAssignmentAsync(int id);

        /// <summary>
        /// Đính kèm / Giao bài kiểm tra riêng lẻ cho các học sinh cụ thể trong lớp.
        /// </summary>
        Task<AssignmentDetailResponse> AssignStudentsAsync(int assignmentId, List<int> studentIds);

        /// <summary>
        /// Hủy giao bài kiểm tra cho một học sinh lẻ.
        /// </summary>
        Task<bool> UnassignStudentAsync(int assignmentId, int studentId);

        /// <summary>
        /// Lấy danh sách bài kiểm tra được giao của một học sinh.
        /// </summary>
        Task<IEnumerable<AssignmentResponse>> GetAssignmentsByStudentAsync(int studentId, int? lessonId = null);
    }
}
