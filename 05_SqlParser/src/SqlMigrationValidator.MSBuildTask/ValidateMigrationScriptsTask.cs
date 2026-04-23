using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SqlMigrationValidator.MSBuildTask;

/// <summary>
/// MSBuild task that validates SQL migration scripts using the same rules as the CLI.
///
/// Usage in a .sqlproj (or any MSBuild project):
///
///   &lt;UsingTask
///     TaskName="SqlMigrationValidator.MSBuildTask.ValidateMigrationScriptsTask"
///     AssemblyFile="$(MSBuildThisFileDirectory)SqlMigrationValidator.MSBuildTask.dll" /&gt;
///
///   &lt;Target Name="ValidateMigrations" BeforeTargets="Build"&gt;
///     &lt;ValidateMigrationScriptsTask
///       MigrationsDirectory="$(MSBuildProjectDirectory)\Migrations"
///       TreatWarningsAsErrors="false" /&gt;
///   &lt;/Target&gt;
/// </summary>
public sealed class ValidateMigrationScriptsTask : Task
{
    [Required]
    public string MigrationsDirectory { get; set; } = string.Empty;

    public string SearchPattern { get; set; } = "*.sql";

    public bool TreatWarningsAsErrors { get; set; } = false;

    public override bool Execute()
    {
        var validator = new MigrationScriptValidator();
        var violations = validator.ValidateDirectory(MigrationsDirectory, SearchPattern);

        foreach (var v in violations)
        {
            bool isError = v.Severity == Severity.Error
                           || (TreatWarningsAsErrors && v.Severity == Severity.Warning);

            if (isError)
                Log.LogError(
                    subcategory: null,
                    errorCode: v.RuleName,
                    helpKeyword: null,
                    file: v.FilePath,
                    lineNumber: v.Line,
                    columnNumber: v.Column,
                    endLineNumber: v.Line,
                    endColumnNumber: v.Column,
                    message: v.Message);
            else
                Log.LogWarning(
                    subcategory: null,
                    warningCode: v.RuleName,
                    helpKeyword: null,
                    file: v.FilePath,
                    lineNumber: v.Line,
                    columnNumber: v.Column,
                    endLineNumber: v.Line,
                    endColumnNumber: v.Column,
                    message: v.Message);
        }

        return !Log.HasLoggedErrors;
    }
}
