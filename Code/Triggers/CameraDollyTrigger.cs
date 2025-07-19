using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace ExtendedCameraDynamics.Code.Triggers
{
    [CustomEntity("ExCameraDynamics/CameraDollyTrigger")]
    public class CameraDollyTrigger : CameraSliderZoomTrigger
    {
        public CameraDollyTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Duration = data.Float("duration");
            this.ZoomMode = Mode.TopToBottom;
        }
        public float Duration { get; set; } = 1f;
        public float Timer { get; set; } = 0f;
        public override void OnStay(Player player)
        {
            base.OnStay(player);
            Timer += Engine.DeltaTime;
        }
        public override void OnLeave(Player player)
        {
            base.OnLeave(player);
            Timer = 0f;
        }
        public override float GetZoom(Vector2 position)
        {
            float t = Math.Clamp(Timer / Duration, 0f, 1f);
            return StartZF * (1f - t) + EndZF * t;
        }
    }
}
