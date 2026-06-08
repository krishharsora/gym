using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using cafe.Models;

namespace cafe.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
         ViewBag.SliderImages = new List<string>
            {
                "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?w=1200",
                "https://images.unsplash.com/photo-1571896349840-0d6f5f44a7a7?w=1200",
                "https://images.unsplash.com/photo-1556909114-f6e7ad7d3133?w=1200",
                "https://images.unsplash.com/photo-1514320291840-2e0a9bf2a9ae?w=1200",
                "https://images.unsplash.com/photo-1470339712915-56a8b4a382ee?w=1200"
            };

            ViewBag.CafeInfo = new CafeInfo
            {
                Name = "CafeXpert",
                Address = "123 Cafe Street, Business District, City Center, 400001",
                Phone = "+91 98765 43210",
                Email = "info@cafexpert.com",
                Stats = new Dictionary<string, int>
                {
                    { "Cafes Served", 500 },
                    { "Uptime", 999 },
                    { "Support", 24 }
                }
            };

            return View();
        }
    }

    public class CafeInfo
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public Dictionary<string, int> Stats { get; set; }
    }

   


