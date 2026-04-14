using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

public sealed class GlobalVariableVisitor : VisitorBase
{
    public GlobalVariableVisitor(string filePath) : base(filePath) { }

    private static readonly Dictionary<string, string> ForbiddenGlobals =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["@@TRANCOUNT"]  = "Reading @@TRANCOUNT implies dependency on transaction nesting level — scripts must not inspect or branch on the managed transaction.",
            ["@@NESTLEVEL"]  = "@@NESTLEVEL exposes execution nesting — migration scripts must not depend on how they are called.",
            ["@@ERROR"]      = "@@ERROR is legacy error handling — use TRY/CATCH blocks instead.",
            ["@@ROWCOUNT"]   = "@@ROWCOUNT is unreliable when scripts are combined — capture row counts via OUTPUT or explicit variables if needed.",
            ["@@IDENTITY"]   = "@@IDENTITY returns the last identity across the whole session and can return unexpected values in combined scripts — use SCOPE_IDENTITY() or an OUTPUT clause instead.",
            ["@@SERVERNAME"] = "Scripts must not branch on server name — environment-specific logic belongs in pipeline variables, not in migration SQL.",
            ["@@VERSION"]    = "Scripts must not branch on server version — target a minimum supported version and write accordingly.",
            ["@@SPID"]       = "@@SPID (session ID) is environment-specific and must not be used in migration scripts.",
            ["@@DBTS"]       = "@@DBTS (database timestamp) is non-deterministic across environments — use GETUTCDATE() or SYSDATETIMEOFFSET() instead.",
        };

    private static readonly HashSet<string> AllowedGlobals = new(StringComparer.OrdinalIgnoreCase)
    {
        "@@ROWCOUNT",
        "@@FETCH_STATUS",
        "@@CURSOR_ROWS",
        "@@LANGUAGE",
    };

    public override void Visit(GlobalVariableExpression node)
    {
        if (ForbiddenGlobals.TryGetValue(node.Name, out var reasonForbidden))
        {
            AddError("NO_GLOBAL_VARIABLE", $"{node.Name}: {reasonForbidden}", node);
            return;
        }

        if (AllowedGlobals.Contains(node.Name))
        {
            return;
        };
        AddWarning("UNKNOWN_GLOBAL_VARIABLE", $"{node.Name}", node);
    }
}
