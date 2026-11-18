using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class EnrollmentAndVnPayTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(AuthenticatedActor Admin, AuthenticatedActor Teacher, AuthenticatedActor Parent, int StudentId, int LevelId, int Class1Id, int Class2Id)> SetupCatalogAsync(int maxCapacity = 5)
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Huy" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        var course = (await (await admin.Client.PostAsJsonAsync("/api/courses", new CourseRequest
        {
            Code = "PAY-C1",
            Name = "Toán Tư Duy Pay",
            MinAge = 6,
            MaxAge = 12,
            Status = "ACTIVE"
        })).Content.ReadFromJsonAsync<CourseResponse>())!;

        var level = (await (await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = course.Id,
            Code = "PAY-L1",
            Name = "Level Pay",
            Fee = 1500000m,
            TotalSessions = 8,
            OrderIndex = 1,
            Status = "ACTIVE"
        })).Content.ReadFromJsonAsync<CourseLevelResponse>())!;

        var c1 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher.ProfileId,
            Name = "Lớp Pay 1",
            MaxCapacity = maxCapacity,
            StartDate = DateTime.UtcNow.Date.AddDays(3),
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        var c2 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher.ProfileId,
            Name = "Lớp Pay 2",
            MaxCapacity = maxCapacity,
            StartDate = DateTime.UtcNow.Date.AddDays(3),
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        return (admin, teacher, parent, student.Id, level.Id, c1.Id, c2.Id);
    }

    [Fact]
    public async Task Enrollment_PreventsDuplicateLevel_And_CancelReleasesSlot()
    {
        var (_, _, parent, studentId, levelId, class1Id, class2Id) = await SetupCatalogAsync();
        var otherParent = await AuthTestHelper.RegisterParentAsync(Factory);

        // Ownership check: otherParent cannot enroll parent's student
        var unauthEnroll = await otherParent.Client.PostAsJsonAsync("/api/enrollment/register", new EnrollmentRequest
        {
            StudentId = studentId,
            ClassId = class1Id
        });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthEnroll.StatusCode);

        // Parent enrolls in Class 1
        var enroll1 = await parent.Client.PostAsJsonAsync("/api/enrollment/register", new EnrollmentRequest
        {
            StudentId = studentId,
            ClassId = class1Id
        });
        Assert.Equal(HttpStatusCode.OK, enroll1.StatusCode);
        using var doc = JsonDocument.Parse(await enroll1.Content.ReadAsStringAsync());
        var invoiceId = doc.RootElement.GetProperty("invoiceId").GetInt32();

        // Cannot enroll same student in Class 2 of same CourseLevel while PENDING
        var dupLevel = await parent.Client.PostAsJsonAsync("/api/enrollment/register", new EnrollmentRequest
        {
            StudentId = studentId,
            ClassId = class2Id
        });
        Assert.Equal(HttpStatusCode.BadRequest, dupLevel.StatusCode);

        // Cancel invoice -> releases slot -> can re-enroll in Class 2
        var cancelResp = await parent.Client.PostAsync($"/api/invoice/cancel/{invoiceId}", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        var enroll2 = await parent.Client.PostAsJsonAsync("/api/enrollment/register", new EnrollmentRequest
        {
            StudentId = studentId,
            ClassId = class2Id
        });
        Assert.Equal(HttpStatusCode.OK, enroll2.StatusCode);

        var regs = (await parent.Client.GetFromJsonAsync<List<EnrollmentRegistrationResponse>>(
            $"/api/enrollment/course-level/{levelId}/registrations"))!;
        var reg = Assert.Single(regs);
        Assert.Equal("PENDING", reg.Status);
        Assert.Equal(class2Id, reg.ClassId);
    }

    [Fact]
    public async Task VnPayFlow_ValidatesSignatureAmountFailureSuccessIdempotency_AndReturnRedirect()
    {
        var (admin, _, parent, studentId, _, class1Id, _) = await SetupCatalogAsync();

        var enroll = await parent.Client.PostAsJsonAsync("/api/enrollment/register", new EnrollmentRequest
        {
            StudentId = studentId,
            ClassId = class1Id
        });
        enroll.EnsureSuccessStatusCode();
        using var enrollDoc = JsonDocument.Parse(await enroll.Content.ReadAsStringAsync());
        var invoiceId = enrollDoc.RootElement.GetProperty("invoiceId").GetInt32();

        // Create Payment URL -> extracts vnp_TxnRef
        var urlResp = await parent.Client.PostAsync($"/api/payment/create-url/{invoiceId}", null);
        Assert.Equal(HttpStatusCode.OK, urlResp.StatusCode);
        using var urlDoc = JsonDocument.Parse(await urlResp.Content.ReadAsStringAsync());
        var paymentUrl = urlDoc.RootElement.GetProperty("paymentUrl").GetString()!;
        var parsedQuery = QueryHelpers.ParseQuery(new Uri(paymentUrl).Query);
        var txnRef = parsedQuery["vnp_TxnRef"].ToString();
        Assert.StartsWith($"INV{invoiceId}_", txnRef);

        using var anon = Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // 1. Invalid signature -> 97
        var badSigQuery = VnPayTestSigner.BuildSignedQuery(TestWebApplicationFactory.VnPayHashSecret, txnRef, 1500000) + "tamper";
        var badSig = await anon.GetFromJsonAsync<VnPayIpnResponse>($"/api/payment/vnpay-ipn?{badSigQuery}");
        Assert.Equal("97", badSig!.RspCode);

        // 2. Wrong amount -> 04
        var wrongAmtQuery = VnPayTestSigner.BuildSignedQuery(TestWebApplicationFactory.VnPayHashSecret, txnRef, 1000);
        var wrongAmt = await anon.GetFromJsonAsync<VnPayIpnResponse>($"/api/payment/vnpay-ipn?{wrongAmtQuery}");
        Assert.Equal("04", wrongAmt!.RspCode);

        // 3. Failed transaction (code=24) -> Invoice=FAILED, ClassStudent remains PENDING
        var failQuery = VnPayTestSigner.BuildSignedQuery(TestWebApplicationFactory.VnPayHashSecret, txnRef, 1500000, responseCode: "24", transactionStatus: "02");
        var failIpn = await anon.GetFromJsonAsync<VnPayIpnResponse>($"/api/payment/vnpay-ipn?{failQuery}");
        Assert.Equal("00", failIpn!.RspCode);

        // 4. Retry create-url restores FAILED -> PENDING and generates new TxnRef
        var retryUrlResp = await parent.Client.PostAsync($"/api/payment/create-url/{invoiceId}", null);
        Assert.Equal(HttpStatusCode.OK, retryUrlResp.StatusCode);
        using var retryDoc = JsonDocument.Parse(await retryUrlResp.Content.ReadAsStringAsync());
        var newTxnRef = QueryHelpers.ParseQuery(new Uri(retryDoc.RootElement.GetProperty("paymentUrl").GetString()!).Query)["vnp_TxnRef"].ToString();
        Assert.NotEqual(txnRef, newTxnRef);

        // 5. Success IPN (00/00) -> Invoice=PAID, ClassStudent=ACTIVE
        var okQuery = VnPayTestSigner.BuildSignedQuery(TestWebApplicationFactory.VnPayHashSecret, newTxnRef, 1500000, "00", "00", "99887766");
        var okIpn = await anon.GetFromJsonAsync<VnPayIpnResponse>($"/api/payment/vnpay-ipn?{okQuery}");
        Assert.Equal("00", okIpn!.RspCode);

        // 6. Duplicate IPN -> 02 (idempotent)
        var dupIpn = await anon.GetFromJsonAsync<VnPayIpnResponse>($"/api/payment/vnpay-ipn?{okQuery}");
        Assert.Equal("02", dupIpn!.RspCode);

        // 7. Return endpoint redirects 302 to MobileDeepLink?code=00
        var retResp = await anon.GetAsync($"/api/payment/vnpay-return?{okQuery}");
        Assert.Equal(HttpStatusCode.Redirect, retResp.StatusCode);
        Assert.Equal("toantuduy://payment-result?code=00", retResp.Headers.Location?.OriginalString);

        // 8. Admin paged invoices KPI reflects PAID invoice
        var adminInvoices = (await admin.Client.GetFromJsonAsync<AdminInvoicePagedResponse>("/api/admin/invoices/paged?status=PAID"))!;
        Assert.Equal(1, adminInvoices.PaidCount);
        Assert.Equal(1500000L, adminInvoices.PaidTotalAmount);
    }

    [Fact]
    public async Task CreatePaymentUrl_Returns409Conflict_WhenClassReachedMaxCapacity()
    {
        var (admin, _, parent, studentId, _, class1Id, _) = await SetupCatalogAsync(maxCapacity: 1);
        var parent2 = await AuthTestHelper.RegisterParentAsync(Factory);
        var student2 = (await (await parent2.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Full" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        // Parent 1 enrolls and waits
        var enroll = await parent.Client.PostAsJsonAsync("/api/enrollment/register", new EnrollmentRequest
        {
            StudentId = studentId,
            ClassId = class1Id
        });
        using var doc = JsonDocument.Parse(await enroll.Content.ReadAsStringAsync());
        var invoiceId = doc.RootElement.GetProperty("invoiceId").GetInt32();

        // Admin manually fills the 1 slot with student2 (ACTIVE)
        var addResp = await admin.Client.PostAsJsonAsync($"/api/classes/{class1Id}/students", new AddClassStudentRequest
        {
            StudentId = student2.Id
        });
        addResp.EnsureSuccessStatusCode();

        // Parent 1 tries to create payment URL -> 409 Conflict
        var payUrl = await parent.Client.PostAsync($"/api/payment/create-url/{invoiceId}", null);
        Assert.Equal(HttpStatusCode.Conflict, payUrl.StatusCode);
    }
}
