using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class CategoryViewModel
    {
         public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public string Description { get; set; }
  
    public int ItemCount { get; set; } 
    }
}