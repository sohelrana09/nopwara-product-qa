using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Messages;
using Nop.Plugin.Misc.NopwaraQA.Components;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Plugins;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.NopwaraQA;

/// <summary>
/// Represents the Nopwara product Q&A plugin
/// </summary>
public class NopwaraQAPlugin : BasePlugin, IWidgetPlugin
{
    #region Fields

    private readonly EmailAccountSettings _emailAccountSettings;
    private readonly ILocalizationService _localizationService;
    private readonly IMessageTemplateService _messageTemplateService;
    private readonly INopUrlHelper _nopUrlHelper;
    private readonly ISettingService _settingService;
    private readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public NopwaraQAPlugin(EmailAccountSettings emailAccountSettings,
        ILocalizationService localizationService,
        IMessageTemplateService messageTemplateService,
        INopUrlHelper nopUrlHelper,
        ISettingService settingService,
        WidgetSettings widgetSettings)
    {
        _emailAccountSettings = emailAccountSettings;
        _localizationService = localizationService;
        _messageTemplateService = messageTemplateService;
        _nopUrlHelper = nopUrlHelper;
        _settingService = settingService;
        _widgetSettings = widgetSettings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets widget zones where this widget should be rendered
    /// </summary>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        //rendered by the Nopwara product templates after the description / specifications / reviews tabs
        return Task.FromResult<IList<string>>(new List<string> { NopwaraQADefaults.PRODUCT_AFTER_TABS_ZONE });
    }

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        return typeof(ProductQuestionsViewComponent);
    }

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _nopUrlHelper.RouteUrl(NopwaraQADefaults.CONFIGURATION_ROUTE_NAME);
    }

    /// <summary>
    /// Install the plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new NopwaraQASettings
        {
            AllowedThemes = "Nopwara",
            AllowGuests = false,
            QuestionsMustBeApproved = true,
            ShowUnanswered = false,
            VisibleQuestions = 5,
            NotifyStoreOwner = true,
            NotifyCustomer = true
        });

        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(NopwaraQADefaults.SYSTEM_NAME))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(NopwaraQADefaults.SYSTEM_NAME);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        //email templates (editable in Content management > Message templates)
        var templates = new[]
        {
            new MessageTemplate
            {
                Name = NopwaraQADefaults.NEW_QUESTION_TEMPLATE,
                Subject = "%Store.Name%. New question about \"%Product.Name%\"",
                Body = "<p><a href=\"%Store.URL%\">%Store.Name%</a></p><p>%NopwaraQA.CustomerName% asked a question about <a href=\"%Product.ProductURLForCustomer%\">%Product.Name%</a>:</p><blockquote>%NopwaraQA.Question%</blockquote><p>Answer it in admin: Catalog &gt; Product questions.</p>",
                IsActive = true,
                EmailAccountId = _emailAccountSettings.DefaultEmailAccountId
            },
            new MessageTemplate
            {
                Name = NopwaraQADefaults.ANSWERED_TEMPLATE,
                Subject = "%Store.Name%. Your question about \"%Product.Name%\" has been answered",
                Body = "<p><a href=\"%Store.URL%\">%Store.Name%</a></p><p>Hello %Customer.FullName%,</p><p>You asked about <a href=\"%Product.ProductURLForCustomer%\">%Product.Name%</a>:</p><blockquote>%NopwaraQA.Question%</blockquote><p>Our answer:</p><blockquote>%NopwaraQA.Answer%</blockquote>",
                IsActive = true,
                EmailAccountId = _emailAccountSettings.DefaultEmailAccountId
            }
        };
        foreach (var template in templates)
        {
            if (!(await _messageTemplateService.GetMessageTemplatesByNameAsync(template.Name)).Any())
                await _messageTemplateService.InsertMessageTemplateAsync(template);
        }

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            //admin
            ["Plugins.Misc.NopwaraQA.Questions"] = "Product questions",
            ["Plugins.Misc.NopwaraQA.Instructions"] = "Customers ask questions on the product page; answer and publish them in Catalog > Product questions. The section is rendered by the Nopwara product templates in the \"nopwara_productdetails_after_tabs\" widget zone. Email templates: Content management > Message templates (NopwaraQA.*).",
            ["Plugins.Misc.NopwaraQA.Fields.AllowedThemes"] = "Themes",
            ["Plugins.Misc.NopwaraQA.Fields.AllowedThemes.Hint"] = "Comma-separated theme system names the Q&A section is used with (its markup is styled by the theme).",
            ["Plugins.Misc.NopwaraQA.Fields.AllowGuests"] = "Allow guests to ask",
            ["Plugins.Misc.NopwaraQA.Fields.AllowGuests.Hint"] = "Let guests ask questions (registered customers can always ask).",
            ["Plugins.Misc.NopwaraQA.Fields.QuestionsMustBeApproved"] = "Questions must be approved",
            ["Plugins.Misc.NopwaraQA.Fields.QuestionsMustBeApproved.Hint"] = "New questions are published only after approval in admin.",
            ["Plugins.Misc.NopwaraQA.Fields.ShowUnanswered"] = "Show unanswered questions",
            ["Plugins.Misc.NopwaraQA.Fields.ShowUnanswered.Hint"] = "Publish approved questions before they are answered.",
            ["Plugins.Misc.NopwaraQA.Fields.VisibleQuestions"] = "Questions shown",
            ["Plugins.Misc.NopwaraQA.Fields.VisibleQuestions.Hint"] = "The number of questions shown before \"Show more\".",
            ["Plugins.Misc.NopwaraQA.Fields.NotifyStoreOwner"] = "Notify store owner",
            ["Plugins.Misc.NopwaraQA.Fields.NotifyStoreOwner.Hint"] = "Email the store owner when a question is asked.",
            ["Plugins.Misc.NopwaraQA.Fields.NotifyCustomer"] = "Notify customer",
            ["Plugins.Misc.NopwaraQA.Fields.NotifyCustomer.Hint"] = "Email the customer when the question is answered.",
            ["Plugins.Misc.NopwaraQA.Fields.Product"] = "Product",
            ["Plugins.Misc.NopwaraQA.Fields.Customer"] = "Customer",
            ["Plugins.Misc.NopwaraQA.Fields.QuestionText"] = "Question",
            ["Plugins.Misc.NopwaraQA.Fields.AnswerText"] = "Answer",
            ["Plugins.Misc.NopwaraQA.Fields.AnswerText.Hint"] = "The answer shown under the question on the product page.",
            ["Plugins.Misc.NopwaraQA.Fields.IsApproved"] = "Published",
            ["Plugins.Misc.NopwaraQA.Fields.IsApproved.Hint"] = "Show the question on the product page.",
            ["Plugins.Misc.NopwaraQA.Fields.CreatedOn"] = "Asked on",
            ["Plugins.Misc.NopwaraQA.Fields.Answered"] = "Answered",
            ["Plugins.Misc.NopwaraQA.Search.Text"] = "Text",
            ["Plugins.Misc.NopwaraQA.Search.Status"] = "Status",
            ["Plugins.Misc.NopwaraQA.Search.Status.All"] = "All",
            ["Plugins.Misc.NopwaraQA.Search.Status.Unanswered"] = "Unanswered",
            ["Plugins.Misc.NopwaraQA.Search.Status.Answered"] = "Answered",
            ["Plugins.Misc.NopwaraQA.Search.Status.Unpublished"] = "Not published",
            ["Plugins.Misc.NopwaraQA.Edit"] = "Answer question",
            ["Plugins.Misc.NopwaraQA.BackToList"] = "back to product question list",
            ["Plugins.Misc.NopwaraQA.Updated"] = "The question has been updated.",
            ["Plugins.Misc.NopwaraQA.CustomerNotified"] = "The customer has been notified of the answer.",
            ["Plugins.Misc.NopwaraQA.PublishSelected"] = "Publish selected",
            ["Plugins.Misc.NopwaraQA.UnpublishSelected"] = "Unpublish selected",

            //storefront
            ["Plugins.Misc.NopwaraQA.Title"] = "Questions & answers",
            ["Plugins.Misc.NopwaraQA.Ask"] = "Ask a question",
            ["Plugins.Misc.NopwaraQA.Ask.Placeholder"] = "What would you like to know about this product?",
            ["Plugins.Misc.NopwaraQA.Ask.Submit"] = "Submit question",
            ["Plugins.Misc.NopwaraQA.Ask.Cancel"] = "Cancel",
            ["Plugins.Misc.NopwaraQA.Ask.Thanks"] = "Thanks! Your question has been sent and will appear here once answered.",
            ["Plugins.Misc.NopwaraQA.Ask.ThanksPublished"] = "Thanks! Your question has been published.",
            ["Plugins.Misc.NopwaraQA.Ask.Required"] = "Please enter your question.",
            ["Plugins.Misc.NopwaraQA.Ask.TooLong"] = "The question is too long (500 characters maximum).",
            ["Plugins.Misc.NopwaraQA.Ask.TooMany"] = "You have asked several questions recently. Please try again later.",
            ["Plugins.Misc.NopwaraQA.Ask.LoginRequired"] = "Please log in to ask a question.",
            ["Plugins.Misc.NopwaraQA.Ask.Error"] = "Your question could not be sent. Please try again.",
            ["Plugins.Misc.NopwaraQA.None"] = "No questions yet. Be the first to ask!",
            ["Plugins.Misc.NopwaraQA.Answer"] = "Answer",
            ["Plugins.Misc.NopwaraQA.NotAnswered"] = "Not answered yet",
            ["Plugins.Misc.NopwaraQA.AskedBy"] = "Asked by {0} on {1}",
            ["Plugins.Misc.NopwaraQA.ShowMore"] = "Show all {0} questions",
            ["Plugins.Misc.NopwaraQA.Guest"] = "a customer"
        });

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall the plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        if (_widgetSettings.ActiveWidgetSystemNames.Contains(NopwaraQADefaults.SYSTEM_NAME))
        {
            _widgetSettings.ActiveWidgetSystemNames.Remove(NopwaraQADefaults.SYSTEM_NAME);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        foreach (var name in new[] { NopwaraQADefaults.NEW_QUESTION_TEMPLATE, NopwaraQADefaults.ANSWERED_TEMPLATE })
        {
            foreach (var template in await _messageTemplateService.GetMessageTemplatesByNameAsync(name))
                await _messageTemplateService.DeleteMessageTemplateAsync(template);
        }

        await _settingService.DeleteSettingAsync<NopwaraQASettings>();
        await _localizationService.DeleteLocaleResourcesAsync("Plugins.Misc.NopwaraQA");

        await base.UninstallAsync();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
    /// </summary>
    public bool HideInWidgetList => false;

    #endregion
}
