namespace StyleCopAnalyzers.CLI;

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

public class ConsoleWriter : IDiagnosticWriter
{
    private bool errorOnly;
    public ConsoleWriter(bool errorOnly)
    {
        this.errorOnly = errorOnly;
    }
    public ConsoleWriter() : this(false)
    {
    }

    void IDiagnosticWriter.Write(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var d in diagnostics)
        {
            if (errorOnly && d.Severity != DiagnosticSeverity.Error)
            {
                continue;
            }

            if (d.Severity == DiagnosticSeverity.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            else if (d.Severity == DiagnosticSeverity.Warning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if (d.Severity == DiagnosticSeverity.Info)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            Console.WriteLine($"{d.Id} [{d.Severity}] : {d.Location.GetLineSpan().Path} : {d.Location.GetLineSpan().Span.Start.Line + 1}: {d.GetMessage()}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}