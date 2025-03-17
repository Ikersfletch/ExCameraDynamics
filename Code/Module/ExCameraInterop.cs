using Celeste.Mod.ExCameraDynamics.Code.Components;
using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.ExCameraDynamics.Code.Triggers;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;
using System;
using System.Collections;
using System.Collections.Generic;
namespace Celeste.Mod.ExCameraDynamics.Code.Module
{
    [ModExportName("ExtendedCameraDynamics")]
    public static class ExCameraInterop
    {

        /// <returns> Whether the extended camera hooks are currently applied. </returns>
        public static bool ExtendedCameraHooksEnabled() => CameraZoomHooks.HooksEnabled;

        /// <returns> The current width, in pixels, of the general render buffers. </returns>
        public static int BufferWidthOverride() => CameraZoomHooks.BufferWidthOverride;

        /// <returns> The current height, in pixels, of the general render buffers. </returns>
        public static int BufferHeightOverride() => CameraZoomHooks.BufferHeightOverride;

        /// <returns> The current dimensions of the visible camera.</returns>
        public static Vector2 GetCameraDimensions(Level level) => CameraZoomHooks.GetCameraDimensions(level);

        /// <summary>
        /// Resizes the given buffer to the size of the general render buffers. <br></br>
        /// This will only resize the target if a resize is called for. <br></br>
        /// I suggest calling this in a relevant 'BeforeRender' hook if you're using a custom <see cref="VirtualRenderTarget"/> that needs to fill the 320x180 screen. <br></br>
        /// </summary>
        public static void ResizeVirtualRenderTarget(VirtualRenderTarget target)
        {
            if (target == null) throw new ArgumentNullException("ResizeVirtualRenderTarget does not accept a null parameter!");

            CameraZoomHooks.ResizeBufferToZoom(target);
        }

        public static void SetRenderVerticalMirroring(bool flipped) => CameraZoomHooks.VerticalMirroring = flipped;

        /// <summary>
        /// A drop-in replacement for <see cref="Level.ZoomBack(float)"/> <br></br>
        /// Replacing the original method via hooking is annoying- IEnumerator methods compile very peculiarly. <br></br>
        /// Just use this to make all our lives easier :)
        /// </summary>
        public static IEnumerator Level_ZoomBack(Level level, float duration) => CameraZoomHooks.HooksEnabled ? CameraZoomHooks.ZoomBackFocus(level, duration) : level.ZoomBack(duration);

        /// <summary>
        /// A near-replacement for <see cref="Level.ZoomTo(Vector2, float, float)"/>. <br></br>
        /// The camera centers itself over <paramref name="worldFocusPoint"/>.
        /// </summary>
        /// <param name="worldFocusPoint">The position in world space to center the camer over.</param>
        /// <returns></returns>
        public static IEnumerator Level_ZoomToFocus(Level level, Vector2 worldFocusPoint, float zoom, float duration) => CameraZoomHooks.ZoomToFocus(level, (CameraFocusWrapper)CameraFocus.FromCenter(worldFocusPoint, zoom), duration);

        /// <summary> <see cref="CameraReferenceFrame"/>s are placed in loenn to easily get camera positions for cutscenes and the like. You can get them here. </summary>
        public static Entity Get_CameraReferenceFrame(Level level, string easyKey) => CameraReferenceFrame.GetFromEasyKey(level, easyKey);
        /// <summary> Zooms to a <see cref="CameraReferenceFrame"/> over duration. </summary>
        public static IEnumerator Level_ZoomToCameraReferenceFrame(Level level, Entity cameraReferenceFrame, float duration) => CameraZoomHooks.ZoomToFocus(level, cameraReferenceFrame as ICameraFocusSource, duration);
        public static IEnumerator Level_ZoomToCameraFocus(Level level, object cameraFocus, float duration) => CameraZoomHooks.ZoomToFocus(level,(CameraFocusWrapper)((CameraFocus)cameraFocus), duration);
        public static void Level_ForceZoomToCameraFocus(Level level, object cameraFocus) => CameraZoomHooks.ForceCameraTo(level, (CameraFocus)cameraFocus);

        /// <returns> The zoom evaluated from <see cref="CameraZoomTrigger"/>s at <paramref name="worldPoint"/> </returns>
        public static float Level_GetTriggerZoomAt(Level level, Vector2 worldPoint) => CalcPlus.GetTriggerZoomAtPosition(level, worldPoint);
        /// <summary>
        /// The game will try to keep all <see cref="CameraFocusTarget"/>s on screen. <br></br>
        /// For reference, the player has a weight of 1. <br></br>
        /// If all targets cannot fit on the screen, then the game will try and zoom out as far as the triggers allow. <br></br>
        /// If it still can't fit them, it will prioritize the player above all else.
        /// </summary>
        /// <param name="entityOffset">The offset relative from the entity to focus on.</param>
        /// <returns> A newly constructed <see cref="CameraFocusTarget"/> component. </returns>
        public static Component Create_CameraFocusTarget(Vector2 entityOffset, float weight) => new CameraFocusTarget(entityOffset, weight);

        /// <returns> An entity's <see cref="CameraFocusTarget"/>. </returns>
        public static Component Get_CameraFocusTarget(Entity ent) => ent.Get<CameraFocusTarget>();

        /// <summary> Set a <see cref="CameraFocusTarget"/>'s offset </summary>
        public static void CameraFocusTarget_SetOffset(Component comp, Vector2 offset) => (comp as CameraFocusTarget)?.SetOffset(offset);

        /// <summary> Set a <see cref="CameraFocusTarget"/>'s weight </summary>
        public static void CameraFocusTarget_SetWeight(Component comp, float weight) => (comp as CameraFocusTarget)?.SetWeight(weight);

        /// <returns> All <see cref="CameraFocusTarget"/>s in the Level </returns>
        public static List<Component> Tracked_CameraFocusTarget(Level level) => level?.Tracker?.GetComponents<CameraFocusTarget>();
        
        public static Type Type_CameraFocusTarget() => typeof(CameraFocusTarget);

        /// <summary> Multiplies the camera's interpolation by a fixed amount. </summary>
        public static void SetSnappingSpeed(float speed) => CameraZoomHooks.SetSnappingSpeed(speed);

        public static object Create_CameraFocus_FromActiveCameraPos(Level level) => new CameraFocus() { Position = level.Camera.Position, Zoom = level.Zoom };
        public static object Create_CameraFocus(Vector2 world_center, float zoom_factor) => CameraFocus.FromCenter(world_center, zoom_factor);
        public static object CameraFocus_Lerp(object focus_a, object focus_b, float t) => ((CameraFocus)focus_a).Lerp((CameraFocus)focus_b, t);


        // Used by Dependency2. 
        // I am not explaining why this exists.
        // I am not explaining what it does.
        // It's not really here for a public API.
        // This is me implementing a probably divisive feature in a stupid and convoluted way. Oh well! 
        // Just know that it's probably not useful to you.
        public static Vector2 GetCameraInterpolation(Vector2 cameraPosition, Vector2 targetOffset, float t, Player player) => CameraZoomHooks.GetCameraInterpolation(cameraPosition, targetOffset, t, player);
    }
}
