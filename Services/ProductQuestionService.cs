using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Messages;
using Nop.Data;
using Nop.Plugin.Misc.NopwaraQA.Domain;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.NopwaraQA.Services;

/// <summary>
/// Product question service (data access + email notifications)
/// </summary>
public class ProductQuestionService
{
    #region Fields

    private readonly EmailAccountSettings _emailAccountSettings;
    private readonly ICustomerService _customerService;
    private readonly IEmailAccountService _emailAccountService;
    private readonly ILanguageService _languageService;
    private readonly ILocalizationService _localizationService;
    private readonly IMessageTemplateService _messageTemplateService;
    private readonly IMessageTokenProvider _messageTokenProvider;
    private readonly IProductService _productService;
    private readonly IRepository<ProductQuestion> _questionRepository;
    private readonly IStoreService _storeService;
    private readonly IWorkflowMessageService _workflowMessageService;
    private readonly MessagesSettings _messagesSettings;

    #endregion

    #region Ctor

    public ProductQuestionService(EmailAccountSettings emailAccountSettings,
        ICustomerService customerService,
        IEmailAccountService emailAccountService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        IMessageTemplateService messageTemplateService,
        IMessageTokenProvider messageTokenProvider,
        IProductService productService,
        IRepository<ProductQuestion> questionRepository,
        IStoreService storeService,
        IWorkflowMessageService workflowMessageService,
        MessagesSettings messagesSettings)
    {
        _emailAccountSettings = emailAccountSettings;
        _customerService = customerService;
        _emailAccountService = emailAccountService;
        _languageService = languageService;
        _localizationService = localizationService;
        _messageTemplateService = messageTemplateService;
        _messageTokenProvider = messageTokenProvider;
        _productService = productService;
        _questionRepository = questionRepository;
        _storeService = storeService;
        _workflowMessageService = workflowMessageService;
        _messagesSettings = messagesSettings;
    }

    #endregion

    #region Utilities

    //the next four helpers mirror nopCommerce 4.90's WorkflowMessageService, where they are protected (public in 5.00)

    private async Task<int> EnsureLanguageIsActiveAsync(int languageId, int storeId)
    {
        var language = await _languageService.GetLanguageByIdAsync(languageId);
        if (language == null || !language.Published)
            language = (await _languageService.GetAllLanguagesAsync(storeId: storeId)).FirstOrDefault();
        if (language == null || !language.Published)
            language = (await _languageService.GetAllLanguagesAsync()).FirstOrDefault();

        return language?.Id ?? throw new Exception("No active language could be loaded");
    }

    private async Task<IList<MessageTemplate>> GetActiveMessageTemplatesAsync(string messageTemplateName, int storeId)
    {
        var messageTemplates = await _messageTemplateService.GetMessageTemplatesByNameAsync(messageTemplateName, storeId);

        return messageTemplates?.Where(messageTemplate => messageTemplate.IsActive).ToList() ?? new List<MessageTemplate>();
    }

