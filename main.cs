#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property LangVersion=14.0

using System.Diagnostics;

var projectRoot = ResolveProjectRoot();
var errorsDirectory = Path.Combine(projectRoot, "errors");

var scenarios = new[]
{
    (Name: "Compilación", Arguments: new[] { "run", "--nologo", "--file", Path.Combine(errorsDirectory, "compilation-error.cs") }, Expected: "CS0029"),
    (Name: "Tiempo de ejecución", Arguments: new[] { "run", "--nologo", "--file", Path.Combine(errorsDirectory, "runtime-error.cs") }, Expected: "IndexOutOfRangeException"),
    (Name: "Test unitario", Arguments: new[] { "test", "--nologo", Path.Combine(errorsDirectory, "UnitTestError.csproj") }, Expected: "Expected: 5")
};

Console.WriteLine("Ejecutando los tres escenarios en paralelo...\n");

var results = await Task.WhenAll(scenarios.Select(RunScenarioAsync));

foreach (var result in results)
{
    Console.WriteLine($"===== {result.Name} | código de salida: {result.ExitCode} =====");
    Console.WriteLine(result.Output.Trim());
    Console.WriteLine();
}

var allErrorsDetected = results.All(result => result.Output.Contains(result.Expected, StringComparison.OrdinalIgnoreCase));
if (allErrorsDetected)
{
    throw new InvalidOperationException(
        "Los tres escenarios reprodujeron errores: compilación, ejecución y test unitario.");
}

Console.Error.WriteLine("No se detectaron todos los errores esperados.");
return 2;

async Task<(string Name, int ExitCode, string Expected, string Output)> RunScenarioAsync(
    (string Name, string[] Arguments, string Expected) scenario)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    foreach (var argument in scenario.Arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = new Process { StartInfo = startInfo };
    process.Start();

    var standardOutput = process.StandardOutput.ReadToEndAsync();
    var standardError = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    var output = $"{await standardOutput}{await standardError}";
    return (scenario.Name, process.ExitCode, scenario.Expected, output);
}

string ResolveProjectRoot()
{
    var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    AddAncestors(Directory.GetCurrentDirectory(), candidates);
    AddAncestors(AppContext.BaseDirectory, candidates);

    foreach (var candidate in candidates)
    {
        var candidateErrorsDirectory = Path.Combine(candidate, "errors");
        if (File.Exists(Path.Combine(candidateErrorsDirectory, "compilation-error.cs")) &&
            File.Exists(Path.Combine(candidateErrorsDirectory, "runtime-error.cs")) &&
            File.Exists(Path.Combine(candidateErrorsDirectory, "unit-test-error.cs")))
        {
            return candidate;
        }
    }

    throw new DirectoryNotFoundException("No se encontró la raíz del laboratorio .NET.");

    static void AddAncestors(string start, ISet<string> candidates)
    {
        var directory = new DirectoryInfo(start);
        while (directory is not null)
        {
            candidates.Add(directory.FullName);
            directory = directory.Parent;
        }
    }
}
