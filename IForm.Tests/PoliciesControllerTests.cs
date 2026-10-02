using IForm.Web.Controllers;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IForm.Tests;

public class PoliciesControllerTests
{
    [Fact]
    public async Task Index_ReturnsPolicies()
    {
        var (db, connection) = TestData.CreateDbContext();
        try
        {
            var user = TestData.User();
            await TestData.SeedUsersAsync(db, user);
            db.Policies.Add(new Policy { Title = "Policy", Status = PolicyStatus.Draft });
            await db.SaveChangesAsync();
            var controller = new PoliciesController(db).SetUser(TestData.Principal(user, "Admin"));
            var result = await controller.Index(null, null);
            var model = Assert.IsAssignableFrom<PolicyListViewModel>(Assert.IsType<ViewResult>(result).Model);
            Assert.Single(model.Policies);
        }
        finally
        {
            await db.DisposeAsync();
            connection.Dispose();
        }
    }
}
