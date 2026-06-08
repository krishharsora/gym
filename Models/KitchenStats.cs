using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class KitchenStats
    {
        public int PendingCount { get; set; }
    public int PreparingCount { get; set; }
    public int ReadyCount { get; set; }
    public int ServedCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalToday { get; set; }
    public decimal TotalRevenueToday { get; set; }
    }
}