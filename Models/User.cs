using System;
using System.Collections.Generic;

namespace cafe.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string Role { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool Status { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
