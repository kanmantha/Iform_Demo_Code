using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Web.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class PerformanceController : Controller
{
    public IActionResult Index() => View();
}
