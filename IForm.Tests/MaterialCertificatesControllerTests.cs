using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class MaterialCertificatesControllerTests
{
    [Fact]
    public async Task Index_ReturnsAllCertificates_WhenNoFilters()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.MaterialCertificates.AddRange(
                TestData.Certificate(1, "MTC-01", "Jyothi Spectro", user.Id),
                TestData.Certificate(2, "MTC-02", "Metals & Chemical Analysis", user.Id));
            await db.SaveChangesAsync();

            var controller = new MaterialCertificatesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<MaterialCertificateListViewModel>(view.Model);
            Assert.Equal(2, model.TotalCount);
            Assert.Equal(2, model.Certificates.Count);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Index_SearchFiltersBySupplierAndAlloy()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            db.MaterialCertificates.AddRange(
                TestData.Certificate(1, "MTC-01", "Jyothi Spectro", user.Id),
                TestData.Certificate(2, "MTC-02", "Metals & Chemical Analysis", user.Id));
            await db.SaveChangesAsync();

            var controller = new MaterialCertificatesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Index(search: "Jyothi");

            var model = Assert.IsAssignableFrom<MaterialCertificateListViewModel>(Assert.IsType<ViewResult>(result).Model);
            var only = Assert.Single(model.Certificates);
            Assert.Equal("MTC-01", only.CertificateNumber);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_GeneratesSequentialNumber_AndPersistsWithoutFile()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.MaterialCertificates.Add(TestData.Certificate(3, "MTC-03", "Jyothi Spectro", user.Id));
            await db.SaveChangesAsync();

            var controller = new MaterialCertificatesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Create(new MaterialCertificateFormViewModel
            {
                Supplier = "Metals & Chemical Analysis",
                SupplierReportNo = "R22u158526",
                WorkOrderNo = "JSA.26-04035",
                Alloy = "Aluminium 6063",
                TestMethod = "ASTM B221 / B557",
                ReportDate = DateTime.UtcNow.AddDays(-5),
                ValidUntil = DateTime.UtcNow.AddDays(180)
            }, file: null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MaterialCertificatesController.Details), redirect.ActionName);

            var saved = Assert.Single(db.MaterialCertificates.Where(c => c.CertificateNumber == "MTC-04"));
            Assert.Equal("Metals & Chemical Analysis", saved.Supplier);
            Assert.Equal("JSA.26-04035", saved.WorkOrderNo);
            Assert.Null(saved.FilePath);
            Assert.Single(db.AuditLogs);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Create_InvalidModel_ReturnsView_AndDoesNotPersist()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new MaterialCertificatesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var model = new MaterialCertificateFormViewModel { Supplier = "" };
            controller.ModelState.AddModelError(nameof(model.Supplier), "Required");

            var result = await controller.Create(model, file: null);

            Assert.IsType<ViewResult>(result);
            Assert.Empty(db.MaterialCertificates);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Edit_UpdatesFields_AndWritesAudit()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.MaterialCertificates.Add(TestData.Certificate(1, "MTC-01", "Jyothi Spectro", user.Id));
            await db.SaveChangesAsync();

            var controller = new MaterialCertificatesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Edit(new MaterialCertificateFormViewModel
            {
                Id = 1,
                Supplier = "Updated Lab",
                SupplierReportNo = "R22u999",
                WorkOrderNo = "JSA.26-99999",
                Alloy = "Aluminium 6061",
                TestMethod = "ASTM B557"
            }, file: null);

            Assert.IsType<RedirectToActionResult>(result);

            var saved = await db.MaterialCertificates.FindAsync(1);
            Assert.NotNull(saved);
            Assert.Equal("Updated Lab", saved.Supplier);
            Assert.Equal("Aluminium 6061", saved.Alloy);
            Assert.NotNull(saved.UpdatedAt);
            Assert.Contains(db.AuditLogs, a => a.EntityType == "MaterialCertificate");
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task Details_MissingCertificate_ReturnsNotFound()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);

            var controller = new MaterialCertificatesController(db, TestData.UserManager(user), new TestWebHostEnvironment())
                .SetUser(TestData.Principal(user));

            var result = await controller.Details(999);

            Assert.IsType<NotFoundResult>(result);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
