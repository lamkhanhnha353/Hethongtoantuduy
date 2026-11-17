
using System.ComponentModel.DataAnnotations;

namespace ToanTuDuy.DTOs
{
    public class EnrollmentRequest
    {
        [Range(1, int.MaxValue)]
        public int StudentId { get; set; }

        [Range(1, int.MaxValue)]
        public int ClassId { get; set; }
    }
}