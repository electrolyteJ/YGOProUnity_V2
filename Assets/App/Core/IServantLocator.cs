namespace App.Core
{
    /// <summary>
    /// Narrow capability interfaces for the remaining Program-backed compatibility seam.
    /// Consumers should depend only on the specific legacy servant access they need.
    /// </summary>
    public interface IMenuServantSource
    {
        Menu menu { get; }
    }

    public interface IOcgcoreServantSource
    {
        Ocgcore ocgcore { get; }
    }

    public interface IRoomServantSource
    {
        Room room { get; }
    }

    public interface ICardDescriptionServantSource
    {
        CardDescription cardDescription { get; }
    }

    public interface ISelectServerServantSource
    {
        SelectServer selectServer { get; }
    }

    public interface IServantTransition
    {
        void shiftToServant(object servant);
    }

    public interface IReplayRecordBuffer
    {
        void clearReplayRecordBuffer();
    }

    /// <summary>
    /// Composite compatibility interface retained for Program-backed implementations.
    /// Prefer the narrower capability interfaces above in consumers.
    /// </summary>
    public interface IServantLocator :
        IMenuServantSource,
        IOcgcoreServantSource,
        IRoomServantSource,
        ICardDescriptionServantSource,
        ISelectServerServantSource,
        IServantTransition,
        IReplayRecordBuffer
    {
    }
}
