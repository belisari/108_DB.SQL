using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects DDL statements for views, stored procedures, and functions.
/// All schema object definitions belong in the dacpac, not in data migration scripts.
/// </summary>
public sealed class SchemaObjectDdlVisitor : MigrationVisitorBase
{
    public SchemaObjectDdlVisitor(string filePath) : base(filePath) { }

    public override void Visit(CreateViewStatement node) =>
        AddError("NO_CREATE_VIEW",
            $"CREATE VIEW [{TableName(node.SchemaObjectName)}] is forbidden in data migration scripts.",
            node);

    public override void Visit(AlterViewStatement node) =>
        AddError("NO_ALTER_VIEW",
            "ALTER VIEW is forbidden in data migration scripts.",
            node);

    public override void Visit(CreateProcedureStatement node) =>
        AddError("NO_CREATE_PROC",
            "CREATE PROCEDURE is forbidden in data migration scripts.",
            node);

    public override void Visit(AlterProcedureStatement node) =>
        AddError("NO_ALTER_PROC",
            "ALTER PROCEDURE is forbidden in data migration scripts.",
            node);

    public override void Visit(CreateFunctionStatement node) =>
        AddError("NO_CREATE_FUNCTION",
            "CREATE FUNCTION is forbidden in data migration scripts.",
            node);

    public override void Visit(AlterFunctionStatement node) =>
        AddError("NO_ALTER_FUNCTION",
            "ALTER FUNCTION is forbidden in data migration scripts.",
            node);
}
