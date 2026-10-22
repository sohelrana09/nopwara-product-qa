using Nop.Core;

namespace Nop.Plugin.Misc.NopwaraQA.Domain;

/// <summary>
/// Represents a customer question about a product (and the store answer)
/// </summary>
public class ProductQuestion : BaseEntity
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the store identifier
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the customer who asked
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the name shown with the question
    /// </summary>
    public string CustomerName { get; set; }

    /// <summary>
    /// Gets or sets the question
    /// </summary>
    public string QuestionText { get; set; }

    /// <summary>
    /// Gets or sets the answer (empty until answered)
    /// </summary>
    public string AnswerText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the question is published
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the customer has been notified of the answer
    /// </summary>
    public bool CustomerNotifiedOfAnswer { get; set; }

    /// <summary>
    /// Gets or sets the date and time the question was asked
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time the question was answered
    /// </summary>
    public DateTime? AnsweredOnUtc { get; set; }
}
