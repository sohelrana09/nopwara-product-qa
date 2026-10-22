namespace Nop.Plugin.Misc.NopwaraQA;

/// <summary>
/// Represents plugin constants
/// </summary>
public static class NopwaraQADefaults
{
    /// <summary>
    /// Gets the plugin system name
    /// </summary>
    public const string SYSTEM_NAME = "Misc.NopwaraQA";

    /// <summary>
    /// Gets the folder (under Nop.Web/Plugins) the plugin is deployed to
    /// </summary>
    public const string PLUGIN_FOLDER = "Misc.NopwaraQA";

    /// <summary>
    /// Gets the folder name themes use to override the plugin views (Themes/{theme}/Views/Plugins/{folder})
    /// </summary>
    public const string THEME_VIEWS_FOLDER = "NopwaraQA";

    /// <summary>
    /// Gets the widget zone the Nopwara product templates render after the tabs (description, specifications, reviews)
    /// </summary>
    public const string PRODUCT_AFTER_TABS_ZONE = "nopwara_productdetails_after_tabs";

    /// <summary>
    /// Gets the name of the "ask a question" (ajax) route
    /// </summary>
    public const string ASK_ROUTE_NAME = "Plugin.Misc.NopwaraQA.Ask";

    /// <summary>
    /// Gets the name of the admin configuration route
    /// </summary>
    public const string CONFIGURATION_ROUTE_NAME = "Plugin.Misc.NopwaraQA.Configure";

    /// <summary>
    /// Gets the system name of the admin menu item
    /// </summary>
    public const string ADMIN_MENU_SYSTEM_NAME = "Nopwara product questions";

    /// <summary>
    /// Gets the message template sent to the store owner when a question is asked
    /// </summary>
    public const string NEW_QUESTION_TEMPLATE = "NopwaraQA.NewQuestion.StoreOwnerNotification";

    /// <summary>
    /// Gets the message template sent to the customer when the question is answered
    /// </summary>
    public const string ANSWERED_TEMPLATE = "NopwaraQA.QuestionAnswered.CustomerNotification";
}
