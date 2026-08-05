using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;
using Moq;

namespace IForm.Tests;

internal static class ControllerTestExtensions
{
    public static T SetUser<T>(this T controller, ClaimsPrincipal principal) where T : Controller
    {
        var httpContext = new DefaultHttpContext { User = principal };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        return controller;
    }

    public static T Get<T>(this JsonResult result, string propertyName)
    {
        Assert.NotNull(result.Value);
        var property = result.Value!.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return (T)property!.GetValue(result.Value)!;
    }
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";

    public string ApplicationName { get; set; } = "IForm.Web";

    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "iform-tests", "wwwroot");

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
