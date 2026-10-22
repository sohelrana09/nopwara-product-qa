using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.NopwaraQA.Infrastructure;

/// <summary>
/// Represents plugin route provider
/// </summary>
public class RouteProvider : IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapControllerRoute(name: NopwaraQADefaults.CONFIGURATION_ROUTE_NAME,
            pattern: "Admin/NopwaraQAAdmin/Configure",
            defaults: new { controller = "NopwaraQAAdmin", action = "Configure", area = AreaNames.ADMIN });

        //no language prefix, like the core ajax endpoints
        endpointRouteBuilder.MapControllerRoute(name: NopwaraQADefaults.ASK_ROUTE_NAME,
            pattern: "nopwara-qa/ask/{productId:min(0)}",
            defaults: new { controller = "NopwaraQA", action = "Ask" });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;
}