    private async Task<EmailAccount> GetEmailAccountOfMessageTemplateAsync(MessageTemplate messageTemplate, int languageId)
    {
        //the localized "Email account" dropdown saves 0 for "Standard"
        var emailAccountId = await _localizationService.GetLocalizedAsync(messageTemplate, mt => mt.EmailAccountId, languageId);
        if (emailAccountId == 0)
            emailAccountId = messageTemplate.EmailAccountId;

        return (await _emailAccountService.GetEmailAccountByIdAsync(emailAccountId)
                ?? await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId))
            ?? (await _emailAccountService.GetAllEmailAccountsAsync()).FirstOrDefault();
    }

    private async Task<(string email, string name)> GetStoreOwnerNameAndEmailAsync(EmailAccount messageTemplateEmailAccount)
    {
        var storeOwnerEmailAccount = _messagesSettings.UseDefaultEmailAccountForSendStoreOwnerEmails
            ? await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId)
            : null;
        storeOwnerEmailAccount ??= messageTemplateEmailAccount;

        return (storeOwnerEmailAccount.Email, storeOwnerEmailAccount.DisplayName);
    }

    private async Task<IList<Token>> PrepareTokensAsync(ProductQuestion question, Product product, int languageId, EmailAccount emailAccount)
    {
        var tokens = new List<Token>();
        var store = await _storeService.GetStoreByIdAsync(question.StoreId) ?? (await _storeService.GetAllStoresAsync()).First();
        await _messageTokenProvider.AddStoreTokensAsync(tokens, store, emailAccount, languageId);
        await _messageTokenProvider.AddProductTokensAsync(tokens, product, languageId);
        await _messageTokenProvider.AddCustomerTokensAsync(tokens, question.CustomerId);

        tokens.Add(new Token("NopwaraQA.Question", question.QuestionText));
        tokens.Add(new Token("NopwaraQA.Answer", question.AnswerText ?? string.Empty));
        tokens.Add(new Token("NopwaraQA.CustomerName", question.CustomerName ?? string.Empty));

        return tokens;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the published questions of a product, newest first
    /// </summary>
    public async Task<IList<ProductQuestion>> GetPublishedQuestionsAsync(int productId, int storeId, bool includeUnanswered)
    {
        return await _questionRepository.GetAllAsync(query =>
        {
            query = query.Where(q => q.ProductId == productId && q.IsApproved && (q.StoreId == 0 || q.StoreId == storeId));
            if (!includeUnanswered)
                query = query.Where(q => q.AnswerText != null && q.AnswerText != string.Empty);

            return query.OrderByDescending(q => q.AnsweredOnUtc).ThenByDescending(q => q.CreatedOnUtc);
        });
    }

    /// <summary>
    /// Searches questions (admin)
    /// </summary>
    public async Task<IPagedList<ProductQuestion>> SearchQuestionsAsync(int productId = 0, bool? answered = null, bool? approved = null,
        string text = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _questionRepository.GetAllPagedAsync(query =>
        {
            if (productId > 0)
                query = query.Where(q => q.ProductId == productId);
            if (answered.HasValue)
                query = answered.Value
                    ? query.Where(q => q.AnswerText != null && q.AnswerText != string.Empty)
                    : query.Where(q => q.AnswerText == null || q.AnswerText == string.Empty);
            if (approved.HasValue)
                query = query.Where(q => q.IsApproved == approved.Value);
            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(q => q.QuestionText.Contains(text) || q.AnswerText.Contains(text));

            return query.OrderByDescending(q => q.CreatedOnUtc);
        }, pageIndex, pageSize);
    }

    /// <summary>
    /// Counts the questions a customer asked since the given time (flood protection)
    /// </summary>
    public async Task<int> CountRecentQuestionsAsync(int customerId, DateTime sinceUtc)
    {
        return (await _questionRepository.GetAllAsync(query => query.Where(q => q.CustomerId == customerId && q.CreatedOnUtc >= sinceUtc))).Count;
    }

    public async Task<ProductQuestion> GetQuestionByIdAsync(int id)
    {
        return await _questionRepository.GetByIdAsync(id);
    }

    public async Task<IList<ProductQuestion>> GetQuestionsByIdsAsync(ICollection<int> ids)
    {
        return await _questionRepository.GetByIdsAsync(ids.ToArray());
    }

    public async Task InsertQuestionAsync(ProductQuestion question)
    {
        await _questionRepository.InsertAsync(question);
    }

    public async Task UpdateQuestionAsync(ProductQuestion question)
    {
        await _questionRepository.UpdateAsync(question);
    }

    public async Task DeleteQuestionsAsync(IList<ProductQuestion> questions)
    {
        await _questionRepository.DeleteAsync(questions);
    }

    /// <summary>
    /// Emails the store owner about a new question
    /// </summary>
    public async Task SendNewQuestionNotificationAsync(ProductQuestion question, int languageId)
    {
        var product = await _productService.GetProductByIdAsync(question.ProductId);
        if (product == null)
            return;

        languageId = await EnsureLanguageIsActiveAsync(languageId, question.StoreId);
        foreach (var template in await GetActiveMessageTemplatesAsync(NopwaraQADefaults.NEW_QUESTION_TEMPLATE, question.StoreId))
        {
            var emailAccount = await GetEmailAccountOfMessageTemplateAsync(template, languageId);
            var tokens = await PrepareTokensAsync(question, product, languageId, emailAccount);
            var (toEmail, toName) = await GetStoreOwnerNameAndEmailAsync(emailAccount);

            await _workflowMessageService.SendNotificationAsync(template, emailAccount, languageId, tokens, toEmail, toName);
        }
    }

    /// <summary>
    /// Emails the customer that the question has been answered (registered customers with an email only)
    /// </summary>
    public async Task<bool> SendAnsweredNotificationAsync(ProductQuestion question, int languageId)
    {
        var customer = await _customerService.GetCustomerByIdAsync(question.CustomerId);
        var product = await _productService.GetProductByIdAsync(question.ProductId);
        if (customer == null || string.IsNullOrEmpty(customer.Email) || product == null)
            return false;

        var sent = false;
        languageId = await EnsureLanguageIsActiveAsync(languageId, question.StoreId);
        foreach (var template in await GetActiveMessageTemplatesAsync(NopwaraQADefaults.ANSWERED_TEMPLATE, question.StoreId))
        {
            var emailAccount = await GetEmailAccountOfMessageTemplateAsync(template, languageId);
            var tokens = await PrepareTokensAsync(question, product, languageId, emailAccount);

            await _workflowMessageService.SendNotificationAsync(template, emailAccount, languageId, tokens, customer.Email, await _customerService.GetCustomerFullNameAsync(customer));
            sent = true;
        }

        return sent;
    }

    #endregion
}
