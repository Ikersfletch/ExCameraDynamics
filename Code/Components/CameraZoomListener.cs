using Monocle;
using System;

namespace Celeste.Mod.ExCameraDynamics.Code.Components
{
    [Tracked]
    public class CameraZoomListener : Component
    {
        public Action<float> OnZoomUpdate;
        public CameraZoomListener(Action<float> onZoomUpdate = null) : base(true, true)
        {
            OnZoomUpdate = onZoomUpdate;
        }
    }
}
