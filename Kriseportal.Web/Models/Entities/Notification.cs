using System.ComponentModel.DataAnnotations;

namespace Kriseportal.Web.Models.Entities;

public class Notification
{
    public int NotificationId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    [Required, MaxLength(200)]
    public string Message { get; set; } = string.Empty;

    public int? RequestId { get; set; }

    public int? NeedId { get; set; }

    public bool IsUrgent { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}