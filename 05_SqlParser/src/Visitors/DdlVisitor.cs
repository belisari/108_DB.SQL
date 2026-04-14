using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Blocks ALL DDL statements
/// All schema objects belong exclusively in the dacpac.
/// </summary>
public sealed class DdlVisitor(string filePath) : VisitorBase(filePath)
{
    private const string Reason = "Schema changes belong in the dacpac — data migration scripts must only manipulate rows.";

    //Tables
    public override void Visit(CreateTableStatement node) =>
        AddError("NO_DDL", $"CREATE TABLE [{TableName(node.SchemaObjectName)}]: {Reason}", node);
    public override void Visit(AlterTableStatement node) =>
        AddError("NO_DDL", $"ALTER TABLE [{TableName(node.SchemaObjectName)}]: {Reason}", node);
    public override void Visit(DropTableStatement node) =>
        AddError("NO_DDL", $"DROP TABLE: {Reason}", node);
    public override void Visit(TruncateTableStatement node) =>
        AddError("NO_TRUNCATE",
            $"TRUNCATE TABLE [{TableName(node.TableName)}] is forbidden — use DELETE with a WHERE clause. " +
            "TRUNCATE is non-transactional in most contexts and cannot be rolled back.",
            node);

    //Indexes
    public override void Visit(CreateIndexStatement node) =>
        AddError("NO_DDL", $"CREATE INDEX [{node.Name?.Value}]: {Reason}", node);
    public override void Visit(AlterIndexStatement node) =>
        AddError("NO_DDL", $"ALTER INDEX ({node.AlterIndexType}): {Reason}", node);
    public override void Visit(DropIndexStatement node) =>
        AddError("NO_DDL", $"DROP INDEX: {Reason}", node);

    //Views
    public override void Visit(CreateViewStatement node) =>
        AddError("NO_DDL", $"CREATE VIEW [{TableName(node.SchemaObjectName)}]: {Reason}", node);
    public override void Visit(AlterViewStatement node) =>
        AddError("NO_DDL", $"ALTER VIEW: {Reason}", node);
    public override void Visit(DropViewStatement node) =>
        AddError("NO_DDL", $"DROP VIEW: {Reason}", node);

    //Stored procedures
    public override void Visit(CreateProcedureStatement node) =>
        AddError("NO_DDL", $"CREATE PROCEDURE: {Reason}", node);
    public override void Visit(AlterProcedureStatement node) =>
        AddError("NO_DDL", $"ALTER PROCEDURE: {Reason}", node);
    public override void Visit(DropProcedureStatement node) =>
        AddError("NO_DDL", $"DROP PROCEDURE: {Reason}", node);

    //Functions
    public override void Visit(CreateFunctionStatement node) =>
        AddError("NO_DDL", $"CREATE FUNCTION: {Reason}", node);
    public override void Visit(AlterFunctionStatement node) =>
        AddError("NO_DDL", $"ALTER FUNCTION: {Reason}", node);
    public override void Visit(DropFunctionStatement node) =>
        AddError("NO_DDL", $"DROP FUNCTION: {Reason}", node);

    //Triggers
    public override void Visit(CreateTriggerStatement node) =>
        AddError("NO_DDL", $"CREATE TRIGGER: {Reason}", node);
    public override void Visit(AlterTriggerStatement node) =>
        AddError("NO_DDL", $"ALTER TRIGGER: {Reason}", node);
    public override void Visit(DropTriggerStatement node) =>
        AddError("NO_DDL", $"DROP TRIGGER: {Reason}", node);

    //Schemas
    public override void Visit(CreateSchemaStatement node) =>
        AddError("NO_DDL", $"CREATE SCHEMA: {Reason}", node);
    public override void Visit(DropSchemaStatement node) =>
        AddError("NO_DDL", $"DROP SCHEMA: {Reason}", node);

    //Types
    public override void Visit(CreateTypeStatement node) =>
        AddError("NO_DDL", $"CREATE TYPE: {Reason}", node);
    public override void Visit(DropTypeStatement node) =>
        AddError("NO_DDL", $"DROP TYPE: {Reason}", node);

    //Synonyms
    public override void Visit(CreateSynonymStatement node) =>
        AddError("NO_DDL", $"CREATE SYNONYM: {Reason}", node);
    public override void Visit(DropSynonymStatement node) =>
        AddError("NO_DDL", $"DROP SYNONYM: {Reason}", node);

    //Sequences
    public override void Visit(CreateSequenceStatement node) =>
        AddError("NO_DDL", $"CREATE SEQUENCE: {Reason}", node);
    public override void Visit(AlterSequenceStatement node) =>
        AddError("NO_DDL", $"ALTER SEQUENCE: {Reason}", node);
    public override void Visit(DropSequenceStatement node) =>
        AddError("NO_DDL", $"DROP SEQUENCE: {Reason}", node);

    //Databases
    public override void Visit(CreateDatabaseStatement node) =>
        AddError("NO_DDL", $"CREATE DATABASE: {Reason}", node);
    public override void Visit(AlterDatabaseStatement node) =>
        AddError("NO_DDL", $"ALTER DATABASE: {Reason}", node);
    public override void Visit(DropDatabaseStatement node) =>
        AddError("NO_DDL", $"DROP DATABASE: {Reason}", node);
}
