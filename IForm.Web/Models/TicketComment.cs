using System.ComponentModel.DataAnnotations;

namespace IForm.Web.Models;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }

    public AppUser? User { get; set; }
}
