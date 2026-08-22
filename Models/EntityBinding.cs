using Crovus.Client;

namespace Crovus.Models;

public readonly struct EntityBinding : IEquatable<EntityBinding>
{
    private readonly ICrovusContext? _context;

    private EntityBinding(ICrovusContext context) => _context = context;

    public static EntityBinding To(ICrovusContext context) => new(context);

    public ICrovusContext? Context => _context;

    public bool IsBound => _context is not null;

    public bool Equals(EntityBinding other) => true;

    public override bool Equals(object? obj) => obj is EntityBinding;

    public override int GetHashCode() => 0;

    public override string ToString() => string.Empty;

    public static bool operator ==(EntityBinding left, EntityBinding right) => true;

    public static bool operator !=(EntityBinding left, EntityBinding right) => false;
}
