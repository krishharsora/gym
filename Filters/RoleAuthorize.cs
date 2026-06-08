using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class RoleAuthorize : Attribute, IAuthorizationFilter
{
    private readonly string _role;

    public RoleAuthorize(string role)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Session.GetString("role");

        if (role == null || role != _role)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
    }
}