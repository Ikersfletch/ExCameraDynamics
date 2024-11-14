using Celeste.Mod.ExCameraDynamics.Code.Components;
using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;

namespace Celeste.Mod.ExCameraDynamics.Code.Hooks
{
    public static partial class CameraZoomHooks
    {
        public static void ForceCameraTo(this Level level, CameraFocus focus)
        {
            AutomaticZooming = false;
            level.Zoom = level.ZoomTarget = focus.Zoom;
            level.ZoomFocusPoint = new Vector2(160f, 90f) / focus.Zoom;
            level.Camera.Viewport.Width = (int)(320f / focus.Zoom);
            level.Camera.Viewport.Height = (int)(180f / focus.Zoom);
            level.Camera.Position = focus.Position;
        }
        public static IEnumerator ZoomToFocus(this Level level, ICameraFocusSource source, float duration)
        {
            AutomaticZooming = false;
            CameraFocus start = new CameraFocus(level);
            for (float t = 0.0f; t < 1.0f; t += Engine.DeltaTime / duration)
            {
                CameraFocus to = source.CameraFocus;
                level.ForceCameraTo(start.Lerp(to, (float)Math.Clamp(Ease.SineInOut(t), 0.0, 1.0)));
                level.ZoomTarget = to.Zoom;
                yield return null;
            }
            level.ForceCameraTo(source.CameraFocus);
        }

        public static IEnumerator FollowFocus(this Level level, ICameraFocusSource source, float duration)
        {
            AutomaticZooming = false;
            for (float t = 0.0f; t < 1.0f; t += Engine.DeltaTime / duration)
            {
                level.ForceCameraTo(source.CameraFocus);
                yield return null;
            }
        }

        public static IEnumerator ZoomToThenFollow(this Level level, ICameraFocusSource source, float zoomToDuration, float followDuration)
        {
            AutomaticZooming = false;
            CameraFocus start = new CameraFocus(level);
            for (float t = 0.0f; t < 1.0f; t += Engine.DeltaTime / zoomToDuration)
            {
                CameraFocus to = source.CameraFocus;
                level.ForceCameraTo(start.Lerp(to, (float)Math.Clamp(Ease.SineInOut(t), 0.0, 1.0)));
                level.ZoomTarget = to.Zoom;
                yield return null;
            }
            for (float t = 0.0f; t < 1.0f; t += Engine.DeltaTime / followDuration)
            {
                level.ForceCameraTo(source.CameraFocus);
                yield return null;
            }
        }
        public static IEnumerator ZoomBackFocus(this Level level, float duration)
        {
            CameraFocus start = new CameraFocus(level);
            //float to = ;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime / duration)
            {
                level.ForceCameraTo(start.Lerp(CameraFocus.FullZoomEval(level), (float)Math.Clamp(Ease.SineInOut(p), 0.0, 1.0)));
                yield return null;
            }

            level.ResetZoom();
        }


