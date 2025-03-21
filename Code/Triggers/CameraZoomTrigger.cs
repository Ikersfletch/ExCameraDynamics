using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;


namespace Celeste.Mod.ExCameraDynamics.Code.Triggers
{
    [Tracked(true)]
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
        
        // I was a fool and made these fields public.

        public float ZoomFactorEnd = 1f;
        public float ZoomFactorStart = 1f;

        // suffer the redundancy of backwards compatibility with foolishness...
        public virtual float EndZF { get => ZoomFactorEnd; set => ZoomFactorEnd = value; }
        public virtual float StartZF { get => ZoomFactorStart; set => ZoomFactorStart = value; }

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

        private bool _inactive = true;

        public CameraZoomTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Depth = (int)(-data.Position.X -data.Position.Y);
            ZoomMode = data.Enum<Mode>("mode", Mode.Start);
            EndZF = data.Float("zoomEnd", 1f);
            StartZF = data.Float("zoomStart", 1f);
            ZoomBoundary = data.Bool("isMax", true) ? Boundary.SetsNearest : Boundary.SetsFurthest;
            DeleteFlag = data.Attr("deleteFlag", "");
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            _inactive = false;
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
                    return Calc.ClampedMap(position.Y, Top, Bottom, StartZF, EndZF);
                case Mode.BottomToTop:
                    return Calc.ClampedMap(position.Y, Bottom, Top, StartZF, EndZF);
                case Mode.LeftToRight:
                    return Calc.ClampedMap(position.X, Left, Right, StartZF, EndZF);
                case Mode.RightToLeft:
                    return Calc.ClampedMap(position.X, Right, Left, StartZF, EndZF);
                default:
                    return StartZF;
            }
        }

        public bool IsActive(Level level)
        {
            return (Active || _inactive) && (string.IsNullOrEmpty(DeleteFlag) || !level.Session.GetFlag(DeleteFlag));

        }
    }
}
