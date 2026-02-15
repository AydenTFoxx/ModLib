using System.Collections.Generic;

namespace ModLib.Objects;

/// <summary>
///     A variant of <see cref="UpdatableAndDeletable"/> which is updated independently of a room.
/// </summary>
public class GlobalUpdatableAndDeletable
{
    internal static readonly List<GlobalUpdatableAndDeletable> Instances = [];

    /// <inheritdoc cref="UpdatableAndDeletable.evenUpdate"/>
    public bool evenUpdate;

    /// <inheritdoc cref="UpdatableAndDeletable.slatedForDeletetion"/>
    public bool slatedForDeletetion;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GlobalUpdatableAndDeletable"/> class.
    /// </summary>
    public GlobalUpdatableAndDeletable()
    {
        Instances.Add(this);
    }

    /// <inheritdoc cref="UpdatableAndDeletable.Update(bool)"/>
    public virtual void Update(bool eu) => evenUpdate = eu;

    /// <inheritdoc cref="UpdatableAndDeletable.PausedUpdate()"/>
    public virtual void PausedUpdate() { }

    /// <inheritdoc cref="UpdatableAndDeletable.Destroy()"/>
    public virtual void Destroy()
    {
        slatedForDeletetion = true;

        Instances.Remove(this);
    }
}