        private static Vector2 _camera_floating_decimal = new Vector2(0f,0f);
        // Check to see if the buffers need to be resized.
        private static void Level_BeforeRender(On.Celeste.Level.orig_BeforeRender orig, Level self)
        {
            float trigger_nearest_zoom = self.Tracker.GetEntity<Player>()?.GetNearestZoomPossible(self) ?? 1f;
            if (ShouldResize(trigger_nearest_zoom, self))
            {
                // Modify buffers to match zoom
                ResizeVanillaBuffers(trigger_nearest_zoom);

                // Used to see when the buffers got resized. Useful to have.
                //self.Flash(Color.GhostWhite, true);

                // Trigger the listeners
                if (finished_update) // not more than once per game update
                {
                    finished_update = false;

                    // trigger resize events.
                    self.OnEndOfFrame += () =>
                    {
                        foreach (CameraZoomListener component in self.Tracker.GetComponents<CameraZoomListener>())
                        {
                            if (component.Active) continue;
                            component.OnZoomUpdate?.Invoke(trigger_nearest_zoom);
                        }
                        finished_update = true;
                    };
                }
            }


            self.Camera.Viewport.Width = (int)(320f / self.Zoom);
            self.Camera.Viewport.Height = (int)(180f / self.Zoom);
            self.ZoomFocusPoint = new Vector2(160f, 90f) / self.Zoom;

            _camera_floating_decimal = self.Camera.Position - self.Camera.Position.Floor();

            if (SaveData.Instance.VariantMode && SaveData.Instance.Assists.MirrorMode)
            {
                _camera_floating_decimal.X *= -1f;
            }

            orig(self);
        }
        private static void Level_ZoomBack(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Level>>((level) => 
                {
                    level.ZoomTarget = ZoomTarget;
                    AutomaticZooming = false;
                }
            );
            //cursor.EmitDelegate<Action>(() => AutomaticZooming = true);
            //cursor.Emit(OpCodes.Ldc_I4_0);
            //cursor.Emit(OpCodes.Ret);

            cursor.GotoNext(next => next.MatchLdsfld(typeof(Ease).GetField("SineInOut")));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, Level, float>>((to, level) => 
                {
                    level.ZoomTarget = ZoomTarget;
                    return ZoomTarget;
                }
            );

        }
        private static void Level_ZoomAcross(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.EmitDelegate<Action>(() => AutomaticZooming = false);
        }
        private static void Level_ZoomTo(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.EmitDelegate<Action>(() => AutomaticZooming = false);
        }
        private static void Level_ResetZoom(On.Celeste.Level.orig_ResetZoom orig, Level self)
        {
            self.ForceCameraTo(CameraFocus.FullZoomEval(self));
            AutomaticZooming = true;
        }

        private static float _level_render_override_zoom_target(Level level, float discarded_zoom_target) => level.Zoom;
        private static Vector2 _level_render_smooth_camera_motion(Vector2 renderPos, Level level)
        {
            if (level.Zoom <= 1f) return renderPos;

            float factor = (level.Zoom * ((320f - level.ScreenPadding * 2f) / 320f));
            renderPos.X -= _camera_floating_decimal.X * factor;
            renderPos.Y -= _camera_floating_decimal.Y * factor;

            return renderPos;
        }
        private static Vector2 _level_render_account_for_mirror_mode(Vector2 screen_space)
        {
            // I don't know why this works.
            // I don't need to do this outside of Mirror Mode.
            // Mathematically, I have no idea why this alone fixes it.
            // Oh well!

            if (SaveData.Instance.Assists.MirrorMode)
            {
                screen_space.X /= 320f;
                screen_space.X *= BufferWidthOverride;
            }

            if (VerticalMirroring)
            {
                screen_space.Y /= 180f;
                screen_space.Y *= BufferHeightOverride;
            }

            return screen_space;
        }
        public static void Level_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // Override the lightning flash
            cursor.Index = 0;
            cursor.GotoNext(next => next.MatchLdcR4(322));
            cursor.PopNext();
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Level, float>>(level => GetCameraWidth(level) + 2);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Level, float>>(level => GetCameraHeight(level) + 2);


            cursor.GotoNext(next => next.MatchLdcR4(320f));
            //cursor.PopNext();
            //cursor.EmitDelegate<Func<float>>(GetBufferWidth);

            // ZoomTarget now represents where a manual zoom routine is trying to zoom to-
            // it's not representative of the actual zoom levels.
            // as such, we're replacing it with level.Zoom in the rendering code.
            // I have not noticed a difference with vanilla as a result of this.
            cursor.GotoNext(next => next.MatchLdcR4(180f));

