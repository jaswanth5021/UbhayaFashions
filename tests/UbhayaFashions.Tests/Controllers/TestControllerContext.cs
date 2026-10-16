using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UbhayaFashions.Tests.Controllers;

internal static class TestControllerContext
{
    public static ControllerContext ForCustomer(int customerId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, customerId.ToString())],
            authenticationType: "UnitTest");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
