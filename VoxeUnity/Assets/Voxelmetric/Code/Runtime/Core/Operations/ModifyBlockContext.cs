using System;
using UnityEngine.Assertions;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    public class ModifyBlockContext
    {
        //! World this operation belong to
        private readonly World world;
        //! Action to perform once all child actions are finished
        private readonly Action<ModifyBlockContext> action;
        //! Block which is to be worked with
        public readonly BlockData block;
        //! Starting block position within chunk
        public readonly int indexFrom;
        //! Ending block position within chunk
        public readonly int indexTo;
        //! Parent action
        private int childActionsPending;
        //! If true we want to mark the block as modified
        public readonly bool setBlockModified;

        public ModifyBlockContext(Action<ModifyBlockContext> action, World world, int index, BlockData block,
            bool setBlockModified)
        {
            this.world = world;
            this.action = action;
            this.block = block;
            indexFrom = indexTo = index;
            childActionsPending = 0;
            this.setBlockModified = setBlockModified;
        }

        public ModifyBlockContext(Action<ModifyBlockContext> action, World world, int indexFrom, int indexTo,
            BlockData block, bool setBlockModified)
        {
            this.world = world;
            this.action = action;
            this.block = block;
            this.indexFrom = indexFrom;
            this.indexTo = indexTo;
            childActionsPending = 0;
            this.setBlockModified = setBlockModified;
        }

        public void RegisterChildAction()
        {
            ++childActionsPending;
        }

        public void ChildActionFinished()
        {
            // Once all child actions are performed register this action in the world
            --childActionsPending;
            Assert.IsTrue(childActionsPending >= 0);
            if (childActionsPending == 0)
            {
                world.RegisterModifyRange(this);
            }
        }

        public void PerformAction()
        {
            action(this);
        }
    }
}
