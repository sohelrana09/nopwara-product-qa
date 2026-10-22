using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.NopwaraQA;

/// <summary>
/// Represents the plugin settings
/// </summary>
public class NopwaraQASettings : ISettings
{
    /// <summary>
    /// Gets or sets comma-separated theme system names the Q&A section is rendered for (its markup is styled by the theme)
    /// </summary>
    public string AllowedThemes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether guests can ask questions
    /// </summary>
    public bool AllowGuests { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether questions must be approved before they are published
    /// </summary>
    public bool QuestionsMustBeApproved { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show approved questions that have no answer yet
    /// </summary>
    public bool ShowUnanswered { get; set; }

    /// <summary>
    /// Gets or sets the number of questions shown before "Show more"
    /// </summary>
    public int VisibleQuestions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to email the store owner about new questions
    /// </summary>
    public bool NotifyStoreOwner { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to email the customer when the question is answered
    /// </summary>
    public bool NotifyCustomer { get; set; }
}
