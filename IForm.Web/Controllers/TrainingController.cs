using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IForm.Web.Controllers;

[Authorize]
public class TrainingController : Controller
{
    public IActionResult Index() => View();
}
