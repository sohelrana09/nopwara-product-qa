using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.NopwaraQA.Models.Admin;
using Nop.Plugin.Misc.NopwaraQA.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Core;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.NopwaraQA.Controllers;

[Area(AreaNames.ADMIN)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
public class NopwaraQAAdminController : BasePluginController
{
    #region Fields

    private readonly ICustomerService _customerService;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationService _notificationService;
    private readonly IProductService _productService;
    private readonly ISettingService _settingService;
    private readonly IWorkContext _workContext;
    private readonly NopwaraQASettings _settings;
    private readonly ProductQuestionService _questionService;

    #endregion

    #region Ctor

    public NopwaraQAAdminController(ICustomerService customerService,
        IDateTimeHelper dateTimeHelper,
        ILocalizationService localizationService,
        INotificationService notificationService,
        IProductService productService,
        ISettingService settingService,
        IWorkContext workContext,
        NopwaraQASettings settings,
        ProductQuestionService questionService)
    {
        _customerService = customerService;
        _dateTimeHelper = dateTimeHelper;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _productService = productService;
        _settingService = settingService;
        _workContext = workContext;
        _settings = settings;
        _questionService = questionService;
    }

    #endregion

    #region Utilities

    private const string VIEWS = $"~/Plugins/{NopwaraQADefaults.PLUGIN_FOLDER}/Views/Admin/";

    private async Task<ProductQuestionModel> PrepareModelAsync(Domain.ProductQuestion question)
    {
        var product = await _productService.GetProductByIdAsync(question.ProductId);
        var customer = await _customerService.GetCustomerByIdAsync(question.CustomerId);
        var customerInfo = customer == null
            ? string.Empty
            : await _customerService.IsGuestAsync(customer)
                ? await _localizationService.GetResourceAsync("Admin.Customers.Guest")
                : customer.Email;

        return new ProductQuestionModel
        {
            Id = question.Id,
            ProductId = question.ProductId,
            ProductName = product?.Name,
            CustomerId = question.CustomerId,
            CustomerInfo = string.IsNullOrEmpty(question.CustomerName) ? customerInfo : $"{question.CustomerName} ({customerInfo})",
            QuestionText = question.QuestionText,
            AnswerText = question.AnswerText,
            IsApproved = question.IsApproved,
            Answered = !string.IsNullOrWhiteSpace(question.AnswerText),
            CreatedOn = await _dateTimeHelper.ConvertToUserTimeAsync(question.CreatedOnUtc, DateTimeKind.Utc)
        };
    }

    #endregion

    #region Questions

    [CheckPermission(StandardPermission.Catalog.PRODUCT_REVIEWS_VIEW)]
    public async Task<IActionResult> List()
    {
        var model = new ProductQuestionSearchModel();
        model.AvailableStatuses.Add(new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.Search.Status.All"), "0"));
        model.AvailableStatuses.Add(new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.Search.Status.Unanswered"), "1"));
        model.AvailableStatuses.Add(new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.Search.Status.Answered"), "2"));
        model.AvailableStatuses.Add(new SelectListItem(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.Search.Status.Unpublished"), "3"));
        model.SetGridPageSize();

        return View(VIEWS + "List.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCT_REVIEWS_VIEW)]
    public async Task<IActionResult> QuestionList(ProductQuestionSearchModel searchModel)
    {
        bool? answered = searchModel.SearchStatus switch { 1 => false, 2 => true, _ => null };
        bool? approved = searchModel.SearchStatus == 3 ? false : null;

        var questions = await _questionService.SearchQuestionsAsync(answered: answered, approved: approved, text: searchModel.SearchText,
            pageIndex: searchModel.Page - 1, pageSize: searchModel.PageSize);

        var model = await new ProductQuestionListModel().PrepareToGridAsync(searchModel, questions,
            () => questions.SelectAwait(async question => await PrepareModelAsync(question)));

        return Json(model);
    }

    [CheckPermission(StandardPermission.Catalog.PRODUCT_REVIEWS_VIEW)]
    public async Task<IActionResult> Edit(int id)
    {
        var question = await _questionService.GetQuestionByIdAsync(id);
        if (question == null)
            return RedirectToAction("List");

        return View(VIEWS + "Edit.cshtml", await PrepareModelAsync(question));
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(StandardPermission.Catalog.PRODUCT_REVIEWS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> Edit(ProductQuestionModel model, bool continueEditing)
    {
        var question = await _questionService.GetQuestionByIdAsync(model.Id);
        if (question == null)
            return RedirectToAction("List");

        var answer = model.AnswerText?.Trim();
        var newlyAnswered = string.IsNullOrEmpty(question.AnswerText) && !string.IsNullOrEmpty(answer);

        question.QuestionText = string.IsNullOrWhiteSpace(model.QuestionText) ? question.QuestionText : model.QuestionText.Trim();
        question.AnswerText = answer;
        question.IsApproved = model.IsApproved;
        if (newlyAnswered)
            question.AnsweredOnUtc = DateTime.UtcNow;
        await _questionService.UpdateQuestionAsync(question);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.Updated"));

        //tell the customer once the question is answered and published
        if (_settings.NotifyCustomer && question.IsApproved && !string.IsNullOrEmpty(question.AnswerText) && !question.CustomerNotifiedOfAnswer)
        {
            if (await _questionService.SendAnsweredNotificationAsync(question, (await _workContext.GetWorkingLanguageAsync()).Id))
            {
                question.CustomerNotifiedOfAnswer = true;
                await _questionService.UpdateQuestionAsync(question);
                _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Misc.NopwaraQA.CustomerNotified"));
            }
        }

        return continueEditing ? RedirectToAction("Edit", new { id = question.Id }) : RedirectToAction("List");
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCT_REVIEWS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> Delete(int id)
    {
        var question = await _questionService.GetQuestionByIdAsync(id);
        if (question != null)
            await _questionService.DeleteQuestionsAsync([question]);

        return RedirectToAction("List");
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCT_REVIEWS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> DeleteSelected(ICollection<int> selectedIds)
    {
        if (selectedIds == null || !selectedIds.Any())
            return NoContent();

        await _questionService.DeleteQuestionsAsync(await _questionService.GetQuestionsByIdsAsync(selectedIds));

        return Json(new { Result = true });
    }

    [HttpPost]
    [CheckPermission(StandardPermission.Catalog.PRODUCT_REVIEWS_CREATE_EDIT_DELETE)]
    public async Task<IActionResult> PublishSelected(ICollection<int> selectedIds, bool publish)
    {
        if (selectedIds == null || !selectedIds.Any())
            return NoContent();

        foreach (var question in await _questionService.GetQuestionsByIdsAsync(selectedIds))
        {
            question.IsApproved = publish;
            await _questionService.UpdateQuestionAsync(question);
        }

        return Json(new { Result = true });
    }

    #endregion

    #region Configuration

    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public IActionResult Configure()
    {
        var model = new ConfigurationModel
        {
            AllowedThemes = _settings.AllowedThemes,
            AllowGuests = _settings.AllowGuests,
            QuestionsMustBeApproved = _settings.QuestionsMustBeApproved,
            ShowUnanswered = _settings.ShowUnanswered,
            VisibleQuestions = _settings.VisibleQuestions,
            NotifyStoreOwner = _settings.NotifyStoreOwner,
            NotifyCustomer = _settings.NotifyCustomer
        };

        return View(VIEWS + "Configure.cshtml", model);
    }

    [HttpPost, ActionName("Configure")]
    [FormValueRequired("save")]
    [CheckPermission(StandardPermission.Configuration.MANAGE_PLUGINS)]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return Configure();

        _settings.AllowedThemes = model.AllowedThemes?.Trim();
        _settings.AllowGuests = model.AllowGuests;
        _settings.QuestionsMustBeApproved = model.QuestionsMustBeApproved;
        _settings.ShowUnanswered = model.ShowUnanswered;
        _settings.VisibleQuestions = Math.Max(1, model.VisibleQuestions);
        _settings.NotifyStoreOwner = model.NotifyStoreOwner;
        _settings.NotifyCustomer = model.NotifyCustomer;
        await _settingService.SaveSettingAsync(_settings);

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return Configure();
    }

    #endregion
}
