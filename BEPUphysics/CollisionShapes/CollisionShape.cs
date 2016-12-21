using System;

namespace BEPUphysics.CollisionShapes
{
    ///<summary>
    /// Superclass of all collision shapes.
    /// Collision shapes are composed entirely of local space information.
    /// Collidables provide the world space information needed to use the shapes to do collision detection.
    ///</summary>
    public abstract class CollisionShape
    {
        ///<summary>
        /// Fires when some of the local space information in the shape changes.
        ///</summary>
        public event Action<CollisionShape> ShapeChanged;

        public void ClearShapeChanged()
        {
            ShapeChanged = null;
        }

        protected virtual void OnShapeChanged()
        {
                ShapeChanged?.Invoke(this);
        }

        public CollisionShape Duplicate()
        {
            return (CollisionShape)MemberwiseClone();
        }

    }
}
