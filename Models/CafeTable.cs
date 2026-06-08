using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace cafe.Models;

public partial class CafeTable
{
    public int TableId { get; set; }
 [Required]
    public int TableNumber { get; set; }
[Range(1, 20, ErrorMessage = "Capacity must be between 1-20")]
    public int? Capacity { get; set; }

    public string? QrCodeUrl { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
