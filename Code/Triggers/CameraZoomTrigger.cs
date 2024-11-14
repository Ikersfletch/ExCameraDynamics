using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

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

        public string DeleteFlag = "";

        public Boundary ZoomBoundary = Boundary.SetsNearest;

        public CameraZoomTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Depth = (int)(-data.Position.X -data.Position.Y);
            ZoomMode = data.Enum<Mode>("mode", Mode.Start);
            ZoomFactorEnd = data.Float("zoomEnd", 1f);
            ZoomFactorStart = data.Float("zoomStart", 1f);
            ZoomBoundary = data.Bool("isMax", true) ? Boundary.SetsNearest : Boundary.SetsFurthest;
            DeleteFlag = data.Attr("deleteFlag", "");
        }

        public override void OnStay(Player player)
        {
            base.OnStay(player);


            if (string.IsNullOrEmpty(DeleteFlag) || !SceneAs<Level>().Session.GetFlag(DeleteFlag))
            {
                if (!CameraZoomHooks.MostRecentTriggerBounds.HasValue)
                {
                    CameraZoomHooks.MostRecentTriggerBounds = new ZoomBounds(CameraZoomHooks.RestingZoomFactor);
                }

                ZoomBounds bounds = CameraZoomHooks.MostRecentTriggerBounds.Value;

                if (ZoomBoundary == Boundary.SetsFurthest)
                {
                    bounds.Furthest = GetZoom(player.Center);
                    bounds.Nearest = Math.Max(bounds.Furthest, bounds.Nearest);
                }
                else
                {
                    bounds.Nearest = GetZoom(player.Center);
                    bounds.Furthest = Math.Min(bounds.Furthest, bounds.Nearest);
                }

                CameraZoomHooks.MostRecentTriggerBounds = bounds;
            }
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);

            if ((Scene as Level)?.Tracker == null)
            {
                CameraZoomHooks.MostRecentTriggerBounds = null;
                return;
            }

            foreach (CameraZoomTrigger trigger in base.Scene?.Tracker?.GetEntities<CameraZoomTrigger>())
            {
                if (trigger.PlayerIsInside)
                {
                    return;
                }
            }
            CameraZoomHooks.MostRecentTriggerBounds = null;
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

        public bool IsActive(Level level)
        {
            return Active && (string.IsNullOrEmpty(DeleteFlag) || !level.Session.GetFlag(DeleteFlag));
        }
    }
}
