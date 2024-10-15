using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.ExCameraDynamics.Code.Entities
{
    /// <summary>
    /// Also blocks the camera.
    /// </summary>
    [Tracked]
    public class FakeRoomEdge : Solid
    {
        public FakeRoomEdge(Vector2 position, float width, float height): base(position, width, height, false) {
            Visible = false;
            Add(new ClimbBlocker(edge: true));
            SurfaceSoundIndex = 33;
        }
    }
}
