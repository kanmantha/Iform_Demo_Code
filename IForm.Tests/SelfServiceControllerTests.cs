using IForm.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Tests;

public class SelfServiceControllerTests
{
    [Fact]
    public void Index_ReturnsView()
    {
        var controller = new SelfServiceController().SetUser(TestData.Principal(TestData.User()));
        var result = controller.Index();
        Assert.IsType<ViewResult>(result);
    }
}
