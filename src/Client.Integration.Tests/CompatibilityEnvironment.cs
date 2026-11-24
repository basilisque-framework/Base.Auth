using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;

namespace Basilisque.Base.Auth.Client.Integration.Tests;

internal sealed class CompatibilityEnvironment : IAsyncDisposable
{
    private readonly List<ChildProcess> _children = [];
    internal static string CurrentFramework { get; } =
        "net" + new FrameworkName(AppContext.TargetFrameworkName!).Version.ToString(2);
    internal string Root { get; }
    internal string DirectoryPath { get; }
#if DEBUG
    private const string Configuration = "debug";
#else
    private const string Configuration = "release";
#endif

    internal CompatibilityEnvironment()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Basilisque.Base.Auth.slnx")))
            directory = directory.Parent;
        Root = directory?.FullName ?? throw new InvalidOperationException("Cannot locate the repository root.");
        DirectoryPath = Path.Combine(Root, "artifacts", "client-compatibility", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }

    internal ChildProcess Start(string executable, IEnumerable<string> arguments, Dictionary<string, string>? environment = null)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = Root,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        if (environment is not null)
        {
            foreach (var pair in environment)
                start.Environment[pair.Key] = pair.Value;
        }

        var child = new ChildProcess(Process.Start(start) ?? throw new InvalidOperationException("Cannot start compatibility process."));
        _children.Add(child);
        return child;
    }

    internal async Task<ProcessResult> RunDotnetAsync(params string[] arguments) =>
        await Start("dotnet", arguments).CompleteAsync();

    internal string ProgramPath(string name, string framework)
    {
        var pivot = name == "MachineService" ? Configuration : Configuration + "_" + framework;
        var extension = framework == "net48" ? ".exe" : ".dll";
        var path = Path.Combine(Root, "artifacts", "bin", "Base.Auth." + name, pivot, "Basilisque.Base.Auth." + name + extension);
        if (!File.Exists(path))
            throw new FileNotFoundException("Build the integration test project before running compatibility tests.", path);
        return path;
    }

    internal ChildProcess StartProgram(string name, string framework, IEnumerable<string> arguments, Dictionary<string, string>? environment = null)
    {
        var path = ProgramPath(name, framework);
        return framework == "net48" ? Start(path, arguments, environment) : Start("dotnet", new[] { path }.Concat(arguments), environment);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var child in _children.AsEnumerable().Reverse())
            await child.DisposeAsync();
        // Only the unique directory created and owned by this fixture is removed.
        if (Directory.Exists(DirectoryPath))
            Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal sealed record ProcessResult
{
    /// <summary>Gets the process exit code.</summary>
    public int ExitCode { get; init; }

    /// <summary>Gets the captured standard output.</summary>
    public string Output { get; init; }

    /// <summary>Gets the captured standard error.</summary>
    public string Error { get; init; }

    /// <summary>Creates a result from a completed compatibility process.</summary>
    public ProcessResult(int exitCode, string output, string error)
    {
        ExitCode = exitCode;
        Output = output;
        Error = error;
    }

    /// <summary>Deconstructs the process exit code and captured output streams.</summary>
    public void Deconstruct(out int exitCode, out string output, out string error)
    {
        exitCode = ExitCode;
        output = Output;
        error = Error;
    }

    internal void EnsureSuccess()
    {
        if (ExitCode != 0)
            throw new InvalidOperationException($"Compatibility setup failed ({ExitCode}). {Output} {Error}");
    }
}

internal sealed class ChildProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Task<string> _output;
    private readonly Task<string> _error;

    internal ChildProcess(Process process)
    {
        _process = process;
        _output = process.StandardOutput.ReadToEndAsync();
        _error = process.StandardError.ReadToEndAsync();
    }

    internal bool HasExited => _process.HasExited;

    internal async Task<ProcessResult> CompleteAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await _process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!_process.HasExited)
                _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
            throw new TimeoutException("The compatibility process did not finish within 60 seconds.");
        }
        return new ProcessResult(_process.ExitCode, await _output, await _error);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
            _process.Kill(entireProcessTree: true);
        await _process.WaitForExitAsync();
        await Task.WhenAll(_output, _error);
        _process.Dispose();
    }
}