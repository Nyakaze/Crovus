using System.Globalization;
using Crovus.Factory;
using Crovus.Logs;
using Crovus.Models;
using Crovus.Rest;

namespace Crovus.Services;

public sealed class CommandService : DiscordService
{
    public CommandService(IDiscordRest rest, ILogger? logger = null, ITelemetry? telemetry = null)
        : base(rest, "Command", logger, telemetry)
    {
    }

    public CommandService(IDiscordRest rest, DiagnosticsHub diagnostics)
        : this(rest, diagnostics, diagnostics)
    {
    }

    public Task<IReadOnlyList<DiscordApplicationCommand>> GetAllAsync(Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(GetAllAsync), Scope(applicationId, guildId),
            () => Rest.GetApplicationCommandsAsync(applicationId, guildId, cancellationToken),
            commands => $"Loaded {commands.Count} commands of {Scope(applicationId, guildId)}", LogLevel.Debug);

    public Task<DiscordApplicationCommand> RegisterAsync(Snowflake applicationId, ApplicationCommandRequest request,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(RegisterAsync), Scope(applicationId, guildId),
            () => Rest.CreateApplicationCommandAsync(applicationId, request, guildId, cancellationToken),
            command => $"Registered command {command.Name} ({command.Id}) for {Scope(applicationId, guildId)}");
    }

    public Task<DiscordApplicationCommand> RegisterAsync(Snowflake applicationId, SlashCommandFactory command,
        Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return RegisterAsync(applicationId, command.Build(), guildId, cancellationToken);
    }

    public Task<DiscordApplicationCommand> RegisterAsync(Snowflake applicationId, string name, string description,
        Action<SlashCommandFactory>? configure = null, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        var factory = SlashCommandFactory.Slash(name, description);
        configure?.Invoke(factory);

        return RegisterAsync(applicationId, factory.Build(), guildId, cancellationToken);
    }

    public Task<DiscordApplicationCommand> UpdateAsync(Snowflake applicationId, Snowflake commandId,
        ApplicationCommandRequest request, Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return TrackAsync(nameof(UpdateAsync), $"command {commandId} of {Scope(applicationId, guildId)}",
            () => Rest.EditApplicationCommandAsync(applicationId, commandId, request, guildId, cancellationToken),
            command => $"Updated command {command.Name} ({command.Id}) of {Scope(applicationId, guildId)}");
    }

    public Task<DiscordApplicationCommand> UpdateAsync(Snowflake applicationId, Snowflake commandId,
        SlashCommandFactory command, Snowflake? guildId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return UpdateAsync(applicationId, commandId, command.Build(), guildId, cancellationToken);
    }

    public Task DeleteAsync(Snowflake applicationId, Snowflake commandId, Snowflake? guildId = null,
        CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(DeleteAsync), $"command {commandId} of {Scope(applicationId, guildId)}",
            () => Rest.DeleteApplicationCommandAsync(applicationId, commandId, guildId, cancellationToken),
            $"Deleted command {commandId} from {Scope(applicationId, guildId)}");

    public Task<IReadOnlyList<DiscordApplicationCommand>> DeployAsync(Snowflake applicationId,
        IEnumerable<ApplicationCommandRequest> requests, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var payload = requests.ToArray();

        CommandNames.RequireUnique(payload.Select(request => $"{request.Type}:{request.Name}"), "command");

        return TrackAsync(nameof(DeployAsync), Scope(applicationId, guildId),
            () => Rest.SetApplicationCommandsAsync(applicationId, payload, guildId, cancellationToken),
            commands => $"Deployed {commands.Count} commands to {Scope(applicationId, guildId)}");
    }

    public Task<IReadOnlyList<DiscordApplicationCommand>> DeployAsync(Snowflake applicationId,
        IEnumerable<SlashCommandFactory> commands, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commands);

        return DeployAsync(applicationId, commands.Select(command => command.Build()).ToArray(), guildId,
            cancellationToken);
    }

    public Task<IReadOnlyList<DiscordApplicationCommand>> ClearAsync(Snowflake applicationId,
        Snowflake? guildId = null, CancellationToken cancellationToken = default) =>
        TrackAsync(nameof(ClearAsync), Scope(applicationId, guildId),
            () => Rest.SetApplicationCommandsAsync(applicationId, [], guildId, cancellationToken),
            commands => $"Cleared every command of {Scope(applicationId, guildId)}");

    public async Task<CommandSyncResult> SynchronizeAsync(Snowflake applicationId,
        IEnumerable<ApplicationCommandRequest> requests, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var desired = requests.ToArray();

        CommandNames.RequireUnique(desired.Select(request => $"{request.Type}:{request.Name}"), "command");

        var result = await TrackAsync(nameof(SynchronizeAsync), Scope(applicationId, guildId),
            async () =>
            {
                var existing = await Rest.GetApplicationCommandsAsync(applicationId, guildId, cancellationToken);
                var live = existing.ToDictionary(command => (command.Type, command.Name));

                var added = 0;
                var changed = 0;
                var unchanged = 0;

                foreach (var request in desired)
                {
                    if (!live.TryGetValue((request.Type, request.Name), out var current))
                        added++;
                    else if (CommandComparer.AreEquivalent(current, request))
                        unchanged++;
                    else
                        changed++;
                }

                var removed = existing.Count(command =>
                    !desired.Any(request => request.Type == command.Type && request.Name == command.Name));

                if (added == 0 && changed == 0 && removed == 0)
                    return new CommandSyncResult(existing, 0, 0, 0, unchanged);

                var deployed = await Rest.SetApplicationCommandsAsync(applicationId, desired, guildId,
                    cancellationToken);

                return new CommandSyncResult(deployed, added, changed, removed, unchanged);
            },
            sync => sync.HasChanges
                ? $"Synchronized {Scope(applicationId, guildId)}: {sync.Added} added, {sync.Changed} changed, " +
                  $"{sync.Removed} removed, {sync.Unchanged} unchanged"
                : $"{Scope(applicationId, guildId)} already matches {sync.Unchanged} commands");

        Emit(new CommandsSynchronized(applicationId, guildId?.Value, result.Added, result.Changed, result.Removed,
            result.Unchanged));

        return result;
    }

    public Task<CommandSyncResult> SynchronizeAsync(Snowflake applicationId,
        IEnumerable<SlashCommandFactory> commands, Snowflake? guildId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commands);

        return SynchronizeAsync(applicationId, commands.Select(command => command.Build()).ToArray(), guildId,
            cancellationToken);
    }

    private static string Scope(Snowflake applicationId, Snowflake? guildId) =>
        guildId is { } guild ? $"application {applicationId} in guild {guild}" : $"application {applicationId}";
}

