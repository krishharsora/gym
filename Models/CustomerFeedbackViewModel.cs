using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class CustomerFeedbackViewModel
    {
          public List<Feedback> RecentFeedbacks { get; set; } = new();
    public Feedback NewFeedback { get; set; } = new();
    public int CartCount { get; set; }
    }
}