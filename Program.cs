using Microsoft.EntityFrameworkCore;
using cafe.Models;
using Microsoft.Extensions.Options;
using Stripe;
using Microsoft.AspNetCore.Authentication.Cookies;
var builder = WebApplication.CreateBuilder(args);
StripeConfiguration.ApiKey = "sk_test_51TBctuQuyAg2bqwwUv1pvz9bVqbqjGfc31kb0AJIwVQVQVo4xyKZLcc9oObdXOj3KiVUAPCmZd3AttSIGcBhgJQE00tMQaOiU9";
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CafeManagementContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 23))
    )
);
builder.Services.AddDistributedMemoryCache();

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<EmailSettings>(
builder.Configuration.GetSection("EmailSettings"));


builder.Services.AddTransient<EmailService>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession(); 
app.UseAuthorization();

app.MapStaticAssets();
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // allow public pages
    if (path == "/" ||
        path.StartsWith("/home") ||
        path.StartsWith("/account") ||
        path.StartsWith("/css") ||
        path.StartsWith("/js") ||
        path.StartsWith("/lib") ||
        path.StartsWith("/images"))
    {
        await next();
        return;
    }

    // check login for other pages
    if (context.Session.GetString("user_id") == null)
    {
        context.Response.Redirect("/Account/Login");
        return;
    }

    await next();
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
