using IForm.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IForm.Web.Data;

public static class DbSeeder
{
    public static readonly string AdminRole = "Admin";
    public static readonly string ManagerRole = "Manager";
    public static readonly string UserRole = "User";

    public static async Task SeedAsync(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        await context.Database.MigrateAsync();

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

        await context.SaveChangesAsync();
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

        var existing = await context.Products.AsNoTracking().ToListAsync();
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
                CreatedAt = createdAt
            });
        }
    }
}
