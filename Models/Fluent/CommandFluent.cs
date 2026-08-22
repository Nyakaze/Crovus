using Crovus.Factory;

namespace Crovus.Models;

public static class CommandFluent
{
    public static Task<DiscordApplicationCommand> UpdateAsync(this DiscordApplicationCommand command,
        ApplicationCommandRequest request, CancellationToken cancellationToken = default) =>
        command.Services().Commands.UpdateAsync(command.ApplicationId, command.Id, request, command.GuildId,
            cancellationToken);

    public static Task<DiscordApplicationCommand> UpdateAsync(this DiscordApplicationCommand command,
        SlashCommandFactory factory, CancellationToken cancellationToken = default) =>
        command.Services().Commands.UpdateAsync(command.ApplicationId, command.Id, factory, command.GuildId,
            cancellationToken);

    public static Task DeleteAsync(this DiscordApplicationCommand command,
        CancellationToken cancellationToken = default) =>
        command.Services().Commands.DeleteAsync(command.ApplicationId, command.Id, command.GuildId,
            cancellationToken);

    public static Task<DiscordCommandPermissions> GetPermissionsAsync(this DiscordApplicationCommand command,
        Snowflake guildId, CancellationToken cancellationToken = default) =>
        command.Rest().GetCommandPermissionsAsync(command.ApplicationId, guildId, command.Id, cancellationToken);

    public static async Task<DiscordApplicationCommand?> RefreshAsync(this DiscordApplicationCommand command,
        CancellationToken cancellationToken = default)
    {
        var commands = await command.Services().Commands
            .GetAllAsync(command.ApplicationId, command.GuildId, cancellationToken);

        return commands.FirstOrDefault(candidate => candidate.Id == command.Id);
    }
}
