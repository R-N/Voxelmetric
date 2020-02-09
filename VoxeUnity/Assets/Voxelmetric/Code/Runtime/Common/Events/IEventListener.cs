namespace Voxelmetric
{
    public interface IEventListener<TEvent>
    {
        void OnNotified(IEventSource<TEvent> source, TEvent evt);
    }
}
