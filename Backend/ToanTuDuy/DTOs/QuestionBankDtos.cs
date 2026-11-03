using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    // ==========================================
    // DTOs cho Ngân hàng câu hỏi (QUESTION_BANK)
    // ==========================================

    public class QuestionBankRequest
    {
        [Required(ErrorMessage = "Khóa học (CourseId) không được để trống")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Tên ngân hàng câu hỏi không được để trống")]
        [MaxLength(200, ErrorMessage = "Tên ngân hàng câu hỏi tối đa 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }

    public class QuestionBankResponse
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TotalQuestions { get; set; }
    }

    public class QuestionBankDetailResponse : QuestionBankResponse
    {
        public List<QuestionResponse> Questions { get; set; } = new List<QuestionResponse>();
    }

    // ==========================================
    // DTOs cho Câu hỏi (QUESTION)
    // ==========================================

    public class QuestionRequest
    {
        [Required(ErrorMessage = "Ngân hàng câu hỏi (BankId) không được để trống")]
        public int BankId { get; set; }

        [Required(ErrorMessage = "Loại câu hỏi (Type) không được để trống")]
        public string Type { get; set; } = "MULTIPLE_CHOICE"; // MULTIPLE_CHOICE, FILL_IN, ESSAY

        [Required(ErrorMessage = "Nội dung câu hỏi không được để trống")]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Độ khó không được để trống")]
        public string Difficulty { get; set; } = "EASY"; // EASY, MEDIUM, HARD

        [Required(ErrorMessage = "Đáp án không được để trống")]
        public string Answer { get; set; } = string.Empty;
    }

    public class QuestionResponse
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
