using Crovus.Models;

namespace Crovus.Factory;

internal static class FactoryParts
{
    public static void AddFile(List<DiscordFile> target, DiscordFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        Limit.Count(target.Count + 1, DiscordLimits.MessageFiles, nameof(file));
        Limit.Text(file.Description, DiscordLimits.AttachmentDescription, nameof(file));

        target.Add(file);
    }

    public static void AddFiles(List<DiscordFile> target, IEnumerable<DiscordFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        foreach (var file in files)
            AddFile(target, file);
    }

    public static void AddComponents(ComponentFactory target, IEnumerable<DiscordComponent> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        foreach (var component in components)
            if (component is DiscordActionRow row)
                target.AddRow(row);
            else
                target.AddRow(component);
    }

    public static DiscordEmbed ImageEmbed(DiscordFile file, Action<EmbedFactory>? configure)
    {
        var embed = EmbedFactory.Create().WithImage(file);
        configure?.Invoke(embed);

        return embed.Build();
    }
}
