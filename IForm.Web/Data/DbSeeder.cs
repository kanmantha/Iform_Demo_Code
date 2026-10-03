using IForm.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Data;

public static class DbSeeder
{
    /// <summary>Shared by the HR seed so every row lands on a consistent "today".</summary>
    private static readonly DateTime now = DateTime.UtcNow;

    private static readonly Random rnd = new(97);

    public static readonly string AdminRole = "Admin";
    public static readonly string ManagerRole = "Manager";
    public static readonly string UserRole = "User";

    public static async Task SeedAsync(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // The checked-in migrations target Postgres. Local SQLite databases are
        // throwaway dev files, so the schema is created straight from the model.
        if (context.Database.IsNpgsql())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        await SeedRolesAsync(roleManager);
        var admin = await SeedUserAsync(userManager, "admin@iform.app", "Admin@123", "System Administrator", "Safety & Compliance", "Administrator", AdminRole);
        var manager = await SeedUserAsync(userManager, "manager@iform.app", "Manager@123", "Sarah Mitchell", "Safety Operations", "Safety Manager", ManagerRole);
        var user1 = await SeedUserAsync(userManager, "john@iform.app", "User@123", "John Carter", "Maintenance", "Maintenance Engineer", UserRole);
        var user2 = await SeedUserAsync(userManager, "priya@iform.app", "User@123", "Priya Sharma", "Quality", "Quality Analyst", UserRole);
        var user3 = await SeedUserAsync(userManager, "mike@iform.app", "User@123", "Mike Walker", "Production", "Production Supervisor", UserRole);

        var sites = await SeedSitesAsync(context);
        var units = await SeedOrganizationUnitsAsync(context, userManager);

        await LinkUsersToUnitsAsync(context, userManager, units, admin, manager, user1, user2, user3);

        if (!await context.Tickets.AnyAsync())
        {
            await SeedTicketsAsync(context, admin, manager, user1, user2, user3, sites);
        }
        else
        {
            await LinkTicketsToSitesAsync(context, sites);
        }

        if (!await context.Incidents.AnyAsync())
        {
            await SeedIncidentsAsync(context, admin, manager, user1, user2, user3, sites);
        }

        if (!await context.ActionItems.AnyAsync())
        {
            await SeedActionsAsync(context, admin, manager, user1, user2, user3);
        }

        var products = await SeedProductsAsync(context);

        if (!await context.SiteQueries.AnyAsync())
        {
            await SeedSiteQueriesAsync(context, manager, user1, user2, user3, products);
        }

        await SeedHrAsync(context, admin);

        await context.SaveChangesAsync();
    }

