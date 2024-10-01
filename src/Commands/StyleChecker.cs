namespace StyleCopAnalyzers.CLI;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CommandLine;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

[Verb("check", HelpText = "Check C# Coding Style")]
public class StyleChecker
{
    [Option('r', "ruleset", Required = false, HelpText = "Ruleset file path.")]
    public string RuleSetFilePath { get; set; } = string.Empty;
    [Option('j', "json", Required = false, HelpText = "stylecop.json file path")]
    public string StyleCopJsonFilePath { get; set; } = string.Empty;
    [Option('f', "format", Required = false, Default = "text", HelpText = "output format\n    text raw text\n    xml  legacy stylecop xml format")]
    public string OutputFormat { get; set; } = string.Empty;

    [Option('e', "errors-only", Required = false, Default = false, HelpText = "Print only error in output stream.")]
    public bool ErrorsOnly { get; set; } = false;

    [Value(0, MetaName = "sln/csproj file path, directory path or file path")]
    public IEnumerable<string> Targets { get; set; }

    private ILogger logger = new SilentLogger();

    public StyleChecker()
    {
        Targets = Array.Empty<string>();
    }

    public void SetLogger(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task<int> Check(CancellationToken cancellationToken)
    {
        RuleSetFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(RuleSetFilePath, "./stylecop.ruleset");
        StyleCopJsonFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(StyleCopJsonFilePath, "./stylecop.json");

        if (!Targets.Any())
        {
            return 1;
        }

        this.logger.LogDebug("Arguments ============================");
        this.logger.LogDebug($"ruleset : {RuleSetFilePath}");
        this.logger.LogDebug($"stylecop.json : {RuleSetFilePath}");
        this.logger.LogDebug($"format : {OutputFormat}");
        this.logger.LogDebug($"error-only : {ErrorsOnly}");
        this.logger.LogDebug($"check : \n{string.Join("\n", Targets)}");
        this.logger.LogDebug("======================================");

        var projects = ImmutableArray.CreateBuilder<Project>();
        foreach (var target in Targets)
        {
            var targetFileOrDirectory = CommandHelper.GetAbsolutePath(target);

            var inputKind = CommandHelper.GetInputKindFromFileOrDirectory(targetFileOrDirectory);
            if (!inputKind.HasValue) { return 1; }

            var readableProjects = inputKind.Value.ToReader().ReadAllSourceCodeFiles(targetFileOrDirectory, StyleCopJsonFilePath);
            if (readableProjects.Length == 0) { return 1; }

            projects.AddRange(readableProjects);
        }

        var outputKind = OutputKindHelper.ToOutputKind(OutputFormat);
        if (outputKind == OutputKind.Undefined)
        {
            Console.Error.WriteLine($"output format is undefined. -f {OutputFormat}");
            return 1;
        }

        var analyzerLoader = new AnalyzerLoader(RuleSetFilePath);
        var analyzers = analyzerLoader.GetAnalyzers();
        var diagnostics = await CommandHelper.GetAnalyzerDiagnosticsAsync(
            projects.ToImmutable(),
            analyzers,
            analyzerLoader.RuleSets,
            cancellationToken).ConfigureAwait(false);

        // calculate result
        int result = 0;
        long info = 0;
        long warning = 0;
        long error = 0;
        foreach (Diagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                result = 1;
                error++;
            }
            else if (diagnostic.Severity == DiagnosticSeverity.Warning)
            {
                warning++;
            }
            else if (diagnostic.Severity == DiagnosticSeverity.Info)
            {
                info++;
            }
        }


        var writer = outputKind.ToWriter(ErrorsOnly);
        writer.Write(diagnostics);

        this.logger.LogDebug("Errors: " + error + ", Warnings: " + warning + ", Infos: " + info);

        return result;
    }
}
