using Celeste.Mod.ExCameraDynamics.Code.Entities;
using ExtendedCameraDynamics.Code.Entities;
using ExtendedCameraDynamics.Code.Module;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.ExCameraDynamics.Code.Hooks
{
    public struct ZoomBounds
    {
        public float Furthest, Nearest;
        public ZoomBounds(float furthest, float nearest)
        {
            if (nearest < 0)
            {
                if (furthest < 0)
                {
                    this.Nearest = 1f;
                    this.Furthest = 1f;
                    return;
                }
                this.Nearest = 1f;
                this.Furthest = Math.Min(furthest, 1f);
                return;
            }
            this.Nearest = nearest;
            this.Furthest = Math.Min(furthest, nearest);
        }
        public ZoomBounds(float factor) : this(factor, factor) { }
    }
    public static partial class CameraZoomHooks
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MatchesSequence(Instruction start, params Predicate<Instruction>[] sequence)
        {
            Instruction current = start;
            for (int i = 0; i < sequence.Length; i++)
            {
                if (!sequence[i].Invoke(current))
                {
                    return false;
                }
                current = current.Next;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReplaceNextFloat(ILCursor cursor, float target, Func<float> dynamic_delegate) {
            cursor.GotoNext(next => next.MatchLdcR4(target));
            cursor.PopNext();
            cursor.EmitDelegate(dynamic_delegate);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReplaceNextInt(ILCursor cursor, int target, Func<int> dynamic_delegate)
        {
            cursor.GotoNext(next => next.MatchLdcI4(target));
            cursor.PopNext();
            cursor.EmitDelegate(dynamic_delegate);
        }

        public static int GetVisibleCameraWidth()
        {
            Level level = Engine.Scene as Level;
            if (level == null) return 320;
            return GetCameraWidthInt(level);
        }
        public static int GetVisibleCameraHeight()
        {
            Level level = Engine.Scene as Level;
            if (level == null) return 180;
            return GetCameraHeightInt(level);
        }

        public const int MaxBufferWidth = 2560;
        public const int MaxBufferHeight = (MaxBufferWidth * 9) / 16;
        // let's make the camera zoom out.
        public static int BufferWidthOverride { get => _bufferWidth; private set { _bufferWidth = value; } }
        public static int BufferHeightOverride { get => _bufferHeight; private set { _bufferHeight = value; } }

        private static int _bufferWidth = 960;
        private static int _bufferHeight = 540;

        private static float current_buffer_zoom = 1f;
        public static float TriggerZoomOverride = -1f;

        private static float trigger_zoom_target = 1f;
        private static float ZoomTarget => TriggerZoomOverride > 0f ? TriggerZoomOverride : trigger_zoom_target;

        private static bool finished_update = true;

        private static bool ShouldResize(float trigger_nearest_zoom, Level self)
        {
            return
                (
                    AutomaticZooming && current_buffer_zoom != trigger_nearest_zoom && ( // the buffer could be resized...
                        (current_buffer_zoom > trigger_nearest_zoom && self.Zoom < current_buffer_zoom) || // we need to enlarge the buffer for what the zoom could be within this trigger...
                        (self.Zoom >= trigger_nearest_zoom && self.Zoom >= current_buffer_zoom)  // OR we're more zoomed in than we could possibly need to be and the buffer's larger than the screen
                    ) 
                );
        }
        /// <summary>
        /// Determines the bounds the camera can pan towards
        /// </summary>
        /// <param name="level"></param>
        /// <param name="cameraPosition"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Rectangle GetBounds(Level level, Vector2 cameraPosition, int width, int height)
        {
            // get bounds
            Rectangle bounds = level.Bounds;
            int bounds_x = bounds.X;
            int bounds_y = bounds.Y;
            int bounds_w = bounds.Width;
            int bounds_h = bounds.Height;

            // the camera center
            Vector2 test_r = cameraPosition + new Vector2(width * 0.5f, height * 0.5f);

            // restrict based on FakeRoomEdges
            foreach (FakeRoomEdge edge in level.Tracker.GetEntities<FakeRoomEdge>())
            {
                
                // if the fake edge is suitable for restricting the x-movement
                if (
                    edge.Top < test_r.Y + (height * 0.5f) && // the fake edge's top is above the bottom of the camera AND
                    edge.Bottom > test_r.Y - (height * 0.5f)) // the fake edge's bottom is below the top of the camera
                {
                    //
                    if (test_r.X > edge.Right && bounds_x < edge.Right)
                    {
                        // remove the left portion of the bounds
                        int dif = (int)(edge.Right - bounds_x);
                        bounds_x += dif;
                        bounds_w -= dif;
                    }
                    else if (test_r.X < edge.Left && bounds_x + bounds_w > edge.Left)
                    {
                        // remove the right portion of the bounds
                        bounds_w -= (int)(bounds_x + bounds_w - edge.Left);
                    }
                }

                // if the fake edge is suitable for restricting y-movement
                if (edge.Right > test_r.X - (width * 0.5f) && // the fake edge's right is to the right of the left of the camera AND
                    edge.Left < test_r.X + (width * 0.5f))  // the fake edge's left is to the left of the right of the camera
                {
                    //
                    if (test_r.Y > edge.Bottom && bounds_y < edge.Bottom)
                    {
                        // remove the top portion of the bounds
                        int dif = (int)(edge.Bottom - bounds_y);
                        bounds_y += dif;
                        bounds_h -= dif;
                    }
                    else if (test_r.Y < edge.Top && bounds_y + bounds_h > edge.Top)
                    {
                        // remove the right portion of the bounds
                        bounds_h -= (int)(bounds_y + bounds_h - edge.Top);
                    }
                }
            }

            return new Rectangle(bounds_x, bounds_y, bounds_w, bounds_h);
        }
        public static float GetBufferWidth()
        {
            return BufferWidthOverride;
        }
        public static float GetBufferHeight()
        {
            return BufferHeightOverride;
        }
        // I am aware that many of these methods are redundant.
        public static float GetCameraWidth(Level level)
        {
            //if (level.Zoom >= 1f) return 320;
            return MathF.Ceiling(320f / level.Zoom);
        }
        public static float GetHalfCameraWidth(Level level)
        {
            //if (level.Zoom >= 1f) return 160;
            return MathF.Ceiling(160f / level.Zoom);
        }
        public static int GetCameraWidthInt(Level level)
        {
            //if (level.Zoom >= 1f) return 320;
            return (int)MathF.Ceiling(320f / level.Zoom);
        }
        public static float GetCameraHeight(Level level)
        {
            //if (level.Zoom >= 1f) return 180;
            return MathF.Ceiling(180f / level.Zoom);
        }
        public static int GetCameraHeightInt(Level level)
        {

            //if (level.Zoom >= 1f) return 180;
            return (int)MathF.Ceiling( 180f / level.Zoom);
        }
        public static Vector2 GetCameraDimensions(Level level)
        {
            //if (level.Zoom >= 1f) return new Vector2(320, 180);
            return new Vector2(MathF.Ceiling(320f / level.Zoom), MathF.Ceiling(180f / level.Zoom));
        }
        public static Vector2 GetHalfCameraDimensions(Level level)
        {
            //if (level.Zoom >= 1f) return new Vector2(160, 90);
            return new Vector2(MathF.Ceiling(160f / level.Zoom), MathF.Ceiling(90f / level.Zoom));
        }

        // Enforces bounds to the new camera size- this implementation also focuses on the center of the room if the camera is larger than the room size.
        private static Vector2 _player_camera_target_trigger_position(Player player)
        {
            CameraFocus focus = CameraFocus.FromZoomEval(player);
            trigger_zoom_target = focus.Zoom;
            return focus.Position;
        }
        private static Vector2 _player_camera_target_clamp_to_level(Player player, Vector2 result)
        {
            Level level = player.level;
            if (level == null) return result;

            // limit the camera's position based on the bounds & zoom
            int width = (int)Math.Ceiling(320 / level.Zoom);
            int height = (width * 9) / 16;
            return ClampedToLevel(result, width, height, level);
        }
        public static void PlayerCameraTarget(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchCall(typeof(Vector2).GetConstructor(new Type[] {typeof(float), typeof(float)})));
            cursor.Index++;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Player, Vector2>>(_player_camera_target_trigger_position);
            cursor.Emit(OpCodes.Stloc_1);

            cursor.GotoNext(next => next.MatchLdfld<Player>("EnforceLevelBounds"));

            cursor.GotoNext(next => next.OpCode == OpCodes.Br_S);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate<Func<Player, Vector2, Vector2>>(_player_camera_target_clamp_to_level);
            cursor.Emit(OpCodes.Stloc_0);
            while (cursor.TryGotoNext(next => next.MatchLdcR4(180f)))
            {
                cursor.PopNext();
                cursor.EmitDelegate<Func<float>>(VisibleHeight);
            }
        }

        // the Camera linearly interpolates its position each frame with the CameraTarget...
        // ..from the top-left corner.
        // if the zoom is constant, this works great, and has identical behavior to any other point.
        // however, if the zoom changes, this doesn't change the position of the top-left corner.
        // so we need to account for this and linearly interpolate with the center instead.
        private static void PlayerCameraInterpolation(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchLdfld<Player>("ForceCameraUpdate"));
            cursor.GotoNext(next => next.MatchStloc(11));
            cursor.Index -= 2;
            cursor.GotoNext(next => next.MatchCallvirt(typeof(Camera).GetProperty("Position").GetSetMethod()));
            cursor.Index -= 2; // after the sub operation; the interpolation amt is at the top of the stack.
            cursor.Emit(OpCodes.Ldarg_0); // player
            cursor.EmitDelegate<Func<Vector2, Vector2, float, Player, Vector2>>(GetCameraInterpolation);
            // This is one of the few times I ever remove operations- replacing method calls.
            // the operations became baked into the above delegate, so behavior is identical.
            // The caveat is that other mods' IL hooks can break... but hopefully that's not THAT big of a deal... right???
            cursor.Remove(); // removes the Vector2 mult
            cursor.Remove(); // removes the Vector2 add
        }
        public static bool AutomaticZooming { get; set; } = true;
        public static Vector2 GetCameraInterpolation(Vector2 cameraPosition, Vector2 targetOffset, float t, Player player)
        {
            
            if (!AutomaticZooming)
            {
                return cameraPosition + targetOffset * t;
            }
            

            Level level = player.level;

            if (level.Zoom != ZoomTarget && level.Zoom == level.ZoomTarget) // we're not at the zoom we want
            {
                float zoomDif = (1f / level.Zoom) - (1f / ZoomTarget);
                float widthDifference = 320f * Math.Abs(zoomDif);

                if (widthDifference < 2.0f)
                {
                    cameraPosition += new Vector2(160f, 90f) * zoomDif;
                    level.Zoom = level.ZoomTarget = ZoomTarget;
                    level.Camera.Viewport.Width = (int)Math.Ceiling(320f / level.Zoom);
                    level.Camera.Viewport.Height = (level.Camera.Viewport.Width * 9) >> 4;
                    level.ZoomFocusPoint = (new Vector2(160f, 90f) / level.Zoom).Floor();
                    return cameraPosition + (player.CameraTarget - cameraPosition) * t;
                }

                float origZoom = level.Zoom;
                float zoomTarget = ZoomTarget;

                // find the true target, accounting for the final zoom
                level.Zoom = zoomTarget;
                Vector2 true_camera_target = player.CameraTarget;

                // see TransitionRoutine.cs for the derivation of this.
                level.Zoom = (origZoom * zoomTarget) / (zoomTarget + t * (origZoom - zoomTarget));

                // adjust the camera to the zoom...
                level.Camera.Viewport.Width = (int)Math.Ceiling(320f / level.Zoom);
                level.Camera.Viewport.Height = (level.Camera.Viewport.Width * 9) / 16;

                // finish adjustment...
                level.ZoomTarget = level.Zoom;
                //level.ZoomFocusPoint = (level.ZoomFocusPoint * origZoom) / level.Zoom;
                level.ZoomFocusPoint = (new Vector2(160f, 90f) / level.Zoom).Round();

                // lerp the rest of the way there...
                return Vector2.Lerp(cameraPosition, true_camera_target, t);
            }


            // just return the interpolation found in vanilla.
            return cameraPosition + targetOffset * t;
        }

        private static Vector2 ClampedToLevel(Vector2 cameraPosition, int width, int height, Level level)
        {
            Rectangle bounds = GetBounds(level, level.Camera.Position, width, height);

            if (bounds.Width < width)
            {
                cameraPosition.X = bounds.Center.X - 0.5f * width; // rooms can be smaller than the camera's size, so account for this.
            }
            else
            {
                cameraPosition.X = MathHelper.Clamp(cameraPosition.X, bounds.X, bounds.X + bounds.Width - width);
            }

            if (bounds.Height < height)
            {
                cameraPosition.Y = bounds.Center.Y - 0.5f * height;
            }
            else
            {
                cameraPosition.Y = MathHelper.Clamp(cameraPosition.Y, bounds.Y, bounds.Y + bounds.Height - height);
            }

            return cameraPosition;
        }

        // It's still based off of the camera here.
        // Should I keep it in a 320/180 around the player to make this vanilla optimization work better?
        private static bool CrystalStaticSpinner_InView(On.Celeste.CrystalStaticSpinner.orig_InView orig, CrystalStaticSpinner self)
        {
            Level level = self.SceneAs<Level>();
            if (level == null /*|| level.Zoom >= 1*/) return orig(self);

            if (self.X < level.Camera.X - 16) return false;

            if (self.Y < level.Camera.Y - 16) return false;

            if (self.X > level.Camera.X + 16 + GetCameraWidth(level)) return false;

            if (self.Y > level.Camera.Y + 16 + GetCameraHeight(level)) return false;

            return true;
        }

        // Positional audio uses the camera position & size.
        // This implementation integrates the player's position, so that the sound doesn't get lost as you zoom out.
        // It's also trying to use the depth parameter to put it more in front of the player as you zoom out.
        // I can't tell a difference regardling 'audio depth', though- so maybe I'm not doing it right?
        private static Vector2 _audio_bend_position_towards_player(Vector2 noPlayer)
        {
            Player player = Engine.Scene?.Tracker?.GetEntity<Player>();
            if (player == null) return noPlayer;

            float zoom = CurrentLevelZoom();

            return (player.Center * (1f - zoom) + noPlayer * zoom);
        }
        private static float _audio_try_push_out_audio() => -(float)Math.Log(CurrentLevelZoom());
        private static void Audio_Position(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ReplaceNextFloat(cursor, 320f, VisibleWidth);
            ReplaceNextFloat(cursor, 180f, VisibleHeight);            

            cursor.GotoNext(next => next.OpCode == OpCodes.Stloc_0);
            cursor.Index++;
            cursor.Emit(OpCodes.Ldloc_0);
            cursor.EmitDelegate<Func<Vector2, Vector2>>(_audio_bend_position_towards_player);
            cursor.Emit(OpCodes.Stloc_0);

            cursor.GotoNext(next => next.MatchStfld<FMOD.VECTOR>("z"));
            cursor.Index--;
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(_audio_try_push_out_audio);
        }

        // so this technically patches the zoom-in case as well.
        // That causes a visual difference from vanilla.
        // Luckily, I don't think most people would care if a visual quirk is corrected.
        public static void CorrectTalkUI(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            ReplaceNextFloat(cursor, 320f, GetBufferWidth);

            ReplaceNextFloat(cursor, 6f, 
                () => 6f * ((Engine.Scene as Level)?.Zoom ?? 1f) 
            );
            ReplaceNextFloat(cursor, 6f,
                () => 6f * ((Engine.Scene as Level)?.Zoom ?? 1f)
            );
        }

        private static float mod(float x, float m)
        {
            return (x % m + m) % m;
        }
        // BigWaterfalls render their sprite until the end of the screen
        private static void BigWaterfall_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            ReplaceNextFloat(cursor, 180f, 
                GetBufferHeight    
            );
        }

        private static Vector2 _big_waterfall_get_parallax_offset(BigWaterfall bigwaterfall) => new Vector2(160, 90) - GetCameraDimensions(bigwaterfall.SceneAs<Level>()) * 0.5f;
        private static void BigWaterfall_RenderPositionAtCamera(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchLdarg(1));
            cursor.Index++;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<BigWaterfall,Vector2>>(_big_waterfall_get_parallax_offset);
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Subtraction"));
        }

        // Lightning stops rendering its flashing when offscreen.
        private static void Lightning_InView(ILContext il)
        {
            //Lightning
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchLdcR4(320f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(Entity).GetMethod("get_Scene"));
            cursor.Emit(OpCodes.Isinst, typeof(Level));
            cursor.EmitDelegate<Func<Level,float>>(GetCameraWidth);

            cursor.GotoNext(next => next.MatchLdcR4(180f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, typeof(Entity).GetMethod("get_Scene"));
            cursor.Emit(OpCodes.Isinst, typeof(Level));
            cursor.EmitDelegate<Func<Level, float>>(GetCameraHeight);
        }

        private static void ReplaceLevelZoomTo(ILCursor cursor)
        {
            cursor.GotoNext(next => next.MatchCallvirt<Level>("ZoomTo"));
            cursor.Remove();
            cursor.EmitDelegate<Func<Level, Vector2, float, float, IEnumerator>>(
                (
                    level,
                    screen_space_location,
                    zoom_amount,
                    duration
                ) => level.ZoomToFocus(
                    (CameraFocusWrapper)CameraFocus.FromFocusPoint(
                        level.Camera.Position,
                        screen_space_location,
                        zoom_amount
                    ),
                    duration
                )
            );
        }

        private static void ReplaceLevelZoomBack(ILCursor cursor)
        {
            cursor.GotoNext(next => next.MatchCallvirt<Level>("ZoomBack"));
            cursor.Remove(); // Removing stuff sucks, but...
            cursor.EmitDelegate<Func<Level, float, IEnumerator>>(
                (
                    level,
                    duration
                ) => {
                    return level.ZoomBackFocus(duration);
                }
            );
        }

        private static void ReplaceLevelZoomSnap(ILCursor cursor)
        {
            cursor.GotoNext(next => next.MatchCallvirt<Level>("ZoomSnap"));
            cursor.Remove();
            cursor.EmitDelegate<Action<Level, Vector2, float>>(
                (
                    level,
                    screen_space_location,
                    zoom_amount
                ) => {
                    level.ForceCameraTo(
                        CameraFocus.FromFocusPoint(
                            level.Camera.Position,
                            screen_space_location,
                            zoom_amount
                        )
                    );
                }
            );
        }


        private static void FlingBird_DoFlingRoutine(ILContext il)
        {
            //FlingBird
            ILCursor cursor = new ILCursor(il);

            ReplaceNextFloat(cursor, 145f,
                () => 145f / CurrentLevelZoom()
            );
            ReplaceNextFloat(cursor, 215f,
                () => 215f / CurrentLevelZoom()
            );
            ReplaceNextFloat(cursor, 85f,
                () => 85f / CurrentLevelZoom()
            );
            ReplaceNextFloat(cursor, 95f,
                () => 95f / CurrentLevelZoom()
            );

            cursor.GotoNext(next => next.MatchLdcR4(1.1f));
            cursor.Index++;
            cursor.EmitDelegate<Func<float>>(CurrentLevelZoom);
            cursor.Emit(OpCodes.Mul);

            ReplaceLevelZoomTo(cursor);
            ReplaceLevelZoomBack(cursor);
        }

        private static void BadelineBoost_BoostRoutine(ILContext il)
        {
            //BadelineBoost
            ILCursor cursor = new ILCursor(il);

            ReplaceNextFloat(cursor, 200f,
                () => VisibleWidth() - 120f
            );
            ReplaceNextFloat(cursor, 120f,
                () => VisibleHeight() - 60f
            );

            ReplaceLevelZoomTo(cursor);
        }


        private static float _lookout_look_routine_center_camera_width(float normal_clamped)
        {
            Level level = (Engine.Scene as Level);
            if (level == null) return normal_clamped;
            float camera_width = VisibleWidth();
            if (camera_width > level.Bounds.Width)
            {
                return level.Bounds.Center.X - camera_width * 0.5f;
            }
            return normal_clamped;
        }
        private static float _lookout_look_routine_center_camera_height(float normal_clamped)
        {
            Level level = (Engine.Scene as Level);
            if (level == null) return normal_clamped;
            float camera_height = VisibleHeight();
            if (camera_height > level.Bounds.Height)
            {
                return level.Bounds.Center.Y - camera_height * 0.5f;
            }
            return normal_clamped;
        }
        private static void _lookout_look_routine_replace_zoom_snap(Level level,Vector2 _screen_space_location, float _zoom_amount)
        {
            level.ResetZoom();
        }
        private static void Lookout_LookRoutine(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            ReplaceNextFloat(cursor, 160f,
                () => VisibleWidth() * 0.5f    
            );
            ReplaceNextFloat(cursor, 90f,
                () => VisibleHeight() * 0.5f
            );

            // if (nodes == null)
            {
                // resets X speed if hits the side
                ReplaceNextFloat(cursor, 320f, VisibleWidth);
                // subtract from level bounds to clamp
                ReplaceNextInt(cursor, 320, () => (int)Math.Ceiling(VisibleWidth()));
                cursor.Index += 3;
                // if the camera is wider than the room, center the camera.
                // otherwise, use the clamped value.
                cursor.EmitDelegate<Func<float, float>>(_lookout_look_routine_center_camera_width);
                // test if the camera would overlap a LookoutBlocker
                ReplaceNextFloat(cursor, 320f, VisibleWidth);
                ReplaceNextFloat(cursor, 180f, VisibleHeight);


                // resets Y speed if hits the top/bottom
                ReplaceNextFloat(cursor, 180f, VisibleHeight);
                // subtract from level bounds to clamp
                ReplaceNextInt(cursor, 180, () => (int)Math.Ceiling(VisibleHeight()));
                cursor.Index += 3;
                // if the camera is taller than the room, center the camera.
                // otherwise, use the clamped value.
                cursor.EmitDelegate<Func<float, float>>(_lookout_look_routine_center_camera_height);
                // test if the camera would overlap a LookoutBlocker
                ReplaceNextFloat(cursor, 320f, VisibleWidth);
                ReplaceNextFloat(cursor, 180f, VisibleHeight);
            }
            // I was originally going to add additional function to the pathed version as well...
            // Instead, that function was moved to a custom watchtower class: ReferenceFrameLookout
            // Use that!
            cursor.GotoNext(next => next.MatchCallvirt<Level>("ZoomSnap"));
            cursor.Remove();
            cursor.EmitDelegate<Action<Level, Vector2, float>>(_lookout_look_routine_replace_zoom_snap);
        }

        private static Vector2 cassette_collect_routine_retain_camera_position(Vector2 _lerped)
        {
            return (Engine.Scene as Level).Camera.Position;
        }
        private static void Cassette_CollectRoutine(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            ReplaceNextFloat(cursor, 160f, () => 80f);
            ReplaceNextFloat(cursor, 90f, () => 45f);

            ReplaceNextFloat(cursor, 260f, () => VisibleWidth() - 60f);
            ReplaceNextFloat(cursor, 120f, () => VisibleHeight() - 60f);

            ReplaceLevelZoomSnap(cursor);
            ReplaceLevelZoomBack(cursor);

            cursor.GotoNext(next => next.MatchCallvirt<Camera>("set_Position"));
            cursor.EmitDelegate<Func<Vector2, Vector2>>(cassette_collect_routine_retain_camera_position);

            ReplaceNextFloat(cursor, 160f, () => 160f / CurrentLevelZoom());
            ReplaceNextFloat(cursor, 90f, () => 90f / CurrentLevelZoom());
        }


        private static void BirdTutorialGui_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ReplaceNextFloat(cursor, 320f, () => VisibleWidth());
            ReplaceNextFloat(cursor, 6f, () => 6f * CurrentLevelZoom());
            ReplaceNextFloat(cursor, 6f, () => 6f * CurrentLevelZoom());
        }

        private static void _angry_oshiro_repeat_renders(Image lightning, Level level)
        {
            int repeats = (int)Math.Ceiling(1f / level.Zoom) - 1;
            for (int i = 0; i < repeats; i++)
            {
                lightning.RenderPosition += new Vector2(lightning.Width, 0f);
                lightning.Render();
            }
        }
        private static void AngryOshiro_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCallvirt<Component>("Render"));
            cursor.Index++;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(AngryOshiro).GetField("lightning", BindingFlags.Instance | BindingFlags.NonPublic));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(AngryOshiro).GetField("level", BindingFlags.Instance | BindingFlags.NonPublic));
            cursor.EmitDelegate<Action<Image, Level>>(_angry_oshiro_repeat_renders);
        }

        private static void MemorialText_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            ReplaceNextFloat(cursor, 6f, () => 6f * CurrentLevelZoom());
            ReplaceNextFloat(cursor, 6f, () => 6f * CurrentLevelZoom());
            ReplaceNextFloat(cursor, 350f, () => 350f * CurrentLevelZoom());
        }

        // Add 'focus_camera' dialog keyword:
        // { focus_camera easy_key over_duration }
        private class TextInducedZoom : FancyText.Node
        {
            public string EasyKey;
            public float Duration;
            public bool TriggerThis()
            {
                ExCameraCommands.ZoomToCameraReferenceFrame(EasyKey, Duration);
                return true;
            }

        }

        // insert the zoom command into the Text nodes...
        private static void _fancyText_parse_insert_zoom_node(FancyText.Text group, List<string> parameters)
        {
            string EasyKey = parameters[0];
            float Duration = (parameters.Count > 1 && float.TryParse(parameters[1], out float duration)) ? duration : 1f;
            group.Nodes.Add(new TextInducedZoom() { EasyKey = EasyKey, Duration = Duration });
        }
        private static void FancyText_Parse(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // get into position
            cursor.GotoNext(next => next.MatchLdstr("/>>"));

            // because we want to call a continue within our 'if', we need to get the jump locations
            //   for both our 'if' statement and the 'continue' inside it.
            cursor.Index--;
            ILLabel no_match = cursor.DefineLabel();
            cursor.MarkLabel(no_match);
            no_match.Target = cursor.Next; // jump to the comparison against "/>>" if our if statement fails
            ILLabel continue_after = cursor.DefineLabel();
            cursor.Index += 3;
            continue_after.Target = (cursor.Next.Operand as ILLabel).Target; //jump to the same place as "/>>"'s continue to effectively use continue

            // reset before testing against "/>>"
            cursor.Index -= 3;
            // if (text == "focus_camera" && list.Count >= 1)
            cursor.Emit(OpCodes.Ldloc, 7); // text
            cursor.Emit(OpCodes.Ldloc, 8); // list
            cursor.EmitDelegate<Func<string, List<string>, bool>>((text, parameters) => (text == "focus_camera" && parameters.Count >= 1));
            cursor.Emit(OpCodes.Brfalse_S, no_match);

            // {

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(FancyText).GetField("group", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)); // group
            cursor.Emit(OpCodes.Ldloc, 8); // list
            cursor.EmitDelegate<Action<FancyText.Text, List<string>>>(_fancyText_parse_insert_zoom_node);
            cursor.Emit(OpCodes.Br, continue_after); // continue;

            // }
        }

        private static bool _textbox_runroutine_test_and_run_zoom(FancyText.Node current) => (current as TextInducedZoom)?.TriggerThis() ?? false;
        // actually run the zoom when a textbox asks for it.
        private static void Textbox_RunRoutine(ILContext il)
        {

            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchIsinst<FancyText.Anchor>());
            cursor.Index -= 2;
            ILLabel not_inst = cursor.DefineLabel();
            cursor.MarkLabel(not_inst); // jump to the next if statement if this one fails
            cursor.Index += 8;
            ILLabel is_inst = cursor.DefineLabel(); // jump to the end of the if-else blocks if this one succeeds
            is_inst.Target = (cursor.Next.Operand as ILLabel).Target;
            cursor.Index -= 8;

            cursor.Emit(OpCodes.Ldarg_0); // routine object
            cursor.Emit(OpCodes.Ldfld, typeof(Textbox).GetNestedTypes(BindingFlags.NonPublic)[0].GetRuntimeFields().Take(2).Last()); // current 'local var'
            cursor.EmitDelegate<Func<FancyText.Node, bool>>(_textbox_runroutine_test_and_run_zoom);
            cursor.Emit(OpCodes.Brfalse_S, not_inst);
            cursor.Emit(OpCodes.Br, is_inst);
            
            // yes, this is a branching IL hook within a coroutine.
        }
    }
}