    private static async Task SeedHrAsync(ApplicationDbContext context, AppUser admin)
    {
        if (await context.Employees.AnyAsync())
        {
            return;
        }

        var departments = new[]
        {
            new Department { Code = "EXEC", Name = "Executive", Description = "Leadership" },
            new Department { Code = "ENG", Name = "Engineering", Description = "Design and site engineering" },
            new Department { Code = "QA", Name = "Quality", Description = "Quality assurance and control" },
            new Department { Code = "SAFETY", Name = "Safety", Description = "Health, safety and environment" },
            new Department { Code = "OPS", Name = "Operations", Description = "Site and production operations" },
            new Department { Code = "MAINT", Name = "Maintenance", Description = "Equipment and facilities" },
            new Department { Code = "HR", Name = "Human Resources", Description = "People and training" },
            new Department { Code = "FIN", Name = "Finance", Description = "Accounts and payroll" }
        };
        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();

        var byCode = departments.ToDictionary(d => d.Code);

        var employees = new[]
        {
            Employee("EMP-001", "Meera", "Krishnan", "exec.admin@iform.app", byCode["EXEC"].Id, "Managing Director"),
            Employee("EMP-002", "Arjun", "Deshpande", "arjun.d@iform.app", byCode["ENG"].Id, "Head of Engineering"),
            Employee("EMP-003", "Kavya", "Reddy", "kavya.reddy@iform.app", byCode["HR"].Id, "HR Manager"),
            Employee("EMP-004", "Rohit", "Shah", "rohit.shah@iform.app", byCode["SAFETY"].Id, "Safety Officer"),
            Employee("EMP-005", "Neha", "Gupta", "neha.gupta@iform.app", byCode["QA"].Id, "Quality Manager"),
            Employee("EMP-006", "Imran", "Sheikh", "imran.sheikh@iform.app", byCode["ENG"].Id, "Senior Site Engineer"),
            Employee("EMP-007", "Divya", "Menon", "divya.menon@iform.app", byCode["OPS"].Id, "Production Supervisor"),
            Employee("EMP-008", "Suresh", "Pai", "suresh.pai@iform.app", byCode["MAINT"].Id, "Maintenance Engineer"),
            Employee("EMP-009", "Ananya", "Iyer", "ananya.iyer@iform.app", byCode["FIN"].Id, "Accounts Executive"),
            Employee("EMP-010", "Vikram", "Singh", "vikram.singh@iform.app", byCode["OPS"].Id, "Site Operator"),
            Employee("EMP-011", "Pooja", "Nair", "pooja.nair@iform.app", byCode["QA"].Id, "Quality Analyst"),
            Employee("EMP-012", "Rahul", "Chauhan", "rahul.chauhan@iform.app", byCode["ENG"].Id, "Design Engineer"),
            Employee("EMP-013", "Fatima", "Begum", "fatima.begum@iform.app", byCode["SAFETY"].Id, "Safety Assistant"),
            Employee("EMP-014", "Aditya", "Rao", "aditya.rao@iform.app", byCode["OPS"].Id, "Store Supervisor"),
            Employee("EMP-015", "Lakshmi", "Pillai", "lakshmi.pillai@iform.app", byCode["HR"].Id, "HR Executive")
        };
        context.Employees.AddRange(employees);
        await context.SaveChangesAsync();

        // Reporting lines, set after insert because the FKs need real ids.
        employees[6].ManagerId = employees[2].Id;
        employees[5].ManagerId = employees[1].Id;
        employees[11].ManagerId = employees[1].Id;
        employees[9].ManagerId = employees[6].Id;
        employees[13].ManagerId = employees[6].Id;
        employees[10].ManagerId = employees[4].Id;
        employees[12].ManagerId = employees[3].Id;
        employees[7].ManagerId = employees[6].Id;
        employees[8].ManagerId = employees[4].Id;
        employees[13].ManagerId = employees[2].Id;
        employees[14].ManagerId = employees[1].Id;
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;

        // Opening entitlements. One grant per leave type per employee so the ledger
        // balance has a sensible starting point instead of zero.
        foreach (var employee in employees)
        {
            context.LeaveLedgerEntries.AddRange(
                new LeaveLedgerEntry
                {
                    EmployeeId = employee.Id,
                    LeaveType = LeaveType.Casual,
                    Days = 12m,
                    Reason = "Casual leave entitlement",
                    CreatedAt = now.AddDays(-180)
                },
                new LeaveLedgerEntry
                {
                    EmployeeId = employee.Id,
                    LeaveType = LeaveType.Earned,
                    Days = 15m,
                    Reason = "Earned leave entitlement",
                    CreatedAt = now.AddDays(-180)
                },
                new LeaveLedgerEntry
                {
                    EmployeeId = employee.Id,
                    LeaveType = LeaveType.Sick,
                    Days = 10m,
                    Reason = "Sick leave entitlement",
                    CreatedAt = now.AddDays(-180)
                });
        }

        var leaveSeed = new (int EmployeeIndex, LeaveType Type, int StartOffset, decimal Days, string Reason, LeaveStatus Status)[]
        {
            (5, LeaveType.Casual, -20, 2m, "Family function at home", LeaveStatus.Approved),
            (6, LeaveType.Sick, -14, 3m, "Viral fever, advised rest by doctor", LeaveStatus.Approved),
            (7, LeaveType.Casual, -9, 1m, "Personal work", LeaveStatus.Approved),
            (8, LeaveType.Earned, -30, 4m, "Planned family holiday", LeaveStatus.Approved),
            (9, LeaveType.Casual, -4, 1m, "Medical appointment", LeaveStatus.Pending),
            (10, LeaveType.Earned, -2, 2m, "Long weekend trip", LeaveStatus.Pending),
            (11, LeaveType.Sick, -1, 5m, "Recovering from dengue", LeaveStatus.Pending),
            (12, LeaveType.Casual, -6, 1m, "Child school function", LeaveStatus.Rejected),
            (13, LeaveType.Casual, -18, 3m, "Attended a cousin's wedding", LeaveStatus.Approved),
            (4, LeaveType.Earned, -25, 2m, "Short break with family", LeaveStatus.Approved)
        };

        int leaveCount = 1;
        foreach (var (employeeIndex, type, startOffset, days, reason, status) in leaveSeed)
        {
            var start = NextWorkingDay(now.AddDays(startOffset));
            var request = new LeaveRequest
            {
                RequestNumber = $"LV-{leaveCount++:D5}",
                EmployeeId = employees[employeeIndex].Id,
                LeaveType = type,
                StartDate = start,
                EndDate = EndDateForWorkingDays(start, days),
                Days = days,
                Reason = reason,
                Status = status,
                CreatedAt = start.AddDays(-3),
                DecidedAt = status == LeaveStatus.Pending ? null : start.AddDays(-2),
                DecidedById = status == LeaveStatus.Pending ? null : admin.Id,
                DecisionNote = status == LeaveStatus.Rejected ? "Peak production week, please reschedule." : null
            };

            context.LeaveRequests.Add(request);

            if (status == LeaveStatus.Approved)
            {
                context.LeaveLedgerEntries.Add(new LeaveLedgerEntry
                {
                    EmployeeId = employees[employeeIndex].Id,
                    LeaveType = type,
                    Days = -days,
                    Reason = $"Approved leave {request.RequestNumber}",
                    LeaveRequest = request,
                    CreatedAt = start.AddDays(-2)
                });
            }
        }

        int claimCount = 1;
        var claimSeed = new (int EmployeeIndex, string Category, decimal Amount, string Description, ExpenseStatus Status)[]
        {
            (5, "Travel", 1850.50m, "Taxi fare for Site C inspection", ExpenseStatus.Approved),
            (7, "Meals", 420.00m, "Team lunch after shutdown", ExpenseStatus.Submitted),
            (9, "Travel", 3200.00m, "Sleeper train to client site", ExpenseStatus.Submitted),
            (11, "Stationery", 640.75m, "Torque wrench calibration kit", ExpenseStatus.Rejected),
            (13, "Accommodation", 2750.00m, "Two nights near project site", ExpenseStatus.Reimbursed),
            (6, "Communication", 899.00m, "Monthly mobile recharge", ExpenseStatus.Approved),
            (2, "Training", 4500.00m, "Safety leadership workshop fee", ExpenseStatus.Reimbursed),
            (12, "Travel", 1250.00m, "Airport transfer and tolls", ExpenseStatus.Submitted)
        };

        foreach (var (employeeIndex, category, amount, description, status) in claimSeed)
        {
            var claim = new ExpenseClaim
            {
                ClaimNumber = $"EX-{claimCount++:D5}",
                EmployeeId = employees[employeeIndex].Id,
                Category = category,
                Description = description,
                Amount = amount,
                Currency = "INR",
                ExpenseDate = now.AddDays(-rnd.Next(1, 40)).Date,
                Status = status,
                CreatedAt = now.AddDays(-rnd.Next(2, 45)),
                DecidedAt = status is ExpenseStatus.Submitted ? null : now.AddDays(-1),
                DecidedById = status is ExpenseStatus.Submitted ? null : admin.Id,
                DecisionNote = status == ExpenseStatus.Rejected ? "Item not on the approved purchase list; please attach the indents." : null
            };

            context.ExpenseClaims.Add(claim);
        }

        var attendanceSeed = new (int EmployeeIndex, int DaysAgo, decimal Hours, AttendanceStatus Status, string? Notes)[]
        {
            (5, 0, 9.5m, AttendanceStatus.Present, null),
            (6, 0, 8.25m, AttendanceStatus.Present, null),
            (7, 0, 4m, AttendanceStatus.HalfDay, "Left early for a doctor visit"),
            (8, 0, 0m, AttendanceStatus.Absent, "Absent without notice"),
            (9, 1, 10m, AttendanceStatus.Present, null),
            (10, 1, 8m, AttendanceStatus.Present, null),
            (11, 1, 6.5m, AttendanceStatus.Present, null),
            (12, 2, 9m, AttendanceStatus.Present, null),
            (13, 2, 0m, AttendanceStatus.Absent, "Absent without intimation"),
            (14, 2, 7.75m, AttendanceStatus.Present, null),
            (5, 3, 8.5m, AttendanceStatus.Present, null),
            (6, 3, 3.5m, AttendanceStatus.HalfDay, "Half day, travel delay"),
            (7, 4, 9m, AttendanceStatus.Present, null),
            (8, 4, 8m, AttendanceStatus.Present, null)
        };

        foreach (var (employeeIndex, daysAgo, hours, status, notes) in attendanceSeed)
        {
            var date = NextWorkingDay(now.AddDays(-daysAgo));
            context.AttendanceRecords.Add(new AttendanceRecord
            {
                EmployeeId = employees[employeeIndex].Id,
                Date = date,
                CheckIn = hours == 0m ? null : date.AddHours(9),
                CheckOut = hours == 0m ? null : date.AddHours(9).AddHours((double)hours),
                HoursWorked = hours,
                Status = status,
                Notes = notes,
                CreatedAt = date.AddHours(18)
            });
        }

        var templates = new[]
        {
            new OnboardingTemplate
            {
                Name = "Site Engineer",
                Description = "Standard checklist for engineering hires joining a site",
                CreatedAt = now.AddDays(-60),
                TaskTemplates =
                {
                    new OnboardingTaskTemplate { Title = "Collect ID and address proofs", DueDayOffset = 1, DisplayOrder = 1 },
                    new OnboardingTaskTemplate { Title = "Complete safety induction", DueDayOffset = 2, DisplayOrder = 2 },
                    new OnboardingTaskTemplate { Title = "Issue PPE and site access card", DueDayOffset = 3, DisplayOrder = 3 },
                    new OnboardingTaskTemplate { Title = "Assign a buddy for the first week", DueDayOffset = 5, DisplayOrder = 4 },
                    new OnboardingTaskTemplate { Title = "Sign off on site SOPs", DueDayOffset = 7, DisplayOrder = 5 }
                }
            },
            new OnboardingTemplate
            {
                Name = "Office / Admin",
                Description = "Checklist for head office and support roles",
                CreatedAt = now.AddDays(-60),
                TaskTemplates =
                {
                    new OnboardingTaskTemplate { Title = "Collect ID and tax proofs", DueDayOffset = 1, DisplayOrder = 1 },
                    new OnboardingTaskTemplate { Title = "Set up payroll and bank details", DueDayOffset = 3, DisplayOrder = 2 },
                    new OnboardingTaskTemplate { Title = "Grant system access", DueDayOffset = 5, DisplayOrder = 3 },
                    new OnboardingTaskTemplate { Title = "Allocate workstation and email", DueDayOffset = 5, DisplayOrder = 4 }
                }
            }
        };
        context.OnboardingTemplates.AddRange(templates);
        await context.SaveChangesAsync();

        var engineerOnboarding = new EmployeeOnboarding
        {
            EmployeeId = employees[12].Id,
            OnboardingTemplateId = templates[0].Id,
            StartedAt = now.AddDays(-9)
        };

        var engineerTasks = new[]
        {
            ("Collect ID and address proofs", 1, true),
            ("Complete safety induction", 2, true),
            ("Issue PPE and site access card", 3, true),
            ("Assign a buddy for the first week", 5, false),
            ("Sign off on site SOPs", 14, false)
        };

        var order = 1;
        foreach (var (title, offset, completed) in engineerTasks)
        {
            engineerOnboarding.Tasks.Add(new EmployeeOnboardingTask
            {
                Title = title,
                DueDate = now.AddDays(-9).Date.AddDays(offset),
                DisplayOrder = order++,
                IsCompleted = completed,
                CompletedAt = completed ? now.AddDays(-9).AddHours(order) : null,
                CompletedById = completed ? admin.Id : null
            });
        }

        var adminOnboarding = new EmployeeOnboarding
        {
            EmployeeId = employees[14].Id,
            OnboardingTemplateId = templates[1].Id,
            StartedAt = now.AddDays(-12)
        };

        var adminTasks = new[]
        {
            ("Collect ID and tax proofs", 1, true),
            ("Set up payroll and bank details", 3, true),
            ("Grant system access", 5, true),
            ("Allocate workstation and email", 5, false)
        };

        order = 1;
        foreach (var (title, offset, completed) in adminTasks)
        {
            adminOnboarding.Tasks.Add(new EmployeeOnboardingTask
            {
                Title = title,
                DueDate = now.AddDays(-12).Date.AddDays(offset),
                DisplayOrder = order++,
                IsCompleted = completed,
                CompletedAt = completed ? now.AddDays(-12).AddHours(order) : null,
                CompletedById = completed ? admin.Id : null
            });
        }

        context.EmployeeOnboardings.AddRange(engineerOnboarding, adminOnboarding);
    }

