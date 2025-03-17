using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace ExtendedCameraDynamics.Code.Triggers
{
    [Tracked(true)]
    [CustomEntity("ExCameraDynamics/CameraSnapTrigger")]
    public class CameraSnapTrigger : Trigger
    {
        public float SnapSpeed = 1f; // I was a fool and made it public.
        public virtual float Snap { get => SnapSpeed; set => SnapSpeed = value; } // lesson learned.

        public CameraSnapTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Depth = (int)(-data.Position.X - data.Position.Y);

            Snap = data.Float("snapSpeed", 1f);
        }
    }
}
