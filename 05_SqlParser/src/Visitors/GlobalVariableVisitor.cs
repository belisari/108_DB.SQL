using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlMigrationValidator.Visitors;

/// <summary>
/// Detects usage of forbidden @@global variables.
/// These globals either expose transaction state, rely on session context,
/// or behave unpredictably when scripts are combined into a single
/// post-deployment script.
/// </summary>
public sealed class GlobalVariableVisitor : MigrationVisitorBase
{
    public GlobalVariableVisitor(string filePath) : base(filePath) { }

    /// <summary>
    /// Forbidden globals with their human-readable reason.
    /// Key: normalised @@NAME (case-insensitive lookup).
    /// Value: explanation shown in the violation message.
    /// Add new entries here to block additional globals.
    /// </summary>
    private static readonly Dictionary<string, string> ForbiddenGlobals =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Transaction state
            ["@@TRANCOUNT"]  = "Reading @@TRANCOUNT implies dependency on transaction nesting level — scripts must not inspect or branch on the managed transaction.",
            ["@@NESTLEVEL"]  = "@@NESTLEVEL exposes execution nesting — migration scripts must not depend on how they are called.",

            // Error handling — use TRY/CATCH instead
            ["@@ERROR"]      = "@@ERROR is legacy error handling — use TRY/CATCH blocks instead.",

            // Row / identity side-effects
            ["@@ROWCOUNT"]   = "@@ROWCOUNT is unreliable when scripts are combined — capture row counts via OUTPUT or explicit variables if needed.",
            ["@@IDENTITY"]   = "@@IDENTITY returns the last identity across the whole session and can return unexpected values in combined scripts — use SCOPE_IDENTITY() or an OUTPUT clause instead.",

            // Environment / server state
            ["@@SERVERNAME"] = "Scripts must not branch on server name — environment-specific logic belongs in pipeline variables, not in migration SQL.",
            ["@@VERSION"]    = "Scripts must not branch on server version — target a minimum supported version and write accordingly.",
            ["@@SPID"]       = "@@SPID (session ID) is environment-specific and must not be used in migration scripts.",
            ["@@DBTS"]       = "@@DBTS (database timestamp) is non-deterministic across environments — use GETUTCDATE() or SYSDATETIMEOFFSET() instead.",
        };

    public override void Visit(GlobalVariableExpression node)
    {
        if (!ForbiddenGlobals.TryGetValue(node.Name, out var reason))
            return;

        AddError("NO_GLOBAL_VARIABLE", $"{node.Name}: {reason}", node);
    }
}
