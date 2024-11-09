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

        [Command("excam_force_zoom", "Forces the zoom factor to the specified value. Negative values undo its effect.")]
        public static void UnsafeSetZoom(float factor)
        {
            CameraZoomHooks.TriggerZoomOverride = factor;
        }

        [Command("excam_set_resting_zoom", "Sets the default zoom to the specified factor. Negative values reset to the value specified in the Chapter's metadata.")]
        public static void SetRestingZoomFactor(float factor)
        {
            CameraZoomHooks.RestingZoomFactorOverride = factor;
        }

        [Command("excam_set_snap_speed", "Sets the camera snapping speed to the specified factor. Negative values reset to default; Default is 1.")]
        public static void SetSnappingSpeed(float speed) => CameraZoomHooks.SetSnappingSpeed(speed);

        [Command("excam_unstick_zoom", "forces automatic zooming if camera hooks are enabled.")]
        public static void ForceAutomaticZoom()
        {
            (Engine.Scene as Level)?.Entities?.FindAll<Dummy>()?.ForEach((dummy) => dummy.RemoveSelf());
            CameraZoomHooks.AutomaticZooming = true;
        }
        //{
            [Command("excam_zoom_to_reference_frame", "zoom to the CameraReferenceFrame with easy key")]
            public static void ZoomToCameraReferenceFrame(string easyKey, float duration = 1f, bool reset_in_gameplay = false)
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
                Dummy dummy = new Dummy(level, frame, duration, reset_in_gameplay);
                level.Add(dummy);
                return;
            }
            private class Dummy : Entity
            {
                private bool ResetZoomInGameplay = false;
                public Dummy(Level level, CameraReferenceFrame frame, float duration, bool resetZoomInGameplay = true)
                {
                    level?.Entities?.FindFirst<Dummy>()?.RemoveSelf();

                    ResetZoomInGameplay = resetZoomInGameplay;
                    
                    Add(new Coroutine(Do(new Coroutine(level.ZoomToFocus(frame, duration)) { RemoveOnComplete = true })));
                }
                public IEnumerator Do(Coroutine doMe)
                {
                    while (doMe.Active)
                    {
                        if (ResetZoomInGameplay && !SceneAs<Level>().InCutscene)
                        {
                            RemoveSelf();
                            ForceAutomaticZoom();
                            yield break;
                        }

                        doMe.Update();
                        yield return null;
                    }

                    while (ResetZoomInGameplay && SceneAs<Level>().InCutscene)
                    {
                        yield return null;
                    }
                    if (ResetZoomInGameplay)
                        ForceAutomaticZoom();
                    RemoveSelf();
                }
            }
        //}
    }
}
