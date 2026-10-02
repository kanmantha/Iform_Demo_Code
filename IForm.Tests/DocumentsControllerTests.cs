using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class DocumentsControllerTests
{
    [Fact]
    public async Task Index_ReturnsDocuments()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Documents.Add(new Document { Title = "Policy", Category = DocumentCategory.Policy, Visibility = DocumentVisibility.Internal });
            await db.SaveChangesAsync();
            var controller = new DocumentsController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null, null);
            var model = Assert.IsAssignableFrom<DocumentListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Documents);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
