using System.Net;
using System.Security.Cryptography;
using System.Text;
using ToanTuDuy.Utils;

namespace ToanTuDuy.Tests.Infrastructure;

public static class VnPayTestSigner
{
    public static string BuildSignedQuery(
        string hashSecret,
        string txnRef,
        long amountVnd,
        string responseCode = "00",
        string transactionStatus = "00",
        string transactionNo = "14000001")
    {
        var data = new SortedList<string, string>(new VnPayCompare())
        {
            ["vnp_Amount"] = (amountVnd * 100).ToString(),
            ["vnp_BankCode"] = "NCB",
            ["vnp_Command"] = "pay",
            ["vnp_CurrCode"] = "VND",
            ["vnp_OrderInfo"] = "Thanh toan test",
            ["vnp_PayDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TmnCode"] = "TESTTMN01",
            ["vnp_TransactionNo"] = transactionNo,
            ["vnp_TransactionStatus"] = transactionStatus,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_Version"] = "2.1.0"
        };

        var sb = new StringBuilder();
        foreach (var kv in data)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                sb.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
        }

        var raw = sb.ToString().TrimEnd('&');
        var hash = ComputeHmacSha512(hashSecret, raw);
        return $"{raw}&vnp_SecureHash={hash}";
    }

    public static string ComputeHmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
