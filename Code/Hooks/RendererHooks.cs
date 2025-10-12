using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Linq;
namespace Celeste.Mod.ExCameraDynamics.Code.Hooks
{
    public static partial class CameraZoomHooks
    {
        public static event Action<int, int> OnBufferCreation;
        public static event Action<int, int> OnBufferResize;
        public static void GameplayBuffers_Create(On.Celeste.GameplayBuffers.orig_Create orig)
        {
            orig();

            // I could overwrite this method, or I could do this and recreate the buffers twice like a boss >:)
            ResizeVanillaBuffers(ZoomTarget);

            // Invoke the event
            OnBufferCreation?.Invoke(BufferWidthOverride, BufferHeightOverride);
        }
        public static void ResizeVanillaBuffers(float zoomTarget)
        {
            // Set the zoom to resize to
            current_buffer_zoom = MathF.Min(1f, zoomTarget);

            // Find the dimensions
            //BufferWidthOverride = /*1 + */(current_buffer_zoom >= 1f ? 320 : (int)Math.Min(MaxBufferWidth, Math.Ceiling(320f / current_buffer_zoom)));
            if (BoundBufferSize)
            {
                BufferWidthOverride = /*1 + */(current_buffer_zoom >= 1f ? 320 : (int)Math.Min(MaxBufferWidth, Math.Ceiling(320f / current_buffer_zoom)));
                BufferHeightOverride = /*1 + */(current_buffer_zoom >= 1f ? 180 : (int)Math.Min(MaxBufferHeight, Math.Ceiling(180f / current_buffer_zoom)));
            }
            else
            {
                BufferWidthOverride = /*1 + */(current_buffer_zoom >= 1f ? 320 : (int)Math.Ceiling(320f / current_buffer_zoom));
                BufferHeightOverride = /* 1 +*/ (current_buffer_zoom >= 1f ? 180 : (int)Math.Ceiling(180f / current_buffer_zoom));
            }


            // resize all relevant vanilla buffers
            ResizeBufferToZoom(GameplayBuffers.Gameplay);
            ResizeBufferToZoom(GameplayBuffers.Level);
            ResizeBufferToZoom(GameplayBuffers.ResortDust);
            ResizeBufferToZoom(GameplayBuffers.Light);
            ResizeBufferToZoom(GameplayBuffers.Displacement);
            ResizeBufferToZoom(GameplayBuffers.TempA);
            ResizeBufferToZoom(GameplayBuffers.TempB);

            // The buffers for BlackholeBGs are created per instance.
            // There should only ever be one at a time, but better safe than sorry.
            // It should also only ever be in the background.
            if (((Engine.Scene as Level)?.Background?.Backdrops?.Any(backdrop => backdrop is BlackholeBG) ?? false)
                ||
                ((Engine.Scene as Level)?.Foreground?.Backdrops?.Any(backdrop => backdrop is BlackholeBG) ?? false)
                )
            {
                foreach (VirtualAsset asset in VirtualContent.Assets.Where(asset => asset.Name == "Black Hole" && asset is VirtualRenderTarget))
                {
                    ResizeBufferToZoom((VirtualRenderTarget)asset);
                }
            }

            // Invoke the event
            OnBufferResize?.Invoke(BufferWidthOverride, BufferHeightOverride);
        }

        public static void ResizeBufferToZoom(VirtualRenderTarget target)
        {
            if (target == null) return;

            if (target.Width == BufferWidthOverride && target.Height == BufferHeightOverride) return; // don't resize what doesn't need to be resized.

            target.Width = BufferWidthOverride;
            target.Height = BufferHeightOverride;
            target.Reload();
        }
        // fix the borders of the dust bunnies, which need the UV-space of one pixel.
        public static void DustEdges_BeforeRender(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(start => CalcPlus.MatchesSequence(
                start,
                next => next.OpCode == OpCodes.Ldsfld,
                next => next.OpCode == OpCodes.Callvirt,
                next => next.MatchLdstr("noiseFromPos"),
                next => next.OpCode == OpCodes.Callvirt,
                next => next.OpCode == OpCodes.Ldarg_0,
                next => next.MatchLdfld<DustEdges>("noiseFromPos"),
                next => next.OpCode == OpCodes.Ldloc_1,
                next => next.MatchLdfld<Vector2>("X"),
                next => next.MatchLdcR4(320)
            )); // I love excessive IL matching protocols. It's great.

            cursor.ReplaceNextFloat( 320f, GetBufferWidth);
            cursor.ReplaceNextFloat( 180f, GetBufferHeight);
            cursor.ReplaceNextFloat( 320f, GetBufferWidth);
            cursor.ReplaceNextFloat( 180f, GetBufferHeight);

            // I love how this is just a constant in the IL code- It's almost certainly (1f / GameWidth) in the source and then evaluated at compile-time,
            // but the random 0.090035209423427394206463405t863408 is just
            // it's great
            cursor.ReplaceNextFloat( 0.003125f, GetPixelU);
            cursor.ReplaceNextFloat( 0.0055555557f, GetPixelV);
        }

        public static float GetPixelU()
        {
            return 1f / BufferWidthOverride;
        }
        public static float GetPixelV()
        {
            return 1f / BufferHeightOverride;
        }

        // lights outside the camera are culled, which needs to account for the zoom now.
        public static void LightingRenderer_BeforeRender(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.ReplaceNextFloat( 320f, GetBufferWidth);
            cursor.ReplaceNextFloat( 180f, GetBufferHeight);
        }

        // Lightning Bloom needs to be adjusted for the entire screen
        private static void LightningRenderer_RenderBloom(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.ReplaceNextFloat( 320f, GetBufferWidth);
            cursor.ReplaceNextFloat( 180f, GetBufferHeight);
        }


        // reduce the effect of the distortion filter, as it's effects are UV-based, not pixel based.
        // (as pixels shrink, we need to reduce its effect to match :P)
        private static void _displacement_renderer_reduce_with_zoom(Scene scene)
        {
            Level level = scene as Level;
            if (level == null) return;
            // this is drawing a rectangle over the displacement buffer with an opacity that scales with the zoom.
            // this should make it stay somewhat pixel-consistent.
            Draw.Rect((float)Math.Floor(level.Camera.X) - 10f, (float)Math.Floor(level.Camera.Y) - 10f, BufferWidthOverride + 20f, BufferHeightOverride + 20f, new Color(0.5f, 0.5f, 0f) * (1f - (320f / BufferWidthOverride)));
        }
        private static void DisplacementRenderer_BeforeRender(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            // the water distortion is modified here of all places.
            cursor.EmitDelegate<Action>(() => { Distort.WaterAlpha = Math.Min(1f, current_buffer_zoom); }); // scales with buffer size, since it's UV-based, too...
            cursor.GotoNext(next => next.MatchCallvirt<SpriteBatch>("End"));
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<Scene>>(_displacement_renderer_reduce_with_zoom);
        }
        // The Bloom renderer draws a rectangle of (-10,-10, width + 20, height + 20), which needs to be adjusted.
        public static void BloomRenderer_Apply(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchLdcR4(340f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Func<Scene, float>>(scene => 20f + GetCameraWidth(scene as Level));
            /*{
                Level level = scene as Level;
                if (level == null) return 340f;
                return 20f + Math.Min(BufferWidthOverride, 320f / level.Zoom);
            });*/
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Func<Scene, float>>(scene => 20f + GetCameraHeight(scene as Level));
            /*{
                Level level = scene as Level;
                if (level == null) return 200f;
                return 20f + Math.Min(BufferHeightOverride, 180f / level.Zoom);
            });
            */
        }
    }
}
