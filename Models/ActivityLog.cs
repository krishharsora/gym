using System;
using System.Collections.Generic;

namespace cafe.Models;

public partial class ActivityLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string? Action { get; set; }

    public string? Entity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
