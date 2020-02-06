using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;

namespace Voxelmetric.Code.Core
{
    public class ChunkEventSource : IEventSource<ChunkStateExternal>
    {
        //! List of external listeners
        private List<IEventListener<ChunkStateExternal>> listenersExternal;

        protected ChunkEventSource()
        {
            Clear();
        }

        public void Clear()
        {

            listenersExternal = new List<IEventListener<ChunkStateExternal>>();
        }

        #region IEventSource<ChunkStateExternal>

        public bool Register(IEventListener<ChunkStateExternal> listener)
        {
            Assert.IsTrue(listener != null);
            if (!listenersExternal.Contains(listener))
            {
                listenersExternal.Add(listener);
                return true;
            }

            return false;
        }

        public bool Unregister(IEventListener<ChunkStateExternal> listener)
        {
            Assert.IsTrue(listener != null);
            return listenersExternal.Remove(listener);
        }

        public void NotifyAll(ChunkStateExternal evt)
        {
            for (int i = 0; i < listenersExternal.Count; i++)
            {
                IEventListener<ChunkStateExternal> listener = listenersExternal[i];
                listener.OnNotified(this, evt);
            }
        }

        #endregion
    }
}