            cursor.GotoNext(next => next.MatchLdfld<Level>("ZoomTarget"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Index++;
            cursor.EmitDelegate<Func<Level, float, float>>(_level_render_override_zoom_target);

            cursor.GotoNext(next => next.MatchLdfld<Level>("ZoomTarget"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Index++;
            cursor.EmitDelegate<Func<Level, float, float>>(_level_render_override_zoom_target);

            // Vector2 vector4 = new Vector2(ScreenPadding, ScreenPadding * 0.5625f);
            cursor.GotoNext(next => next.MatchLdloca(9));
            // vector4.X = -vector4.X
            cursor.GotoNext(next => next.MatchLdloca(9));
            cursor.GotoNext(next => next.MatchLdloc(5));
            cursor.Index++;
            // param in draw call
            cursor.GotoNext(next => next.MatchLdloc(5));
            cursor.Index += 3;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Vector2, Level, Vector2>>(_level_render_smooth_camera_motion);

            cursor.Index++;
            // finally, we're here...
            cursor.GotoNext(next => next.MatchLdloc(5));
            cursor.Index++;
            cursor.EmitDelegate<Func<Vector2, Vector2>>(_level_render_account_for_mirror_mode);
        }


        private static void Level_EnforceBounds(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchLdcI4(320));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Level, int>>(GetCameraWidthInt);

            cursor.GotoNext(next => next.MatchLdcI4(180));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Level, int>>(GetCameraHeightInt);
        }
        public static void Level_IsInCamera(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(instr => instr.MatchLdcI4(320));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Level, int>>(GetCameraWidthInt);
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Level, int>>(GetCameraHeightInt);
        }

        // I could use IL. Or not.
        private static bool Level_InsideCamera(On.Celeste.Level.orig_InsideCamera orig, Level self, Vector2 position, float expand)
        {
            //if (self.Zoom >= 1f) return orig(self, position, expand);

            if (position.X < self.Camera.X - expand) return false;

            if (position.Y < self.Camera.Y - expand) return false;

            if (position.X > self.Camera.X + Math.Ceiling(320f / self.Zoom) + expand) return false;

            if (position.Y > self.Camera.Y + Math.Ceiling(180f / self.Zoom) + expand) return false;

            return true;
        }

        private static float _level_loadlevel_preserve_zoom(Level level) => level.Zoom;
        private static void _level_loadlevel_reset_buffers(Level self, Player player)
        {
            self.ForceCameraTo(CameraFocus.FullZoomEvalLoading(player, self));
            AutomaticZooming = true;
            MostRecentTriggerBounds = null;
            //ResizeVanillaBuffers(self.Zoom);
        }
        public static void Level_LoadLevel_orig(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            /* yes, this does change: "level.Zoom = 1f;" 
             * to "level.Zoom = level.Zoom;"
             * and I think that's great.
             * it's what all optimisations strive towards! */
            cursor.GotoNext(next => next.MatchStfld<Level>("Zoom"));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_0);                       // param: the Level
            cursor.EmitDelegate<Func<Level, float>>(_level_loadlevel_preserve_zoom);
            // we do this non-assignment because we need to wait for zoom triggers to be added to the scene:

            // we need to make sure EntityList.UpdateLists() has been called...
            cursor.GotoNext(next => next.MatchLdloc((byte)53));
            cursor.GotoNext(next => next.MatchCallvirt<EntityList>("UpdateLists"));
            cursor.Index++;

            // This double-resize is stupid, but it makes sure that reloading with F5 or respawning after golden deaths don't result in glitched appearances.
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, 53);
            cursor.EmitDelegate<Action<Level,Player>>(_level_loadlevel_reset_buffers);
        }

        private static Vector2 Level_ScreenToWorld(On.Celeste.Level.orig_ScreenToWorld orig, Level self, Vector2 screen_position)
        {
            // padding accounts for stuff like watchtowers. 
            Vector2 padding = new Vector2(self.ScreenPadding, self.ScreenPadding * 9f / 16f);

            // the actual render scale
            float render_scaling = (self.Zoom * ((320f - self.ScreenPadding * 2f) / 320f));

            Vector2 adjusted_position = (screen_position / 6.0f) - padding;

            Vector2 camera_relative = adjusted_position / render_scaling;

            return camera_relative + self.Camera.Position;
        }

        private static Vector2 Level_WorldToScreen(On.Celeste.Level.orig_WorldToScreen orig, Level self, Vector2 position)
        {
            const float paddingScale = 9f / 16f;
            // padding accounts for stuff like watchtowers. 
            Vector2 padding = new Vector2(self.ScreenPadding, self.ScreenPadding * paddingScale);

            // the actual render scale
            float render_scaling = (self.Zoom * ((320f - self.ScreenPadding * 2f) / 320f));

            Vector2 camera_relative = position - self.Camera.Position;

            Vector2 adjusted_position = camera_relative * render_scaling;

            return (padding + adjusted_position) * 6f;
        }

    }
}
