using System;
using System.Text.Json.Serialization;

namespace ToanTuDuy.DTOs
{
    public class EnrollmentRegistrationResponse
    {
        public int? InvoiceId { get; set; }
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class InvoiceHistoryResponse
    {
        public int InvoiceId { get; set; }
        public int Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string TxnRef { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public int ClassId { get; set; }
        public int CourseLevelId { get; set; }
        public string CourseLevelName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
    }

    public class AdminInvoiceResponse
    {
        public int InvoiceId { get; set; }
        public string TxnRef { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public string? ParentEmail { get; set; }
        public string? ParentPhone { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CourseLevelName { get; set; } = string.Empty;
    }

    public class VnPayIpnResponse
    {
        [JsonPropertyName("RspCode")]
        public string RspCode { get; set; } = string.Empty;

        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;
    }
}
