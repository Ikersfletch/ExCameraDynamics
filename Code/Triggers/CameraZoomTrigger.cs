using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ExCameraDynamics.Code.Triggers
{
    [Tracked]
    [CustomEntity("ExCameraDynamics/CameraZoomTrigger")]
    public class CameraZoomTrigger : Trigger
    {
        public enum Mode
        {
            Start,
            TopToBottom,
            BottomToTop,
            LeftToRight,
            RightToLeft
        }
        public Mode ZoomMode;
        public float ZoomFactorEnd = 1f;
        public float ZoomFactorStart = 1f;

        /// <summary>
        /// <see cref="Boundary.SetsNearest"/>: (default) this trigger sets how zoomed in the camera is allowed to be- a maximum on the zoom factor<br></br>
        /// <see cref="Boundary.SetsFurthest"/>: this trigger sets how zoomed out the camera is allowed to be- a minimum on the zoom factor
        /// </summary>
        public enum Boundary : byte
        {
            SetsNearest,
            SetsFurthest
        }

        public Boundary ZoomBoundary = Boundary.SetsNearest;

        public CameraZoomTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Depth = (int)(-data.Position.X -data.Position.Y);
            ZoomMode = data.Enum<Mode>("mode", Mode.Start);
            ZoomFactorEnd = data.Float("zoomEnd", 1f);
            ZoomFactorStart = data.Float("zoomStart", 1f);
            ZoomBoundary = data.Bool("isMax", true) ? Boundary.SetsNearest : Boundary.SetsFurthest;
        }
        public virtual float GetZoom(Vector2 position)
        {
            switch (ZoomMode)
            {
                case Mode.TopToBottom:
                    return Calc.ClampedMap(position.Y, Top, Bottom, ZoomFactorStart, ZoomFactorEnd);
                case Mode.BottomToTop:
                    return Calc.ClampedMap(position.Y, Bottom, Top, ZoomFactorStart, ZoomFactorEnd);
                case Mode.LeftToRight:
                    return Calc.ClampedMap(position.X, Left, Right, ZoomFactorStart, ZoomFactorEnd);
                case Mode.RightToLeft:
                    return Calc.ClampedMap(position.X, Right, Left, ZoomFactorStart, ZoomFactorEnd);
                default:
                    return ZoomFactorStart;
            }
        }
    }
}
