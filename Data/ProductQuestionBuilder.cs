using FluentMigrator.Builders.Create.Table;
using Nop.Core.Domain.Catalog;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.NopwaraQA.Domain;

namespace Nop.Plugin.Misc.NopwaraQA.Data;

/// <summary>
/// Represents the product question entity builder
/// </summary>
public class ProductQuestionBuilder : NopEntityBuilder<ProductQuestion>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ProductQuestion.ProductId)).AsInt32().ForeignKey<Product>()
            //no foreign key to Customer: core hard-deletes old guest customers, which would cascade to their (answered) questions
            .WithColumn(nameof(ProductQuestion.CustomerId)).AsInt32()
            .WithColumn(nameof(ProductQuestion.CustomerName)).AsString(200).Nullable()
            .WithColumn(nameof(ProductQuestion.QuestionText)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(ProductQuestion.AnswerText)).AsString(int.MaxValue).Nullable();
    }
}
