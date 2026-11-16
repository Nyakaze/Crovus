using System.IO.Pipes;

namespace Crovus.RichPresence;

internal sealed class RichPresenceConnection : IAsyncDisposable
{
    private const string PipeDirectory = @"\\.\pipe\";

    private readonly NamedPipeClientStream _pipe;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    private RichPresenceConnection(NamedPipeClientStream pipe, string pipeName)
    {
        _pipe = pipe;
        PipeName = pipeName;
    }

    public string PipeName { get; }

    public static async Task<RichPresenceConnection> OpenAsync(RichPresenceClientOptions options,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
            throw new RichPresenceException(new RichPresenceFailure(RichPresenceFailureKind.Unsupported,
                "Rich Presence over IPC is currently only supported on Windows."));

        var existing = ListPipes();
        Exception? denied = null;

        foreach (var pipeName in options.PipeNames)
        {
            if (existing is not null && !existing.Contains(pipeName))
                continue;

            var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                await pipe.ConnectAsync(options.ConnectTimeout, cancellationToken);

                return new RichPresenceConnection(pipe, pipeName);
            }
            catch (UnauthorizedAccessException failure)
            {
                denied ??= failure;
                await pipe.DisposeAsync();
            }
            catch (Exception failure) when (failure is TimeoutException or IOException)
            {
                await pipe.DisposeAsync();
            }
            catch
            {
                await pipe.DisposeAsync();
                throw;
            }
        }

        if (denied is not null)
            throw new RichPresenceException(new RichPresenceFailure(RichPresenceFailureKind.AccessDenied,
                "Discord's IPC pipe exists but access was denied. Discord and this app are most likely running " +
                "with different privileges, for example Discord as administrator and this app not.",
                Exception: denied));

        throw new RichPresenceException(new RichPresenceFailure(RichPresenceFailureKind.DiscordNotRunning,
            "No Discord IPC pipe could be opened. Is the Discord desktop app running?"));
    }

    public async ValueTask SendAsync(RichPresenceOpcode opcode, ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);

        try
        {
            await RichPresenceFraming.WriteAsync(_pipe, opcode, payload, cancellationToken);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public ValueTask<RichPresenceFrame> ReceiveAsync(CancellationToken cancellationToken) =>
        RichPresenceFraming.ReadAsync(_pipe, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _pipe.DisposeAsync();
        _writeGate.Dispose();
    }

    private static HashSet<string>? ListPipes()
    {
        try
        {
            return Directory.EnumerateFiles(PipeDirectory)
                .Select(Path.GetFileName)
                .OfType<string>()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