internal static class CommandComparer
{
    public static bool AreEquivalent(DiscordApplicationCommand existing, ApplicationCommandRequest desired)
    {
        if (existing.Type != desired.Type || !string.Equals(existing.Name, desired.Name, StringComparison.Ordinal))
            return false;

        if (!string.Equals(existing.Description, desired.Description ?? string.Empty, StringComparison.Ordinal))
            return false;

        if (existing.Nsfw != (desired.Nsfw ?? false))
            return false;

        if (existing.DefaultMemberPermissions != desired.DefaultMemberPermissions)
            return false;

        if (desired.Contexts is { } contexts && !SameSet(existing.Contexts, contexts))
            return false;

        if (desired.IntegrationTypes is { } integrations && !SameSet(existing.IntegrationTypes, integrations))
            return false;

        return SameOptions(existing.Options, desired.Options);
    }

    private static bool SameOptions(IReadOnlyList<DiscordApplicationCommandOption>? left,
        IReadOnlyList<DiscordApplicationCommandOption>? right)
    {
        var first = left ?? [];
        var second = right ?? [];

        if (first.Count != second.Count)
            return false;

        for (var index = 0; index < first.Count; index++)
        {
            if (!SameOption(first[index], second[index]))
                return false;
        }

        return true;
    }

    private static bool SameOption(DiscordApplicationCommandOption left, DiscordApplicationCommandOption right)
    {
        if (left.Type != right.Type || !string.Equals(left.Name, right.Name, StringComparison.Ordinal))
            return false;

        if (!string.Equals(left.Description, right.Description, StringComparison.Ordinal))
            return false;

        if (left.Required != right.Required)
            return false;

        if ((left.Autocomplete ?? false) != (right.Autocomplete ?? false))
            return false;

        if (left.MinValue != right.MinValue || left.MaxValue != right.MaxValue)
            return false;

        if (left.MinLength != right.MinLength || left.MaxLength != right.MaxLength)
            return false;

        if (!SameSet(left.ChannelTypes, right.ChannelTypes))
            return false;

        return SameChoices(left.Choices, right.Choices) && SameOptions(left.Options, right.Options);
    }

    private static bool SameChoices(IReadOnlyList<DiscordApplicationCommandChoice>? left,
        IReadOnlyList<DiscordApplicationCommandChoice>? right)
    {
        var first = left ?? [];
        var second = right ?? [];

        if (first.Count != second.Count)
            return false;

        for (var index = 0; index < first.Count; index++)
        {
            if (!string.Equals(first[index].Name, second[index].Name, StringComparison.Ordinal))
                return false;

            if (!SameValue(first[index].Value, second[index].Value))
                return false;
        }

        return true;
    }

    private static bool SameValue(object left, object right)
    {
        if (left is string || right is string)
            return left is string leftText && right is string rightText &&
                   string.Equals(leftText, rightText, StringComparison.Ordinal);

        return Convert.ToDouble(left, CultureInfo.InvariantCulture)
            .Equals(Convert.ToDouble(right, CultureInfo.InvariantCulture));
    }

    private static bool SameSet<T>(IReadOnlyList<T>? left, IReadOnlyList<T>? right) where T : struct
    {
        var first = left ?? [];
        var second = right ?? [];

        return first.Count == second.Count && !first.Except(second).Any();
    }
}
