using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cafe.Models
{
    public class MenuViewModel
    {
         public List<MenuItem> MenuItems { get; set; }
        public List<MenuCategory> Categories { get; set; }
        public int CartCount { get; set; } 
    }
}