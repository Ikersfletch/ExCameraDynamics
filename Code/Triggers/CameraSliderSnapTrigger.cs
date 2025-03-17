using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendedCameraDynamics.Code.Triggers
{
    [CustomEntity("ExCameraDynamics/CameraSliderSnapTrigger")]
    public class CameraSliderSnapTrigger : CameraSnapTrigger
    {
        private CalcPlus.IFloatSource speed = null;
        public override float Snap { 
            get => speed.Value;
            set => speed = new CalcPlus.RawFloat(value);
        }
        public CameraSliderSnapTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            speed = CalcPlus.CreateFloatSource(data, (Engine.Scene as Level)?.Session, "snapSpeed");
        }
    }
}
