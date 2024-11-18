namespace StyleCopAnalyzers.CLI;

using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress +=
            (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };

        var logger = new SimpleConsoleLogger() as ILogger;

        int result = 0;
        try
        {

            result = await Parser.Default.ParseArguments<StyleChecker, StyleFixer>(args)
                .MapResult(
                    async (StyleChecker style) =>
                    {
                        style.SetLogger(logger);
                        return await style.Check(cancellationTokenSource.Token).ConfigureAwait(false);
                    },
                    async (StyleFixer style) =>
                    {
                        style.SetLogger(logger);
                        return await style.FixCode(cancellationTokenSource.Token).ConfigureAwait(false);
                    },
                    async _ => await Task.FromResult(1))
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception.Message);
            logger.LogError(exception.StackTrace!);
            result = 1;
        }

        cancellationTokenSource.Dispose();

        return result;
    }
}