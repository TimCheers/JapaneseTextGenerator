using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KotobaApi.Authorization;

public class SameUserAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _routeParamName;

    public SameUserAttribute(string routeParamName) => _routeParamName = routeParamName;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var routeValue = context.RouteData.Values[_routeParamName]?.ToString();
        var currentUserId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (routeValue != currentUserId)
        {
            context.Result = new ForbidResult();
        }
    }
}
