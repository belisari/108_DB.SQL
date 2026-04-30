using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SqlMigrationValidator.Analyzer;

// ─────────────────────────────────────────────────────────────────────────────
// ANATOMY OF A ROSLYN ANALYZER
//
//  1. Inherit DiagnosticAnalyzer
//  2. Declare one or more DiagnosticDescriptor (the rule metadata)
//  3. Override SupportedDiagnostics to advertise them
//  4. Override Initialize to register callbacks on specific syntax/symbol events
//  5. In each callback, inspect the node and call context.ReportDiagnostic(...)
// ─────────────────────────────────────────────────────────────────────────────

[DiagnosticAnalyzer(LanguageNames.CSharp)]   // <── tells Roslyn this is a C# analyzer
public sealed class RuleNameShouldBeConstantAnalyzer : DiagnosticAnalyzer
{
    // ── Rule metadata ─────────────────────────────────────────────────────────
    //
    // DiagnosticDescriptor is the "blueprint" for a diagnostic.
    // Every field here maps directly to what you see in the Error List / squiggle.
    //
    //  id          → appears as "SMV001" in build output and .editorconfig
    //  title       → short label shown in VS lightbulb tooltip
    //  messageFormat → the actual message (supports {0} placeholders)
    //  category    → logical grouping (Design, Naming, Performance, …)
    //  severity    → Warning / Error / Info / Hidden
    //  isEnabled   → whether on by default
    //  description → longer help text shown in the rule details pane

    public const string DiagnosticId = "SMV001";

    private static readonly DiagnosticDescriptor Rule = new(
        id:                 DiagnosticId,
        title:              "Rule name should be a named constant",
        messageFormat:      "'{0}' is a raw string literal; extract it to a private const so the rule name is reusable and typo-proof",
        category:           "Design",
        defaultSeverity:    DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:        "Passing a raw string literal as the rule name to AddError/AddWarning "
                          + "makes the rule name hard to reuse across overloads and easy to mistype. "
                          + "Declare it as a private const string field instead.");

    // Roslyn asks this property to know which diagnostics this analyzer can emit.
    // Return ALL descriptors you ever pass to ReportDiagnostic.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    // ── Registration ─────────────────────────────────────────────────────────
    //
    // Initialize is called once per compilation.
    // You register callbacks here — Roslyn calls them as it processes each node.
    //
    // Available registration points (most common):
    //   RegisterSyntaxNodeAction     → fires for every node of a given SyntaxKind
    //   RegisterSymbolAction         → fires for every ISymbol of a given kind
    //   RegisterSemanticModelAction  → fires once per syntax tree (heavy, avoid)
    //   RegisterOperationAction      → fires for high-level semantic operations
    //
    // We use RegisterSyntaxNodeAction because we only need syntax — no type info.

    public override void Initialize(AnalysisContext context)
    {
        // Performance best-practices required by EnforceExtendedAnalyzerRules:
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None); // skip generated files
        context.EnableConcurrentExecution();                                     // allow parallel analysis

        // Ask Roslyn to call AnalyzeInvocation every time it encounters a
        // method-call expression anywhere in the compilation.
        context.RegisterSyntaxNodeAction(
            AnalyzeInvocation,
            SyntaxKind.InvocationExpression);
    }

    // ── The actual analysis ───────────────────────────────────────────────────
    //
    // SyntaxNodeAnalysisContext gives us:
    //   .Node            → the SyntaxNode that triggered this callback
    //   .SemanticModel   → type/symbol resolution (we don't need it here)
    //   .ReportDiagnostic(d) → emit a diagnostic to the compiler pipeline
    //   .CancellationToken   → honour cancellation (long builds, IDE)

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        // Cast is safe — we registered only for InvocationExpression nodes.
        var invocation = (InvocationExpressionSyntax)context.Node;

        // ── Step 1: Is this a call to AddError or AddWarning? ─────────────────
        //
        // The "expression" part of  foo.AddError(...)  is a MemberAccessExpression.
        // We grab the method name from its .Name token.
        //
        //   InvocationExpression
        //   ├─ Expression  →  MemberAccessExpression  (foo.AddError)
        //   │               ├─ Expression  →  foo
        //   │               └─ Name        →  AddError   ◄── we read this
        //   └─ ArgumentList
        //       ├─ Argument  → "NO_DDL"               ◄── and check this

        var methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax m => m.Name.Identifier.Text,
            IdentifierNameSyntax i         => i.Identifier.Text,
            _                              => null
        };

        if (methodName is not ("AddError" or "AddWarning"))
            return;

        // ── Step 2: Does the first argument exist? ────────────────────────────

        var args = invocation.ArgumentList.Arguments;
        if (args.Count == 0)
            return;

        var firstArg = args[0].Expression;

        // ── Step 3: Is that first argument a raw string literal? ──────────────
        //
        // A string literal in the syntax tree is a LiteralExpressionSyntax
        // with Kind == StringLiteralExpression.
        //
        // Contrast with a *constant reference* like  RuleNames.NoDdl  which
        // would be an IdentifierNameSyntax or MemberAccessExpressionSyntax.

        if (firstArg is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal)
            return;

        // ── Step 4: Report the diagnostic ────────────────────────────────────
        //
        // Diagnostic.Create binds the descriptor to:
        //   - a Location  (the red squiggle in the editor)
        //   - message arguments that fill the {0} placeholder

        var diagnostic = Diagnostic.Create(
            descriptor: Rule,
            location:   literal.GetLocation(),
            messageArgs: literal.Token.ValueText);   // fills {0} with the actual string value

        context.ReportDiagnostic(diagnostic);
    }
}
