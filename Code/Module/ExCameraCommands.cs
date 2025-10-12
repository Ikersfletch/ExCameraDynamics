using Celeste;
using Celeste.Mod;
using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace ExtendedCameraDynamics.Code.Module
{
    public class ExCameraCommands
    {
        [Command("excam_render_chapter", "Renders every level in this chapter, at native resolution.\n The images are found in <Celeste Directory>/Saves/excam_chapter_renders/...")]
        public static void ScreenshotBin(bool render_debug = false, bool render_foreground = true, bool render_background = true)
        {

            Level l = Engine.Scene as Level;
            Player p = l?.Tracker?.GetEntity<Player>();

            if (p == null)
            {
                throw new Exception("Need active player in level");
            }

            if (!CameraZoomHooks.HooksEnabled)
            {
                throw new Exception("Enable hooks to use!!");
            }

            p.Visible = CameraZoomHooks.AutomaticZooming = false;

            string starting_room = l.Session.Level;

            // Just create this now, it won't change over the loop...
            string save_folder = Path.Combine(UserIO.SavePath, "excam_chapter_renders", l.Session.MapData.Filename);
            Directory.CreateDirectory(save_folder);

            // Prepare for rendering
            GameplayRenderer.RenderDebug = render_debug;
            Engine.Commands.Open = false;
            CameraZoomHooks.BoundBufferSize = false;

            // a generic buffer for the color data
            Color[] clipData = null;


            foreach (string levelname in l.Session.MapData.levelsByName.Keys)
            {

                if (levelname.Equals("end-cinematic"))
                {
                    continue;
                }
                // skip levels w/out a respawn (these are not accessible to the player)
                LevelData data = l.Session.MapData.levelsByName[levelname];
                if (data.Spawns.Count <= 0)
                {
                    continue;
                }


                // put camera into position *before* loading entities (for visibility)
                float zoom = Math.Min(320f / data.Bounds.Width, 180f / data.Bounds.Height);
                CameraFocus focus = CameraFocus.FromCenter(data.Bounds.Center.ToVector2(), zoom);
                CameraZoomHooks.ForceCameraTo(l, focus);
                CameraZoomHooks.TriggerZoomOverride = zoom;

                // move to the level in question
                l.TeleportTo(p, levelname, Player.IntroTypes.None, null);
                l.Wipe.Visible = false;

                // TeleportTo messes w/ camera
                CameraZoomHooks.ForceCameraTo(l, focus);
                // Make the player invisible
                l.Tracker.GetEntity<Player>().Visible = false;

                // Render the level
                l.Update();
                l.BeforeRender();
                l.Render();
                l.AfterRender();

                // Determine what portion of the render buffer is being saved to disk
                Rectangle clip = new Rectangle(
                        l.Bounds.X - (int)l.Camera.Position.X,
                        l.Bounds.Y - (int)l.Camera.Position.Y,
                        l.Bounds.Width,
                        l.Bounds.Height
                    ).ClampTo(
                        new Rectangle(0, 0, GameplayBuffers.Level.Width, GameplayBuffers.Level.Height)
                    );

                // Put the data from the render buffer into clipData
                int count = clip.Width * clip.Height;
                if ((clipData?.Length ?? 0) < count)
                {
                    clipData = new Color[count];
                }
                GameplayBuffers.Level.Target.GetData(0, clip, clipData, 0, count);

                // open file for writing
                string filename = Path.Combine(save_folder, levelname+".png");
                using Stream stream = File.OpenWrite(filename);
                GCHandle dataHandler = GCHandle.Alloc(clipData, GCHandleType.Pinned);

                try {
                    // save to disk
                    FNA3D.WritePNGStream(stream, clip.Width, clip.Height, clip.Width, clip.Height, dataHandler.AddrOfPinnedObject());
                }
                finally
                {
                    // clean up after saving
                    if (dataHandler.IsAllocated)
                    {
                        dataHandler.Free();
                    }
                }
            }

            // return to original room, reset camera shenanigans.
            l.TeleportTo(p, starting_room, Player.IntroTypes.Respawn);
            l.Wipe.Visible = true;
            CameraZoomHooks.TriggerZoomOverride = -1f;
            l.Update();
            CameraZoomHooks.AutomaticZooming = CameraZoomHooks.BoundBufferSize = true;
        }


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
