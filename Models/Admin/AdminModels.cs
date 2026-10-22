using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.NopwaraQA.Models.Admin;

/// <summary>
/// Represents the product question search model
/// </summary>
public record ProductQuestionSearchModel : BaseSearchModel
{
    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Search.Text")]
    public string SearchText { get; set; }

    /// <summary>
    /// Gets or sets the status filter: 0 all, 1 unanswered, 2 answered, 3 not published
    /// </summary>
    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Search.Status")]
    public int SearchStatus { get; set; }

    public IList<SelectListItem> AvailableStatuses { get; set; } = new List<SelectListItem>();
}

/// <summary>
/// Represents the product question list model
/// </summary>
public record ProductQuestionListModel : BasePagedListModel<ProductQuestionModel>;

/// <summary>
/// Represents a product question (grid row and edit page)
/// </summary>
public record ProductQuestionModel : BaseNopEntityModel
{
    public int ProductId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.Product")]
    public string ProductName { get; set; }

    public int CustomerId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.Customer")]
    public string CustomerInfo { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.QuestionText")]
    public string QuestionText { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.AnswerText")]
    public string AnswerText { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.IsApproved")]
    public bool IsApproved { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.Answered")]
    public bool Answered { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.CreatedOn")]
    public DateTime CreatedOn { get; set; }
}

/// <summary>
/// Represents the plugin configuration model
/// </summary>
public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.AllowedThemes")]
    public string AllowedThemes { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.AllowGuests")]
    public bool AllowGuests { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.QuestionsMustBeApproved")]
    public bool QuestionsMustBeApproved { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.ShowUnanswered")]
    public bool ShowUnanswered { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.VisibleQuestions")]
    public int VisibleQuestions { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.NotifyStoreOwner")]
    public bool NotifyStoreOwner { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NopwaraQA.Fields.NotifyCustomer")]
    public bool NotifyCustomer { get; set; }
}