    private static Employee Employee(
        string code,
        string firstName,
        string lastName,
        string email,
        int departmentId,
        string jobTitle)
        => new()
        {
            EmployeeCode = code,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            DepartmentId = departmentId,
            JobTitle = jobTitle,
            Status = EmploymentStatus.Active,
            DateJoined = now.AddDays(-rnd.Next(40, 900)),
            EmergencyContactName = "Emergency Contact",
            EmergencyContactPhone = "+91 90000 00000",
            CreatedAt = now.AddDays(-rnd.Next(40, 900))
        };

    /// <summary>Skips to the next Monday-to-Friday day so seeded leave lands on working days.</summary>
    private static DateTime NextWorkingDay(DateTime date)
    {
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date.Date;
    }

    /// <summary>
    /// Returns the end date of a leave span that covers exactly <paramref name="workingDays"/>
    /// working days starting at <paramref name="start"/>, so StartDate/EndDate always agree with
    /// the Days value the way LeaveCalculator would count them.
    /// </summary>
    private static DateTime EndDateForWorkingDays(DateTime start, decimal workingDays)
    {
        var end = start;
        var remaining = (int)Math.Round(workingDays, MidpointRounding.AwayFromZero) - 1;

        while (remaining > 0)
        {
            end = end.AddDays(1);
            if (end.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                remaining--;
            }
        }

        return end;
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { AdminRole, ManagerRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task<AppUser> SeedUserAsync(
        UserManager<AppUser> userManager,
        string email,
        string password,
        string fullName,
        string department,
        string jobTitle,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                Department = department,
                JobTitle = jobTitle,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("fullName", fullName));
        }
        else
        {
            user.FullName = fullName;
            user.Department = department;
            user.JobTitle = jobTitle;
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task<List<Site>> SeedSitesAsync(ApplicationDbContext context)
    {
        if (await context.Sites.AnyAsync())
        {
            return await context.Sites.OrderBy(s => s.Id).ToListAsync();
        }

        var sites = new[]
        {
            new Site { Code = "HQ", Name = "Head Office", Address = "1 Corporate Park", City = "Hyderabad", Country = "India" },
            new Site { Code = "PLT-A", Name = "Plant A - Manufacturing", Address = "Plot 42, Industrial Estate", City = "Hyderabad", Country = "India" },
            new Site { Code = "PLT-B", Name = "Plant B - Packing", Address = "Sector 18", City = "Pune", Country = "India" },
            new Site { Code = "WH-1", Name = "Central Warehouse", Address = "Logistics Hub", City = "Chennai", Country = "India" },
            new Site { Code = "SITE-C", Name = "Site C - Construction", Address = "Expressway Corridor", City = "Mumbai", Country = "India" }
        };

        context.Sites.AddRange(sites);
        await context.SaveChangesAsync();
        return sites.ToList();
    }

    private static async Task<Dictionary<string, OrganizationUnit>> SeedOrganizationUnitsAsync(
        ApplicationDbContext context,
        UserManager<AppUser> userManager)
    {
        if (await context.OrganizationUnits.AnyAsync())
        {
            return await context.OrganizationUnits.ToDictionaryAsync(u => u.Code);
        }

        var manager = await userManager.FindByEmailAsync("manager@iform.app");

        var units = new[]
        {
            new OrganizationUnit { Code = "OPS", Name = "Operations", Description = "Production and site operations", ParentId = null, ManagerId = manager?.Id },
            new OrganizationUnit { Code = "SAFETY", Name = "Safety & Compliance", Description = "Health, safety and environment", ParentId = null, ManagerId = manager?.Id },
            new OrganizationUnit { Code = "MAINT", Name = "Maintenance", Description = "Equipment and facilities maintenance", ParentId = null, ManagerId = manager?.Id },
            new OrganizationUnit { Code = "QUALITY", Name = "Quality", Description = "Quality assurance and control", ParentId = null, ManagerId = manager?.Id },
            new OrganizationUnit { Code = "HR", Name = "Human Resources", Description = "People and training", ParentId = null, ManagerId = manager?.Id }
        };

        context.OrganizationUnits.AddRange(units);
        await context.SaveChangesAsync();

        return units.ToDictionary(u => u.Code);
    }

    private static async Task LinkUsersToUnitsAsync(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        Dictionary<string, OrganizationUnit> units,
        AppUser admin,
        AppUser manager,
        AppUser user1,
        AppUser user2,
        AppUser user3)
    {
        var links = new (AppUser User, string Code)[]
        {
            (admin, "SAFETY"),
            (manager, "SAFETY"),
            (user1, "MAINT"),
            (user2, "QUALITY"),
            (user3, "OPS")
        };

        foreach (var (user, code) in links)
        {
            if (user.OrganizationUnitId != units[code].Id)
            {
                user.OrganizationUnitId = units[code].Id;
                await userManager.UpdateAsync(user);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task LinkTicketsToSitesAsync(ApplicationDbContext context, List<Site> sites)
    {
        var tickets = await context.Tickets.ToListAsync();
        var rnd = new Random(7);
        foreach (var ticket in tickets)
        {
            if (!ticket.SiteId.HasValue)
            {
                ticket.SiteId = sites[rnd.Next(sites.Count)].Id;
            }
        }
    }

    private static async Task SeedTicketsAsync(
        ApplicationDbContext context,
        AppUser admin,
        AppUser manager,
        AppUser user1,
        AppUser user2,
        AppUser user3,
        List<Site> sites)
    {
        var users = new[] { user1, user2, user3 };
        var rnd = new Random(42);
        var now = DateTime.UtcNow;

        var seed = new (string Title, string Description, TicketCategory Category, string Location, string Assignee)[]
        {
            ("Spill of hydraulic oil on loading bay floor", "Hydraulic oil leaked from forklift near bay 3. Area is slippery and requires immediate attention.", TicketCategory.Incident, "Loading Bay 3", nameof(user1)),
            ("Slippery surface near Pump 3", "Water accumulation from a leaking pipe makes the walkway hazardous for workers.", TicketCategory.UnsafeCondition, "Pump House, Zone B", nameof(user1)),
            ("Worker bypassing machine guard", "An operator was observed operating the press without the safety guard in place.", TicketCategory.UnsafeAct, "Press Shop 2", nameof(user3)),
            ("Loose railing on warehouse staircase", "The handrail on the mezzanine staircase is loose and wobbles under pressure.", TicketCategory.Hazard, "Warehouse Mezzanine", nameof(user2)),
            ("Fire extinguisher inspection overdue", "Fire extinguisher near boiler area has exceeded its inspection date.", TicketCategory.Equipment, "Boiler Room", nameof(user1)),
            ("Near miss: falling crate narrowly missed worker", "A stacked crate toppled from the rack; no injury occurred but risk was high.", TicketCategory.NearMiss, "Storage Racks A-12", nameof(user2)),
            ("Smoke observed from conveyor motor", "Burning smell and light smoke coming from conveyor motor C in packing line.", TicketCategory.Incident, "Packing Line C", nameof(user3)),
            ("Confined space permit expired", "The PTW for tank cleaning is expired and work is still ongoing.", TicketCategory.Hazard, "Effluent Tank", nameof(user1)),
            ("Exposed electrical wiring near wash bay", "Damaged cable insulation exposes live wires close to water source.", TicketCategory.UnsafeCondition, "Vehicle Wash Bay", nameof(user2)),
            ("Incorrect PPE usage in grinding area", "Workers were grinding without face shields. Retraining requested.", TicketCategory.Training, "Grinding Area", nameof(user3)),
            ("Ventilation fan not functioning", "Exhaust fan in paint booth is not operating, fumes accumulating.", TicketCategory.Equipment, "Paint Booth", nameof(user1)),
            ("Noise level exceeded safe limit in compressor room", "Sound meter logged 92 dB continuously in compressor room.", TicketCategory.General, "Compressor Room", nameof(user2)),
            ("Chemical drum without proper label", "A drum of unknown chemical was found unlabelled near storage.", TicketCategory.Hazard, "Chemical Store", nameof(user3)),
            ("Trip hazard from cable across aisle", "Power cables running across the main aisle are not covered or secured.", TicketCategory.UnsafeCondition, "Assembly Floor", nameof(user1)),
            ("Annual safety training pending for new hires", "New employees hired this quarter have not completed mandatory safety orientation.", TicketCategory.Training, "HR Training Hall", nameof(user2))
        };

        var statuses = new[] { TicketStatus.New, TicketStatus.New, TicketStatus.Open, TicketStatus.Open, TicketStatus.InProgress, TicketStatus.InProgress, TicketStatus.Pending, TicketStatus.Resolved, TicketStatus.Resolved, TicketStatus.Closed, TicketStatus.Closed };
        var priorities = new[] { TicketPriority.Low, TicketPriority.Medium, TicketPriority.Medium, TicketPriority.High, TicketPriority.High, TicketPriority.High, TicketPriority.Critical, TicketPriority.Critical };

        int ticketCount = 1;
        foreach (var (title, desc, category, location, assigneeKey) in seed)
        {
            var reportedBy = users[rnd.Next(users.Length)];
            var assignedTo = assigneeKey switch
            {
                nameof(user1) => user1,
                nameof(user2) => user2,
                _ => user3
            };

            var createdAt = now.AddDays(-rnd.Next(1, 31)).AddHours(-rnd.Next(0, 10));
            var status = statuses[rnd.Next(statuses.Length)];
            var priority = priorities[rnd.Next(priorities.Length)];

            var ticket = new Ticket
            {
                TicketNumber = $"TKT-{ticketCount++:D4}",
                Title = title,
                Description = desc,
                Category = category,
                Priority = priority,
                Status = status,
                Location = location,
                SiteId = sites[rnd.Next(sites.Count)].Id,
                ReportedById = reportedBy.Id,
                AssignedToId = assignedTo.Id,
                CreatedAt = createdAt,
                UpdatedAt = status == TicketStatus.New ? null : createdAt.AddHours(rnd.Next(4, 72)),
                DueDate = createdAt.AddDays(priority >= TicketPriority.High ? 3 : 7),
                ClosedAt = status is TicketStatus.Resolved or TicketStatus.Closed
                    ? createdAt.AddDays(rnd.Next(2, 6))
                    : null,
                ResolutionNotes = status is TicketStatus.Resolved or TicketStatus.Closed
                    ? "Root cause addressed and corrective action verified by the site safety team."
                    : null
            };

            if (status is TicketStatus.Resolved or TicketStatus.Closed)
            {
                ticket.Status = TicketStatus.Resolved;
            }

            context.Tickets.Add(ticket);

            if (status is TicketStatus.Open or TicketStatus.InProgress or TicketStatus.Pending)
            {
                context.TicketComments.Add(new TicketComment
                {
                    Ticket = ticket,
                    UserId = assignedTo.Id,
                    CreatedAt = createdAt.AddHours(rnd.Next(2, 24)),
                    Body = $"Assigned to {assignedTo.FullName} for investigation and resolution."
                });
            }

            context.AuditLogs.Add(new AuditLog
            {
                UserId = reportedBy.Id,
                UserName = reportedBy.FullName,
                Action = "Create",
                EntityType = "Ticket",
                EntityId = ticket.TicketNumber,
                Details = $"Ticket {ticket.TicketNumber} reported.",
                CreatedAt = createdAt
            });
        }
    }

    private static async Task SeedIncidentsAsync(
        ApplicationDbContext context,
        AppUser admin,
        AppUser manager,
        AppUser user1,
        AppUser user2,
        AppUser user3,
        List<Site> sites)
    {
        var rnd = new Random(7);
        var now = DateTime.UtcNow;

        var seed = new (string Title, string Description, IncidentType Type, IncidentSeverity Severity, string Location, bool WorkRelated, int? Persons, int? Days, string? Immediate, string? RootCause, string? Notes, string Assignee)[]
        {
            ("Hand injury while operating shearing machine",
             "Operator sustained a crush injury to the left index finger while feeding steel sheets. Immediate first aid provided and the worker was referred to the medical centre.",
             IncidentType.FirstAidCase, IncidentSeverity.High, "Press Shop 2", true, 1, 0,
             "Machine stopped, area cordoned off, first aid administered.",
             "Operator bypassed the interlock guard to clear a jammed sheet.",
             "Interlock guard maintained in working order; a LOTO training session was scheduled.",
             nameof(user3)),
            ("Forklift near-miss with pedestrian",
             "A forklift reversing in the warehouse came within one metre of a pedestrian who was not visible to the driver. Horn was sounded and the pedestrian moved clear.",
             IncidentType.NearMiss, IncidentSeverity.Medium, "Central Warehouse", true, 2, 0,
             "Operation paused, area walkways re-marked.",
             "Blind corner at racking end and no mirror installed.",
             "Convex mirrors to be installed at all blind corners; pedestrian walkways repainted.",
             nameof(user2)),
            ("Hydraulic oil leak onto shop floor",
             "A hydraulic hose burst on a press, releasing approx. 20 litres of oil onto the shop floor. Clean-up completed and floor treated.",
             IncidentType.UnsafeCondition, IncidentSeverity.Medium, "Press Shop 1", true, 0, 0,
             "Machine isolated, spill kit deployed, absorbent used.",
             "Hose fatigue beyond scheduled replacement interval.",
             "Preventive maintenance schedule updated for hydraulic hoses.",
             nameof(user1)),
            ("Fire in electrical panel",
             "Minor electrical fire broke out in the MCC panel due to loose termination. Suppressed with CO2 extinguisher. No injuries.",
             IncidentType.PropertyDamage, IncidentSeverity.High, "Plant A - MCC Room", true, 0, 0,
             "Panel de-energised, extinguisher used, fire brigade notified.",
             "Loose terminations caused arcing and overheating.",
             "Thermographic scanning programme introduced for all MCC panels.",
             nameof(user1)),
            ("Worker felt dizzy from fumes in paint booth",
             "An operator reported dizziness after extended time in the paint booth. Removed from area, monitored by nurse, symptoms resolved.",
             IncidentType.Illness, IncidentSeverity.Medium, "Paint Booth", true, 1, 1,
             "Work stopped, ventilation checked, worker rested.",
             "Ventilation fan failure allowed solvent vapour build-up.",
             "Ventilation system maintenance contract revised with weekly checks.",
             nameof(user3)),
            ("Slip on oily surface outside cafeteria",
             "A staff member slipped on an oily patch outside the cafeteria and bruised her elbow. First aid administered.",
             IncidentType.FirstAidCase, IncidentSeverity.Low, "Cafeteria", true, 1, 0,
             "Area cleaned and temporary signage placed.",
             "Oil drippings from waste collection trolley.",
             "Waste collection route revised; daily floor inspection added.",
             nameof(user2)),
            ("Near miss: suspended load swing",
             "A crane load swung out of control during a lift at Site C, narrowly missing two workers. Load set down safely.",
             IncidentType.NearMiss, IncidentSeverity.Critical, "Site C - Block 4", true, 3, 0,
             "Lift area evacuated, crane operation suspended.",
             "Improper rigging and high wind conditions.",
             "Rigger certification review and wind-speed limit policy introduced.",
             nameof(user3)),
            ("Exposed live cable near water line",
             "Workmen discovered a damaged cable with exposed conductors running next to a water line. Electricity isolated immediately.",
             IncidentType.UnsafeCondition, IncidentSeverity.Critical, "Plant B - Utility Yard", true, 0, 0,
             "Power isolated, area barricaded, electrician called.",
             "Excavation damage from a recent trenching contractor.",
             "Sub-contractor toolbox talk and cable location survey mandated before digging.",
             nameof(user1)),
            ("Chemical splash on arm",
             "A technician received a minor splash of caustic solution on the forearm during transfer. Rinsed per procedure, no blistering.",
             IncidentType.FirstAidCase, IncidentSeverity.Medium, "Chemical Store", true, 1, 0,
             "Safety shower used, first aid applied.",
             "Worn transfer hose coupling.",
             "Transfer hoses replaced; inspection frequency increased.",
             nameof(user2)),
            ("Visitor injured by falling panel",
             "A stored ceiling panel fell and struck a contractor's hand during material retrieval.",
             IncidentType.PropertyDamage, IncidentSeverity.Medium, "Warehouse Rack 7", false, 1, 0,
             "First aid provided, rack area cleared.",
             "Improper panel storage above head height.",
             "Storage standard reviewed; heavy items moved to ground level.",
             nameof(user3))
        };

        var statuses = new[] { IncidentStatus.New, IncidentStatus.UnderInvestigation, IncidentStatus.UnderInvestigation, IncidentStatus.InvestigationComplete, IncidentStatus.InvestigationComplete, IncidentStatus.CorrectiveAction, IncidentStatus.Closed, IncidentStatus.Closed };

        int incidentCount = 1;
        foreach (var (title, desc, type, severity, location, workRelated, persons, days, immediate, rootCause, notes, assigneeKey) in seed)
        {
            var reportedBy = assigneeKey switch
            {
                nameof(user1) => user1,
                nameof(user2) => user2,
                _ => user3
            };
            var assignedTo = assigneeKey switch
            {
                nameof(user1) => user1,
                nameof(user2) => user2,
                _ => user3
            };

            var occurredAt = now.AddDays(-rnd.Next(2, 45)).AddHours(-rnd.Next(0, 12));
            var status = statuses[rnd.Next(statuses.Length)];

            context.Incidents.Add(new Incident
            {
                IncidentNumber = $"INC-{incidentCount++:D3}",
                Title = title,
                Description = desc,
                Type = type,
                Severity = severity,
                Status = status,
                OccurredAt = occurredAt,
                Location = location,
                SiteId = sites[rnd.Next(sites.Count)].Id,
                ReportedById = reportedBy.Id,
                AssignedToId = assignedTo.Id,
                ImmediateActionTaken = immediate,
                RootCause = status is IncidentStatus.InvestigationComplete or IncidentStatus.CorrectiveAction or IncidentStatus.Closed ? rootCause : null,
                InvestigationNotes = status is IncidentStatus.InvestigationComplete or IncidentStatus.CorrectiveAction or IncidentStatus.Closed ? notes : null,
                IsWorkRelated = workRelated,
                PersonsInvolved = persons,
                DaysLost = days,
                CreatedAt = occurredAt.AddHours(1),
                UpdatedAt = status == IncidentStatus.New ? null : occurredAt.AddDays(rnd.Next(1, 8)),
                ClosedAt = status == IncidentStatus.Closed ? occurredAt.AddDays(rnd.Next(5, 15)) : null
            });
        }
    }

    private static async Task SeedActionsAsync(
        ApplicationDbContext context,
        AppUser admin,
        AppUser manager,
        AppUser user1,
        AppUser user2,
        AppUser user3)
    {
        var rnd = new Random(11);
        var now = DateTime.UtcNow;
        var incidents = await context.Incidents.ToListAsync();
        var tickets = await context.Tickets.ToListAsync();
        var users = new[] { user1, user2, user3 };

        var seed = new (string Title, string Description, ActionPriority Priority, ActionStatus Status, int DaysDue, bool LinkIncident, bool LinkTicket, string Assignee)[]
        {
            ("Install convex mirrors at all blind corners", "Fit safety mirrors at racking blind corners per near-miss INC-002 and store all related inspection evidence.", ActionPriority.High, ActionStatus.InProgress, 4, true, false, nameof(user1)),
            ("Update hydraulic hose PM schedule", "Add hydraulic hose replacement to the preventive maintenance schedule across all presses.", ActionPriority.High, ActionStatus.Open, 6, true, false, nameof(user1)),
            ("Thermographic scan of all MCC panels", "Conduct thermal imaging of all MCC panels and log findings after the panel fire incident.", ActionPriority.Critical, ActionStatus.Open, 2, true, false, nameof(user1)),
            ("Run LOTO training session", "Deliver lock-out tag-out refresher training to press shop operators.", ActionPriority.Medium, ActionStatus.InProgress, 10, true, false, nameof(user3)),
            ("Repaint pedestrian walkways in warehouse", "Repaint and re-mark pedestrian lanes after the forklift near miss.", ActionPriority.Medium, ActionStatus.Open, 12, true, false, nameof(user2)),
            ("Revise waste collection route", "Change waste collection routing to prevent oil drips outside cafeteria.", ActionPriority.Low, ActionStatus.Completed, 3, true, false, nameof(user2)),
            ("Enforce wind-speed limit for crane lifts", "Adopt policy limiting crane lifts above 20 km/h wind at Site C.", ActionPriority.Critical, ActionStatus.Open, 5, true, false, nameof(user3)),
            ("Replace worn transfer hose couplings", "Replace caustic transfer hose couplings and increase inspection frequency.", ActionPriority.High, ActionStatus.InProgress, 7, true, false, nameof(user2)),
            ("Cover power cables across assembly aisle", "Install cable covers for cables running across the assembly floor aisle.", ActionPriority.High, ActionStatus.Open, 3, false, true, nameof(user1)),
            ("Service ventilation fan in paint booth", "Repair or replace the exhaust fan in the paint booth.", ActionPriority.Critical, ActionStatus.InProgress, 2, false, true, nameof(user3)),
            ("Replenish fire extinguisher in boiler room", "Inspect, service and replenish the boiler room fire extinguisher.", ActionPriority.Medium, ActionStatus.Open, 8, false, true, nameof(user1)),
            ("Complete safety orientation for new hires", "Schedule mandatory safety orientation for new employees.", ActionPriority.Medium, ActionStatus.Open, 14, false, true, nameof(user2)),
            ("Secure loose warehouse handrail", "Tighten and weld the loose mezzanine handrail.", ActionPriority.Low, ActionStatus.Completed, 4, false, true, nameof(user2)),
            ("Label unlabelled chemical drums", "Identify and label all unlabelled chemical drums in storage.", ActionPriority.High, ActionStatus.Open, 5, false, true, nameof(user3)),
            ("Deliver face-shield PPE training", "Retrain grinding-area staff on correct face-shield usage.", ActionPriority.Medium, ActionStatus.Open, 9, false, true, nameof(user3))
        };

        int actionCount = 1;
        foreach (var (title, desc, priority, status, daysDue, linkIncident, linkTicket, assigneeKey) in seed)
        {
            var assignedTo = assigneeKey switch
            {
                nameof(user1) => user1,
                nameof(user2) => user2,
                _ => user3
            };

            Incident? incident = null;
            Ticket? ticket = null;
            if (linkIncident && incidents.Count > 0)
            {
                incident = incidents[rnd.Next(incidents.Count)];
            }
            if (linkTicket && tickets.Count > 0)
            {
                ticket = tickets[rnd.Next(tickets.Count)];
            }

            var createdAt = now.AddDays(-rnd.Next(1, 15));
            var completed = status == ActionStatus.Completed;
            var dueDate = createdAt.AddDays(daysDue);
            DateTime? completedAt = completed ? createdAt.AddDays(daysDue - 1) : null;

            context.ActionItems.Add(new ActionItem
            {
                ActionNumber = $"ACT-{actionCount++:D3}",
                Title = title,
                Description = desc,
                Priority = priority,
                Status = status,
                DueDate = dueDate,
                AssignedToId = assignedTo.Id,
                IncidentId = incident?.Id,
                TicketId = ticket?.Id,
                CreatedById = manager.Id,
                CompletionNotes = completed ? "Completed and verified by the site safety team." : null,
                CompletedAt = completedAt,
                CreatedAt = createdAt,
                UpdatedAt = completed ? completedAt : createdAt.AddHours(rnd.Next(4, 48))
            });
        }
    }

    private static async Task<List<Product>> SeedProductsAsync(ApplicationDbContext context)
    {
        var catalog = AccessoryCatalog.Build();

        var existing = await context.Products.ToListAsync();
        var existingCodes = existing.Select(p => p.ProductCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var catalogCodes = catalog.Select(p => p.ProductCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (existing.Count == 0 || !catalogCodes.IsSubsetOf(existingCodes))
        {
            var queries = await context.SiteQueries.Where(q => q.ProductId != null).ToListAsync();
            foreach (var query in queries)
            {
                query.ProductId = null;
                query.ProductCode = null;
            }

            context.Products.RemoveRange(existing);
            await context.SaveChangesAsync();

            context.Products.AddRange(catalog);
            await context.SaveChangesAsync();
            return catalog.OrderBy(p => p.ProductCode).ToList();
        }

        var catalogByCode = catalog.ToDictionary(p => p.ProductCode, p => p.ImagePath, StringComparer.OrdinalIgnoreCase);
        var imagePathChanged = false;
        foreach (var product in existing)
        {
            var expected = catalogByCode.GetValueOrDefault(product.ProductCode);
            if (string.IsNullOrWhiteSpace(product.ImagePath) && !string.IsNullOrWhiteSpace(expected))
            {
                product.ImagePath = expected;
                imagePathChanged = true;
            }
        }

        if (imagePathChanged)
        {
            await context.SaveChangesAsync();
        }

        return existing.OrderBy(p => p.ProductCode).ToList();
    }

    private static async Task SeedSiteQueriesAsync(
        ApplicationDbContext context,
        AppUser manager,
        AppUser user1,
        AppUser user2,
        AppUser user3,
        List<Product> products)
    {
        var rnd = new Random(21);
        var now = DateTime.UtcNow;
        var engineers = new[] { user1, user2, user3 };

        var projects = new[]
        {
            "Reliance E-1", "Golkonda Tattvam", "SRR Khammam", "Hallmark", "North Star",
            "Vijayawada Towers", "My Home Tridasa", "Prestige Lakeside", "Lodha Crown", "Godrej Woods",
            "Sobha Hibiscus", "Aparna CyberLife", "Tanishq Residences", "DLF Galleria", "Brigade Metropolis",
            "Purva Panorama", "Concorde Orion", "Ramky One"
        };

        var missing = new[]
        {
            "Item listed in the approved BOQ is not delivered to site.",
            "Material count on site falls short of the dispatch note quantity.",
            "Required item has not reached the site despite confirmed dispatch.",
            "Fabricated element is absent from the delivered batch."
        };

        var production = new[]
        {
            "Fabricated profile dimensions are out of tolerance.",
            "Surface finish does not match the approved sample.",
            "Powder coating shade mismatch with the client approval.",
            "Machining slots and holes are incorrectly positioned."
        };

        var design = new[]
        {
            "Mullion depth clashes with the structural bracket layout.",
            "Section size conflicts with the approved shop drawing.",
            "Glazing pocket width is smaller than the glass thickness specified.",
            "Frame junctions do not align with the elevation detail."
        };

        var dispatch = new[]
        {
            "Consignment dispatched but delivery documents are missing.",
            "Dispatch quantity differs from the packing list.",
            "Material shipped to the wrong project location.",
            "Delivery is pending because the transport allocation failed."
        };

        var seedRows = new List<(string Ipo, string Project, QueryCategory Category, int Delay, bool Resolved)>
        {
            ("556", "Hallmark", QueryCategory.Missing, 45, false),
            ("571", "SRR Khammam", QueryCategory.Missing, 46, false),
            ("565", "Golkonda Tattvam", QueryCategory.Missing, 46, false),
            ("561", "Reliance E-1", QueryCategory.Missing, 61, false),
            ("535", "North Star", QueryCategory.Missing, 32, false)
        };

        var categories = new[] { QueryCategory.Missing, QueryCategory.Missing, QueryCategory.Missing, QueryCategory.Missing, QueryCategory.ProductionMistake, QueryCategory.ProductionMistake, QueryCategory.DesignMistake, QueryCategory.DispatchMissing };

        var resolvedCount = 0;
        while (seedRows.Count < 50)
        {
            var resolved = rnd.Next(100) < 26 && resolvedCount < 12;
            var category = categories[rnd.Next(categories.Length)];
            var project = projects[rnd.Next(projects.Length)];
            seedRows.Add(($"{(500 + rnd.Next(80))}", project, category, rnd.Next(3, 62), resolved));
            if (resolved) resolvedCount++;
        }

        int queryCount = 1;
        foreach (var (ipo, project, category, delay, resolved) in seedRows)
        {
            var raisedBy = engineers[rnd.Next(engineers.Length)];
            var createdAt = now.AddDays(-delay).AddHours(-rnd.Next(0, 10));

            var description = category switch
            {
                QueryCategory.Missing => missing[rnd.Next(missing.Length)],
                QueryCategory.ProductionMistake => production[rnd.Next(production.Length)],
                QueryCategory.DesignMistake => design[rnd.Next(design.Length)],
                _ => dispatch[rnd.Next(dispatch.Length)]
            };

            var product = products.Count > 0 ? products[rnd.Next(products.Count)] : null;

            var status = resolved ? QueryStatus.Resolved : (rnd.Next(100) < 20 ? QueryStatus.InProgress : QueryStatus.Pending);
            DateTime? resolvedAt = resolved ? createdAt.AddDays(delay) : null;
            DateTime? updatedAt = resolved ? resolvedAt : createdAt.AddDays(rnd.Next(1, Math.Max(2, delay / 2)));

            var query = new SiteQuery
            {
                QueryNumber = $"QY-{queryCount++:D3}",
                IpoNumber = ipo,
                Project = project,
                Category = category,
                Status = status,
                Description = description,
                QuantityNos = rnd.Next(1, 25),
                QuantitySqm = Math.Round((decimal)(rnd.NextDouble() * 8 + 0.5), 2),
                ProductId = product?.Id,
                ProductCode = product?.ProductCode,
                SlabTargetDate = createdAt.AddDays(7 + rnd.Next(0, 30)),
                SlabCompletedDate = resolved ? createdAt.AddDays(delay) : (rnd.Next(100) < 40 ? createdAt.AddDays(rnd.Next(3, delay)) : (DateTime?)null),
                RaisedById = raisedBy.Id,
                ResolvedById = resolved ? manager.Id : null,
                ResolvedAt = resolvedAt,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            if (query.SlabCompletedDate.HasValue)
            {
                query.SlabDelayDays = Math.Max(0, (int)(query.SlabCompletedDate.Value - query.CreatedAt).TotalDays - rnd.Next(0, 10));
            }

            context.SiteQueries.Add(query);

            if (rnd.Next(100) < 60)
            {
                context.QueryComments.Add(new QueryComment
                {
                    SiteQuery = query,
                    UserId = raisedBy.Id,
                    CreatedAt = createdAt.AddHours(rnd.Next(1, 6)),
                    Body = $"Query raised on-site with photo evidence (qty {query.QuantityNos} nos / {query.QuantitySqm} sqm)."
                });
            }

            if (resolved && rnd.Next(100) < 70)
            {
                context.QueryComments.Add(new QueryComment
                {
                    SiteQuery = query,
                    UserId = manager.Id,
                    CreatedAt = resolvedAt!.Value.AddHours(rnd.Next(1, 8)),
                    Body = "Dispatched and verified at site. Query marked resolved by the manager."
                });
            }

            context.AuditLogs.Add(new AuditLog
            {
                UserId = raisedBy.Id,
                UserName = raisedBy.FullName,
                Action = "Create",
                EntityType = "SiteQuery",
                EntityId = query.QueryNumber,
                Details = $"Query {query.QueryNumber} raised for {project} (IPO {ipo})",
            });
        }
    }
}