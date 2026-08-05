using IForm.Web.Data;
using IForm.Web.Models;
using IForm.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Controllers;

[Authorize]
public class EotController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public EotController(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        EotCategory? category = null,
        EotScenario? scenario = null,
        EotSubmissionStatus? submissionStatus = null,
        EotClientApproval? clientApproval = null,
        int page = 1)
    {
        const int pageSize = 10;

        var query = _context.EotRequests
            .AsNoTracking()
            .Include(e => e.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(e =>
                e.EotNumber.Contains(search) ||
                e.Project.Contains(search) ||
                (e.Client != null && e.Client.Contains(search)) ||
                (e.Reference != null && e.Reference.Contains(search)) ||
                e.Reason.Contains(search));
        }

        if (category.HasValue) query = query.Where(e => e.Category == category.Value);
        if (scenario.HasValue) query = query.Where(e => e.Scenario == scenario.Value);
        if (submissionStatus.HasValue) query = query.Where(e => e.SubmissionStatus == submissionStatus.Value);
        if (clientApproval.HasValue) query = query.Where(e => e.ClientApproval == clientApproval.Value);

        query = query.OrderByDescending(e => e.CreatedAt);

        page = Math.Max(1, page);
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var eotRequests = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new EotListViewModel
        {
            Search = search,
            Category = category,
            Scenario = scenario,
            SubmissionStatus = submissionStatus,
            ClientApproval = clientApproval,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            EotRequests = eotRequests,
            CategoryOptions = CategoryOptions(category),
            ScenarioOptions = ScenarioOptions(scenario),
            SubmissionStatusOptions = SubmissionStatusOptions(submissionStatus),
            ClientApprovalOptions = ClientApprovalOptions(clientApproval)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var eot = await _context.EotRequests
            .AsNoTracking()
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eot is null)
        {
            return NotFound();
        }

        var model = new EotDetailViewModel
        {
            EotRequest = eot,
            CanEdit = User.IsInRole(DbSeeder.AdminRole) || User.IsInRole(DbSeeder.ManagerRole)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EotFormViewModel
        {
            FinancialYear = CurrentFinancialYear(),
            CategoryOptions = CategoryOptions(),
            ScenarioOptions = ScenarioOptions(),
            ChangeProposedByOptions = ChangeProposedByOptions(),
            SubmissionStatusOptions = SubmissionStatusOptions(),
            ClientApprovalOptions = ClientApprovalOptions(),
            ProjectOptions = await ProjectOptionsAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EotFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReloadCreateAsync(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var nextNumber = (await _context.EotRequests.MaxAsync(e => (int?)e.Id)) + 1 ?? 1;

        var eot = new EotRequest
        {
            EotNumber = $"EOT-{nextNumber:D2}",
            Project = model.Project.Trim(),
            Client = model.Client?.Trim(),
            FinancialYear = model.FinancialYear?.Trim(),
            RevisionNumber = model.RevisionNumber,
            Category = model.Category,
            Scenario = model.Scenario,
            ChangeProposedBy = model.ChangeProposedBy,
            Reference = model.Reference?.Trim(),
            Reason = model.Reason.Trim(),
            SpaDate = ToUtcOrNull(model.SpaDate),
            DesignRevisionDate = ToUtcOrNull(model.DesignRevisionDate),
            EstimatedTimeImpactDays = model.EstimatedTimeImpactDays,
            EstimatedCostImpact = model.EstimatedCostImpact,
            OriginalApprovedScope = model.OriginalApprovedScope?.Trim(),
            RevisedScope = model.RevisedScope?.Trim(),
            ScopeAddition = model.ScopeAddition,
            ScopeReduction = model.ScopeReduction,
            DelayDays = model.DelayDays,
            CostEscalation = model.CostEscalation?.Trim(),
            SubmissionStatus = model.SubmissionStatus,
            ClientApproval = model.ClientApproval,
            Remarks = model.Remarks?.Trim(),
            HasApprovedDrawings = model.HasApprovedDrawings,
            HasRevisedDrawings = model.HasRevisedDrawings,
            HasClientInstructions = model.HasClientInstructions,
            HasDelayAnalysis = model.HasDelayAnalysis,
            HasScopeVariationStatement = model.HasScopeVariationStatement,
            HasProgressReport = model.HasProgressReport,
            HasConsultantCorrespondence = model.HasConsultantCorrespondence,
            HasSupportingDocuments = model.HasSupportingDocuments,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.EotRequests.Add(eot);
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = user.Id,
            UserName = user.FullName,
            Action = "Create",
            EntityType = "EotRequest",
            EntityId = eot.EotNumber,
            Details = $"EOT {eot.EotNumber} created for {eot.Project}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["Success"] = $"EOT {eot.EotNumber} created for {eot.Project}.";
        return RedirectToAction(nameof(Details), new { id = eot.Id });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(int id)
    {
        var eot = await _context.EotRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eot is null)
        {
            return NotFound();
        }

        var model = new EotFormViewModel
        {
            Id = eot.Id,
            EotNumber = eot.EotNumber,
            Project = eot.Project,
            Client = eot.Client,
            FinancialYear = eot.FinancialYear,
            RevisionNumber = eot.RevisionNumber,
            Category = eot.Category,
            Scenario = eot.Scenario,
            ChangeProposedBy = eot.ChangeProposedBy,
            Reference = eot.Reference,
            Reason = eot.Reason,
            SpaDate = eot.SpaDate?.ToLocalTime(),
            DesignRevisionDate = eot.DesignRevisionDate?.ToLocalTime(),
            EstimatedTimeImpactDays = eot.EstimatedTimeImpactDays,
            EstimatedCostImpact = eot.EstimatedCostImpact,
            OriginalApprovedScope = eot.OriginalApprovedScope,
            RevisedScope = eot.RevisedScope,
            ScopeAddition = eot.ScopeAddition,
            ScopeReduction = eot.ScopeReduction,
            DelayDays = eot.DelayDays,
            CostEscalation = eot.CostEscalation,
            SubmissionStatus = eot.SubmissionStatus,
            ClientApproval = eot.ClientApproval,
            Remarks = eot.Remarks,
            HasApprovedDrawings = eot.HasApprovedDrawings,
            HasRevisedDrawings = eot.HasRevisedDrawings,
            HasClientInstructions = eot.HasClientInstructions,
            HasDelayAnalysis = eot.HasDelayAnalysis,
            HasScopeVariationStatement = eot.HasScopeVariationStatement,
            HasProgressReport = eot.HasProgressReport,
            HasConsultantCorrespondence = eot.HasConsultantCorrespondence,
            HasSupportingDocuments = eot.HasSupportingDocuments,
            CategoryOptions = CategoryOptions(),
            ScenarioOptions = ScenarioOptions(),
            ChangeProposedByOptions = ChangeProposedByOptions(),
            SubmissionStatusOptions = SubmissionStatusOptions(),
            ClientApprovalOptions = ClientApprovalOptions(),
            ProjectOptions = await ProjectOptionsAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Edit(EotFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReloadEditAsync(model);
        }

        var eot = await _context.EotRequests.FirstOrDefaultAsync(e => e.Id == model.Id);
        if (eot is null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);

        eot.Project = model.Project.Trim();
        eot.Client = model.Client?.Trim();
        eot.FinancialYear = model.FinancialYear?.Trim();
        eot.RevisionNumber = model.RevisionNumber;
        eot.Category = model.Category;
        eot.Scenario = model.Scenario;
        eot.ChangeProposedBy = model.ChangeProposedBy;
        eot.Reference = model.Reference?.Trim();
        eot.Reason = model.Reason.Trim();
        eot.SpaDate = ToUtcOrNull(model.SpaDate);
        eot.DesignRevisionDate = ToUtcOrNull(model.DesignRevisionDate);
        eot.EstimatedTimeImpactDays = model.EstimatedTimeImpactDays;
        eot.EstimatedCostImpact = model.EstimatedCostImpact;
        eot.OriginalApprovedScope = model.OriginalApprovedScope?.Trim();
        eot.RevisedScope = model.RevisedScope?.Trim();
        eot.ScopeAddition = model.ScopeAddition;
        eot.ScopeReduction = model.ScopeReduction;
        eot.DelayDays = model.DelayDays;
        eot.CostEscalation = model.CostEscalation?.Trim();
        eot.SubmissionStatus = model.SubmissionStatus;
        eot.ClientApproval = model.ClientApproval;
        eot.Remarks = model.Remarks?.Trim();
        eot.HasApprovedDrawings = model.HasApprovedDrawings;
        eot.HasRevisedDrawings = model.HasRevisedDrawings;
        eot.HasClientInstructions = model.HasClientInstructions;
        eot.HasDelayAnalysis = model.HasDelayAnalysis;
        eot.HasScopeVariationStatement = model.HasScopeVariationStatement;
        eot.HasProgressReport = model.HasProgressReport;
        eot.HasConsultantCorrespondence = model.HasConsultantCorrespondence;
        eot.HasSupportingDocuments = model.HasSupportingDocuments;
        eot.UpdatedAt = DateTime.UtcNow;

        if (user is not null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Action = "Update",
                EntityType = "EotRequest",
                EntityId = eot.EotNumber,
                Details = $"EOT {eot.EotNumber} details updated",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"EOT {eot.EotNumber} updated.";
        return RedirectToAction(nameof(Details), new { id = eot.Id });
    }

    public async Task<IActionResult> Email(int id)
    {
        var eot = await _context.EotRequests
            .AsNoTracking()
            .Include(e => e.CreatedBy)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eot is null)
        {
            return NotFound();
        }

        var (subject, body) = BuildEmail(eot);
        ViewData["EmailSubject"] = subject;
        ViewData["EmailBody"] = body;
        return View(eot);
    }

    private static (string Subject, string Body) BuildEmail(EotRequest eot)
    {
        var subject = $"[{eot.EotNumber}] {eot.Project} \u2013 Extension of Time Request";

        var body =
            $"Dear Team,\n\n" +
            $"This is to notify that the following change, identified after Shell Plan Approval and Hardcopy Signoff, " +
            $"impacts the approved project schedule.\n\n" +
            $"Change Proposed By: {ChangeProposedByText(eot.ChangeProposedBy)}\n" +
            $"Status of Project: {ScenarioText(eot.Scenario)}\n" +
            $"Project: {eot.Project}\n" +
            $"EOT No.: {eot.EotNumber}\n" +
            $"Reason: {eot.Reason}\n" +
            $"Reference: {eot.Reference}\n" +
            $"Estimated Time Impact: {(eot.EstimatedTimeImpactDays?.ToString() ?? "-")} Days (Approx.)\n" +
            $"Estimated Cost Impact: {(eot.EstimatedCostImpact?.ToString("N0") ?? "To be Assessed")}\n\n" +
            $"Kindly review and confirm the time impact. The cost impact will be evaluated separately by the " +
            $"Contracts Department.\n\n" +
            $"Thanks & Regards,\n" +
            $"Design Department\n" +
            $"I-FORM Aluminium & Design LLP";

        return (subject, body);
    }

    private static string CurrentFinancialYear()
    {
        var now = DateTime.Now;
        var startYear = now.Month >= 4 ? now.Year : now.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:00}";
    }

    private static DateTime? ToUtcOrNull(DateTime? value)
        => value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Local).ToUniversalTime()
            : null;

    private static string CategoryText(EotCategory category) => category switch
    {
        EotCategory.DesignRevision => "Design Revision",
        EotCategory.ScopeChange => "Scope Change",
        EotCategory.ClientInstruction => "Client Instruction",
        EotCategory.ApprovalDelay => "Approval Delay",
        EotCategory.SiteConstraint => "Site Constraint",
        EotCategory.ForceMajeure => "Force Majeure",
        EotCategory.OtherContractualEvents => "Other Contractual Events",
        _ => category.ToString()
    };

    private static string ScenarioText(EotScenario? scenario) => scenario switch
    {
        EotScenario.Sc1 => "SC-1",
        EotScenario.Sc2 => "SC-2",
        EotScenario.Sc3 => "SC-3",
        _ => "Not set"
    };

    private static string ScenarioDescription(EotScenario scenario) => scenario switch
    {
        EotScenario.Sc1 => "Production Completed & Changes Initiated",
        EotScenario.Sc2 => "Production Partially Completed & Changes Initiated",
        EotScenario.Sc3 => "Production Not Started & Changes Initiated",
        _ => ""
    };

    private static string ChangeProposedByText(ChangeProposedBy? proposedBy) => proposedBy?.ToString() ?? "Not set";

    private async Task<IReadOnlyList<string>> ProjectOptionsAsync()
        => await _context.EotRequests
            .AsNoTracking()
            .Where(e => !string.IsNullOrWhiteSpace(e.Project))
            .Select(e => e.Project)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

    private static IEnumerable<SelectListItem> CategoryOptions(EotCategory? selected = null)
        => Enum.GetValues<EotCategory>().Select(v =>
            new SelectListItem(CategoryText(v), v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> ScenarioOptions(EotScenario? selected = null)
        => Enum.GetValues<EotScenario>().Select(v =>
            new SelectListItem($"{v} - {ScenarioDescription(v)}", v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> ChangeProposedByOptions(ChangeProposedBy? selected = null)
        => Enum.GetValues<ChangeProposedBy>().Select(v =>
            new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> SubmissionStatusOptions(EotSubmissionStatus? selected = null)
        => Enum.GetValues<EotSubmissionStatus>().Select(v =>
            new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private static IEnumerable<SelectListItem> ClientApprovalOptions(EotClientApproval? selected = null)
        => Enum.GetValues<EotClientApproval>().Select(v =>
            new SelectListItem(v.ToString(), v.ToString(), v == selected));

    private async Task<IActionResult> ReloadCreateAsync(EotFormViewModel model)
    {
        model.CategoryOptions = CategoryOptions();
        model.ScenarioOptions = ScenarioOptions();
        model.ChangeProposedByOptions = ChangeProposedByOptions();
        model.SubmissionStatusOptions = SubmissionStatusOptions();
        model.ClientApprovalOptions = ClientApprovalOptions();
        model.ProjectOptions = await ProjectOptionsAsync();
        return View(model);
    }

    private async Task<IActionResult> ReloadEditAsync(EotFormViewModel model)
    {
        model.CategoryOptions = CategoryOptions();
        model.ScenarioOptions = ScenarioOptions();
        model.ChangeProposedByOptions = ChangeProposedByOptions();
        model.SubmissionStatusOptions = SubmissionStatusOptions();
        model.ClientApprovalOptions = ClientApprovalOptions();
        model.ProjectOptions = await ProjectOptionsAsync();
        return View(model);
    }
}
