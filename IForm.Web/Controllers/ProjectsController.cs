using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Web.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    public IActionResult Index() => View();
}
