
using System;
using System.Collections.Generic;

namespace ToanTuDuy.DTOs
{
    public class CourseResponse
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Trả về kèm danh sách các Level của khóa học
        public List<CourseLevelResponse> CourseLevels { get; set; } = new List<CourseLevelResponse>();
    }

    public class CourseLevelResponse
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Fee { get; set; }
        public int TotalSessions { get; set; }
        public int OrderIndex { get; set; }
        public string Status { get; set; }

        // Các trường bổ sung để map dữ liệu từ Course cha và JSON ra Mobile App
        public string? CourseName { get; set; }
        public string? Description { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public double Rating { get; set; }
        public int Reviews { get; set; }
        public string? VideoUrl { get; set; }
        public List<string>? Benefits { get; set; }
        public List<string>? FacilityImages { get; set; }
        public object? Syllabus { get; set; }
        public object? Faqs { get; set; }
    }
}