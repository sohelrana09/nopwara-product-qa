using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Plugin.Misc.NopwaraQA.Domain;
using Nop.Plugin.Misc.NopwaraQA.Services;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.NopwaraQA.Controllers;

/// <summary>
/// Storefront endpoint for asking a question (ajax)
/// </summary>
[AutoValidateAntiforgeryToken]
public class NopwaraQAController : BasePublicController
{
    #region Constants

    private const int MAX_QUESTION_LENGTH = 500;

    //the shown name column is 200 characters
    private const int MAX_NAME_LENGTH = 200;

    //at most this many questions per customer per hour (each one emails the store owner)
    private const int MAX_QUESTIONS_PER_HOUR = 5;

    #endregion

    #region Fields

    private readonly CaptchaSettings _captchaSettings;
    private readonly IAclService _aclService;
    private readonly ICustomerService _customerService;
    private readonly ILocalizationService _localizationService;
    private readonly IProductService _productService;
    private readonly IStoreContext _storeContext;
    private readonly IStoreMappingService _storeMappingService;
    private readonly IWorkContext _workContext;
    private readonly NopwaraQASettings _settings;
    private readonly ProductQuestionService _questionService;

    #endregion

    #region Ctor

    public NopwaraQAController(CaptchaSettings captchaSettings,
        IAclService aclService,
        ICustomerService customerService,
        ILocalizationService localizationService,
        IProductService productService,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService,
        IWorkContext workContext,
        NopwaraQASettings settings,
        ProductQuestionService questionService)
    {
        _captchaSettings = captchaSettings;
        _aclService = aclService;
        _customerService = customerService;
        _localizationService = localizationService;
        _productService = productService;
        _storeContext = storeContext;
        _storeMappingService = storeMappingService;
        _workContext = workContext;
        _settings = settings;
        _questionService = questionService;
    }

    #endregion

    #region Utilities

    private async Task<IActionResult> ErrorAsync(string resource)
    {
        return Json(new { success = false, message = await _localizationService.GetResourceAsync(resource) });
    }

    #endregion

    #region Methods

    [HttpPost]
    [ValidateCaptcha]
    [ValidateHoneypot]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> Ask(int productId, string question, bool captchaValid)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        if (product == null || product.Deleted || !product.Published ||
            !await _aclService.AuthorizeAsync(product) || !await _storeMappingService.AuthorizeAsync(product))
        {
            return NotFound();
        }

        var customer = await _workContext.GetCurrentCustomerAsync();
        var isGuest = await _customerService.IsGuestAsync(customer);
        if (isGuest && !_settings.AllowGuests)
            return await ErrorAsync("Plugins.Misc.NopwaraQA.Ask.LoginRequired");

        if (_captchaSettings.Enabled && _captchaSettings.ShowOnProductReviewPage && !captchaValid)
            return await ErrorAsync("Common.WrongCaptchaMessage");

        question = question?.Trim();
        if (string.IsNullOrEmpty(question))
            return await ErrorAsync("Plugins.Misc.NopwaraQA.Ask.Required");
        if (question.Length > MAX_QUESTION_LENGTH)
            return await ErrorAsync("Plugins.Misc.NopwaraQA.Ask.TooLong");

        if (await _questionService.CountRecentQuestionsAsync(customer.Id, DateTime.UtcNow.AddHours(-1)) >= MAX_QUESTIONS_PER_HOUR)
            return await ErrorAsync("Plugins.Misc.NopwaraQA.Ask.TooMany");

        //the public name follows the store's customer name format (Customer settings > "Customer name format"), like reviews
        var name = isGuest ? null : (await _customerService.FormatUsernameAsync(customer, stripTooLong: true, maxLength: MAX_NAME_LENGTH))?.Trim();

        var store = await _storeContext.GetCurrentStoreAsync();
        var entity = new ProductQuestion
        {
            ProductId = product.Id,
            StoreId = store.Id,
            CustomerId = customer.Id,
            CustomerName = string.IsNullOrEmpty(name) ? null : name,
            QuestionText = question,
            IsApproved = !_settings.QuestionsMustBeApproved,
            CreatedOnUtc = DateTime.UtcNow
        };
        await _questionService.InsertQuestionAsync(entity);

        if (_settings.NotifyStoreOwner)
            await _questionService.SendNewQuestionNotificationAsync(entity, (await _workContext.GetWorkingLanguageAsync()).Id);

        var message = entity.IsApproved && _settings.ShowUnanswered
            ? "Plugins.Misc.NopwaraQA.Ask.ThanksPublished"
            : "Plugins.Misc.NopwaraQA.Ask.Thanks";

        return Json(new { success = true, message = await _localizationService.GetResourceAsync(message) });
    }

    #endregion
}
