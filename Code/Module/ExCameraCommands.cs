using Celeste;
using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Monocle;
using System;
using System.Collections;

namespace ExtendedCameraDynamics.Code.Module
{
    public class ExCameraCommands
    {

        [Command("excam_is_active", "lets find out")]
        public static void IAMLOADED()
        {
            throw new Exception(CameraZoomHooks.HooksEnabled ? "True" : "False");
        }

        [Command("excam_enable_hooks", "Enables ExtendedCameraDynamic hooks. Can be used in any level.")]
        public static void ManuallyHook()
        {
            CameraZoomHooks.Hook();
            CameraZoomHooks.ResizeVanillaBuffers((Engine.Scene as Level)?.Zoom ?? 1f);
        }

        [Command("excam_force_zoom", "forcibly sets the zoom to the specified factor. negative values reset to normal behavior. force_zoom <factor>")]
        public static void UnsafeSetZoom(float factor)
        {
            CameraZoomHooks.TriggerZoomOverride = factor;
        }

        [Command("excam_set_resting_zoom", "sets the default zoom to the specified factor. negative values reset to normal behavior.")]
        public static void SetRestingZoomFactor(float factor)
        {
            CameraZoomHooks.RestingZoomFactorOverride = factor;
        }

        [Command("excam_unstick_zoom", "forces automatic zooming if camera hooks are enabled.")]
        public static void ForceAutomaticZoom()
        {
            CameraZoomHooks.AutomaticZooming = true;
        }
        //{
            [Command("excam_zoom_to_reference_frame", "zoom to the CameraReferenceFrame with easy key")]
            public static void ZoomToCameraReferenceFrame(string easyKey, float duration = 1f)
            {

                Level level = (Engine.Scene as Level);
                if (level == null)
                {
                    return;
                }
                CameraReferenceFrame frame = CameraReferenceFrame.GetFromEasyKey(level, easyKey);
                if (frame == null)
                {
                    return;
                }
                Dummy dummy = new Dummy(level, frame, duration);
                level.Add(dummy);
                return;
            }
            private class Dummy : Entity
            {
                public Dummy(Level level, CameraReferenceFrame frame, float duration)
                {
                    Add(new Coroutine(Do(new Coroutine(level.ZoomToFocus(frame, duration)) { RemoveOnComplete = true })));
                }
                public IEnumerator Do(Coroutine doMe)
                {
                    while (doMe.Active)
                    {
                        doMe.Update();
                        yield return null;
                    }
                    RemoveSelf();
                }
            }
        //}
    }
}
