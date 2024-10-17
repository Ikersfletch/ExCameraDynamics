using Celeste.Mod.ExCameraDynamics.Code.Triggers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.ExCameraDynamics.Code.Hooks
{
    public static partial class CameraZoomHooks
    {
        // no extra local variables + DynamicData is annoying (and inconsistent in my experience) -> using global vars. ~lovely!~
        private static float _mid_transition_starting_zoom = 1f;
        private static float _mid_transition_target_zoom = 1f;
        private static void _set_start_transition_zoom(Level level, Player player, Vector2 playerTo, List<Entity> toRemove)
        {
            // the current zoom to start transitioning from
            _mid_transition_starting_zoom = level.Zoom;
            // the zoom to eventually transition to
            _mid_transition_target_zoom = RestingZoomFactor;
            // trigger zoom override is the value of `force_zoom` command
            if (TriggerZoomOverride > 0f)
            {
                _mid_transition_target_zoom = TriggerZoomOverride;
            }
            else
            {
                Vector2 playerPos = player.Position;
                player.Position = playerTo;
                // find the target zoom for the end of the transition
                foreach (CameraZoomTrigger item in player.CollideAll<CameraZoomTrigger>())
                {
                    if (!toRemove.Contains(item) && item.ZoomBoundary == CameraZoomTrigger.Boundary.SetsNearest)
                    {
                        _mid_transition_target_zoom = item.GetZoom(playerTo);
                        break;
                    }
                }
                player.Position = playerPos;
                trigger_zoom_target = _mid_transition_target_zoom;
            }
            level.Zoom = _mid_transition_target_zoom;
        }
        private static void _reset_start_transition_zoom(Level level)
        {
            level.Zoom = _mid_transition_starting_zoom;
        }

        private static void _snap_end_transition_camera(Level level)
        {
            level.Zoom = _mid_transition_target_zoom;
            level.ZoomTarget = _mid_transition_target_zoom;
            level.ZoomFocusPoint = new Vector2(160, 90) / _mid_transition_target_zoom;
            level.Camera.Viewport.Width = (int)Math.Ceiling(320f / _mid_transition_target_zoom);
            level.Camera.Viewport.Height = (int)Math.Ceiling(180f / _mid_transition_target_zoom);
        }

        private static float _interp_mid_transition_camera(float t, Level level)
        {
            /*** let corners of camera be points px0, px1, s.t.:
              px0 = sx0 * (1f - t) + ex0 * t,
              px1 = sx1 * (1f - t) + ex1 * t,
              and px1 > px0

             *** let the width of the camera be dif:
              dif = px1 - px0 
              dif = (sx0 * (1f - t) + ex0 * t) - (sx1 * (1f - t) + ex1 * t)
              dif = (sx0 - sx1) * (1f - t) + (ex0 - ex1) * t

             *** let the zoom factor we want be neededZoom:
              320 / zoom = dif;

              320 / neededZoom = (sx0 - sx1) * (1f - t) + (ex0 - ex1) * t

             *** let endZoom, startZoom be zoom factors s.t.:
              ex1 = ex0 + 320 / endZoom
              sx1 = sx0 + 320 / startZoom

             *** substitute ex1, sx1
              320 / neededZoom = (sx0 - (sx0 + 320 / startZoom)) * (1f - t) + (ex0 - (ex0 + 320 / endZoom)) * t
              320 / neededZoom = (-320 / startZoom) * (1f - t) + (-320 / endZoom) * t
              320 / neededZoom = -320 * ( (1f - t) / startZoom + t / endZoom)
              1 / neededZoom = ( (1f - t) / startZoom + t / endZoom)

              neededZoom = 1 / ( (1f - t) / startZoom + t / endZoom)

             *** That's a pretty natural feeling function- it just takes the inverse of the linear interpolation of the inverses.
             ...of course it's that. why didn't I just think of that. 
             oh well!

             *** but let's simplify to reduce divisions:
              neededZoom = 1 / ( ( endZoom * (1f - t) + startZoom * t ) / ( startZoom * endZoom ) )
              neededZoom = ( startZoom * endZoom ) / ( endZoom * (1f - t) + startZoom * t )

             *** booyah
              neededZoom = ( startZoom * endZoom ) / ( endZoom + t * ( startZoom - endZoom ) )
            */


            // if camera hooks exist, then this always runs. It's a small interpolation, so I don't care.
            // one interesting note is how if the zoom remains constant, this simplifies to currentZoom, which is intended behavior! Perfect!
            float zoomInterpolate = (_mid_transition_starting_zoom * _mid_transition_target_zoom) / (_mid_transition_target_zoom + t * (_mid_transition_starting_zoom - _mid_transition_target_zoom));
            level.Zoom = zoomInterpolate;
            level.ZoomTarget = zoomInterpolate;
            level.ZoomFocusPoint = new Vector2(160, 90) / zoomInterpolate;
            level.Camera.Viewport.Width = (int)Math.Ceiling(320f / zoomInterpolate);
            level.Camera.Viewport.Height = (int)Math.Ceiling(180f / zoomInterpolate);
            return t;
        }

        private static void Level_TransitionRoutine(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            Type TransitionRoutineType = typeof(Level).GetNestedTypes(BindingFlags.NonPublic)[2];

            // set to the target zoom before the game calls GetFullCameraTarget()
            // so that the method can properly calculate where the post-zoom camera should be.
            cursor.GotoNext(next => next.MatchStfld(TransitionRoutineType.GetRuntimeFields().Take(13).Last()));
            cursor.Index -= 7;
            cursor.Emit(OpCodes.Ldloc_1); // Level
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, TransitionRoutineType.GetRuntimeFields().Take(7).Last()); // player - the player the routine manipulates
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, TransitionRoutineType.GetRuntimeFields().Take(12).Last()); // playerTo - the location the player is going to be post-transition
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, TransitionRoutineType.GetRuntimeFields().Take(8).Last()); // toRemove - all of the entities that will be despawned post-transition
            cursor.EmitDelegate(_set_start_transition_zoom);
            // set back to the starting zoom- we only changed it so that the position is correctly calculated.
            cursor.Index += 8;
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate(_reset_start_transition_zoom);

            // snap the camera to the correct size at the end of the zoom
            cursor.GotoNext(next => next.MatchCallvirt<Camera>("set_Position"));
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.EmitDelegate(_snap_end_transition_camera);

            cursor.Index += 2; // <- otherwise we'd match the same set_Position call

            // interpolate the camera's size during the zoom
            cursor.GotoNext(next => next.MatchCallvirt<Camera>("set_Position"));
            cursor.Index--;
            cursor.Emit(OpCodes.Ldloc_1);
            // the float argument is the interpolation percent, post-easer.
            // it's the input to Vector2.Lerp(_, _, t);
            // we return it at the end of this delegate so we can share it with its original use.
            cursor.EmitDelegate(_interp_mid_transition_camera);
        }
    }
}
