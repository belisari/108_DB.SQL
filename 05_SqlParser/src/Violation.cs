namespace SqlMigrationValidator;

public enum Severity { Error, Warning }

public record Violation(
    string FilePath,
    string RuleName,
    string Message,
    int Line,
    int Column,
    Severity Severity
);
