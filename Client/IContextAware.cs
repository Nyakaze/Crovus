namespace Crovus.Client;

public interface IContextAware
{
    ICrovusContext? Context { get; set; }
}
