using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Triggers;
using Celeste.Mod.ExCameraDynamics;
using Microsoft.Xna.Framework;
using Monocle;

namespace ExtendedCameraDynamics.Code.Triggers
{
    [CustomEntity("ExCameraDynamics/CameraSliderZoomTrigger")]
    public class CameraSliderZoomTrigger : CameraZoomTrigger
    {
        private CalcPlus.IFloatSource zoomStart = null;
        private CalcPlus.IFloatSource zoomEnd = null;
        public override float StartZF {
            get => zoomStart.Value;
            set => zoomStart = new CalcPlus.RawFloat(value);
        }
        public override float EndZF
        {
            get => zoomEnd.Value;
            set => zoomEnd = new CalcPlus.RawFloat(value);
        }
        public CameraSliderZoomTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            Session session = (Engine.Scene as Level)?.Session;
            zoomStart = CalcPlus.CreateFloatSource(data, session, "zoomStart");
            zoomEnd = CalcPlus.CreateFloatSource(data, session, "zoomEnd");
        }


        //[Command("TEST_SLIDER_SHIT", "poaihsdjf")]
        // public static void TEST(string name, float value)
        //{
        //    (Engine.Scene as Level).Session.SetSlider(name, value);
        //}
    }
}
