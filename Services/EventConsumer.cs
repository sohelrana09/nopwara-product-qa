using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.NopwaraQA.Services;

/// <summary>
/// Adds "Product questions" to the admin Catalog menu
/// </summary>
public class EventConsumer : IConsumer<AdminMenuCreatedEvent>
{
    private readonly ILocalizationService _localizationService;
    private readonly IPluginManager<IPlugin> _pluginManager;

    public EventConsumer(ILocalizationService localizationService,
        IPluginManager<IPlugin> pluginManager)
    {
        _localizationService = localizationService;
        _pluginManager = pluginManager;
    }

    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        //LoadPluginBySystemNameAsync returns only fully installed plugins (the event may run before the installation is complete)
        var plugin = await _pluginManager.LoadPluginBySystemNameAsync(NopwaraQADefaults.SYSTEM_NAME);
        if (plugin == null)
            return;

        var catalog = eventMessage.RootMenuItem.GetItemBySystemName("Catalog");
        catalog?.InsertAfter("Product reviews", new AdminMenuItem
        {
            Visible = true,
            SystemName = NopwaraQADefaults.ADMIN_MENU_SYSTEM_NAME,
            Title = await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.Questions"),
            Url = eventMessage.GetMenuItemUrl("NopwaraQAAdmin", "List"),
            IconClass = "far fa-dot-circle",
            PermissionNames = new List<string> { StandardPermission.Catalog.PRODUCT_REVIEWS_VIEW }
        });
    }
}
