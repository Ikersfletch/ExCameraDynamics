using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ExCameraDynamics.Code.Entities
{
    /// <summary>
    /// Also blocks the camera.
    /// </summary>
    [Tracked]
    public class FakeRoomEdge : Solid
    {
        public FakeRoomEdge(EntityData data, Vector2 offset) : this(data.Position + offset, data.Width, data.Height) { }
        public FakeRoomEdge(Vector2 position, float width, float height): base(position, width, height, false) {
            Visible = false;
            Add(new ClimbBlocker(edge: true));
            SurfaceSoundIndex = 33;
        }
    }
}
