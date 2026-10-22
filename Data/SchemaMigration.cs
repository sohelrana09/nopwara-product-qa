using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.NopwaraQA.Domain;

namespace Nop.Plugin.Misc.NopwaraQA.Data;

[NopMigration("2026-10-22 12:00:00", "Nop.Plugin.Misc.NopwaraQA schema", MigrationProcessType.Installation)]
public class SchemaMigration : Migration
{
    /// <summary>
    /// Collect the UP migration expressions
    /// </summary>
    public override void Up()
    {
        this.CreateTableIfNotExists<ProductQuestion>();
    }

    /// <summary>
    /// Collects the DOWN migration expressions
    /// </summary>
    public override void Down()
    {
        this.DeleteTableIfExists<ProductQuestion>();
    }
}
