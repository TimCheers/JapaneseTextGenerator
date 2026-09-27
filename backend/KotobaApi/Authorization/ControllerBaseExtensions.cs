using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace KotobaApi.Authorization;

public static class ControllerBaseExtensions
{
    public static Guid GetCurrentUserId(this ControllerBase controller) =>
        Guid.Parse(controller.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}