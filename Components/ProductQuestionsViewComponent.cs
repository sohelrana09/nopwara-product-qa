using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Core.Http;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.NopwaraQA.Models;
using Nop.Plugin.Misc.NopwaraQA.Services;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Themes;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Mvc.Routing;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.NopwaraQA.Components;

/// <summary>
/// Renders the "Questions & answers" section of a product page
/// </summary>
public class ProductQuestionsViewComponent : NopViewComponent
{
    #region Fields

    private readonly CaptchaSettings _captchaSettings;
    private readonly ICustomerService _customerService;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ILocalizationService _localizationService;
    private readonly INopFileProvider _fileProvider;
    private readonly INopUrlHelper _nopUrlHelper;
    private readonly IStoreContext _storeContext;
    private readonly IThemeContext _themeContext;
    private readonly IWorkContext _workContext;
    private readonly NopwaraQASettings _settings;
    private readonly ProductQuestionService _questionService;
    private readonly SecuritySettings _securitySettings;

    #endregion

    #region Ctor

    public ProductQuestionsViewComponent(CaptchaSettings captchaSettings,
        ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        INopFileProvider fileProvider,
        INopUrlHelper nopUrlHelper,
        IStoreContext storeContext,
        IThemeContext themeContext,
        IWorkContext workContext,
        NopwaraQASettings settings,
        ProductQuestionService questionService,
        SecuritySettings securitySettings)
    {
        _captchaSettings = captchaSettings;
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _fileProvider = fileProvider;
        _nopUrlHelper = nopUrlHelper;
        _storeContext = storeContext;
        _themeContext = themeContext;
        _workContext = workContext;
        _settings = settings;
        _questionService = questionService;
        _securitySettings = securitySettings;
    }

    #endregion

    #region Methods

    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        if (additionalData is not ProductDetailsModel product)
            return Content(string.Empty);

        //the markup is styled by the theme, so only render it for the allowed themes
        var theme = await _themeContext.GetWorkingThemeNameAsync();
        var allowed = (_settings.AllowedThemes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(theme, StringComparer.OrdinalIgnoreCase);
        if (!allowed)
            return Content(string.Empty);

        var store = await _storeContext.GetCurrentStoreAsync();
        var customer = await _workContext.GetCurrentCustomerAsync();
        var guestName = await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.Guest");

        var model = new ProductQuestionsModel
        {
            ProductId = product.Id,
            ProductName = product.Name,
            VisibleQuestions = Math.Max(1, _settings.VisibleQuestions),
            CanAsk = _settings.AllowGuests || !await _customerService.IsGuestAsync(customer),
            LoginUrl = _nopUrlHelper.RouteUrl(NopRouteNames.General.LOGIN, new { returnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString }),
            AskUrl = _nopUrlHelper.RouteUrl(NopwaraQADefaults.ASK_ROUTE_NAME, new { productId = product.Id }),
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnProductReviewPage,
            HoneypotEnabled = _securitySettings.HoneypotEnabled
        };

        foreach (var question in await _questionService.GetPublishedQuestionsAsync(product.Id, store.Id, _settings.ShowUnanswered))
        {
            model.Questions.Add(new QuestionItemModel
            {
                Id = question.Id,
                Question = question.QuestionText,
                Answer = question.AnswerText,
                AskedBy = string.IsNullOrWhiteSpace(question.CustomerName) ? guestName : question.CustomerName,
                AskedOn = (await _dateTimeHelper.ConvertToUserTimeAsync(question.CreatedOnUtc, DateTimeKind.Utc)).ToString("D")
            });
        }

        //FAQPage structured data for the answered questions
        var answered = model.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Answer)).ToList();
        if (answered.Any())
        {
            model.JsonLd = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "FAQPage",
                ["mainEntity"] = answered.Select(q => new Dictionary<string, object>
                {
                    ["@type"] = "Question",
                    ["name"] = q.Question,
                    ["acceptedAnswer"] = new Dictionary<string, object> { ["@type"] = "Answer", ["text"] = q.Answer }
                })
            });
        }

        //themes can override the view at Themes/{theme}/Views/Plugins/NopwaraQA/ProductQuestions.cshtml
        var themeView = $"~/Themes/{theme}/Views/Plugins/{NopwaraQADefaults.THEME_VIEWS_FOLDER}/ProductQuestions.cshtml";
        var view = _fileProvider.FileExists(_fileProvider.MapPath(themeView))
            ? themeView
            : $"~/Plugins/{NopwaraQADefaults.PLUGIN_FOLDER}/Views/ProductQuestions.cshtml";

        return View(view, model);
    }

    #endregion
}
