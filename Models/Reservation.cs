using System;
using System.Collections.Generic;

namespace cafe.Models;

public partial class Reservation
{
    public int ReservationId { get; set; }

    public int? CustomerId { get; set; }

    public int? TableId { get; set; }

    public DateOnly? ReservationDate { get; set; }

    public TimeOnly? ReservationTime { get; set; }

    public int? GuestCount { get; set; }

    public string? Status { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual CafeTable? Table { get; set; }
}
