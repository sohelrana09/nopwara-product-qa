using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.NopwaraQA.Models;

/// <summary>
/// Represents the Q&A section of a product page
/// </summary>
public record ProductQuestionsModel : BaseNopModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public IList<QuestionItemModel> Questions { get; set; } = new List<QuestionItemModel>();

    public int VisibleQuestions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the current customer can ask (otherwise a login link is shown)
    /// </summary>
    public bool CanAsk { get; set; }

    public string LoginUrl { get; set; }

    public string AskUrl { get; set; }

    public bool DisplayCaptcha { get; set; }

    public bool HoneypotEnabled { get; set; }

    /// <summary>
    /// Gets or sets the FAQPage structured data of the answered questions (JSON)
    /// </summary>
    public string JsonLd { get; set; }
}

/// <summary>
/// Represents a published question
/// </summary>
public record QuestionItemModel : BaseNopEntityModel
{
    public string Question { get; set; }

    public string Answer { get; set; }

    public string AskedBy { get; set; }

    public string AskedOn { get; set; }
}
