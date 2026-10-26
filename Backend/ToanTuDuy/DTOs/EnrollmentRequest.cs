
namespace ToanTuDuy.DTOs
{
    public class EnrollmentRequest
    {
        public int ParentId { get; set; }
        public int StudentId { get; set; }
        public int ClassId { get; set; }

        // Tạm thời truyền số tiền từ ngoài vào để test luồng. 
        // Sau này có thể truy vấn trực tiếp thuộc tính Price của bảng Course/Class.
        public int Amount { get; set; }
    }
}