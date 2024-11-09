using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace ExtendedCameraDynamics.Code.Triggers
{
    [Tracked]
    [CustomEntity("ExCameraDynamics/CameraSnapTrigger")]
    public class CameraSnapTrigger : Trigger
    {
        public float SnapSpeed = 1f;

        public CameraSnapTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Depth = (int)(-data.Position.X - data.Position.Y);

            SnapSpeed = data.Float("snapSpeed", 1f);
        }

    }
}
