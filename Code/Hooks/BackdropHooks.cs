using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Celeste.Mod.ExCameraDynamics.Code.Hooks
{
    public static partial class CameraZoomHooks
    {
        private static float CurrentLevelZoom() => (Engine.Scene as Level)?.Zoom ?? 1f;
        private static float VisibleWidth() => 320f / CurrentLevelZoom();
        private static float VisibleHeight() => 180f / CurrentLevelZoom();
        private static Vector2 VisibleDimensions() => new Vector2(320f, 180f) / CurrentLevelZoom();
        //// PARALLAX
        public static Vector2 GetParallaxZoomOffset()
        {
            return (new Vector2(-160f, -90f) + (0.5f * VisibleDimensions()));// * (Vector2.One - self.Scroll);
        }
        private static void Parallax_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            MethodInfo Vector2_op_Addition = typeof(Vector2).GetMethod("op_Addition");

            /*
            from this:

            Vector2 CameraPosition = (Level.Camera.Position + self.CameraOffset).Floor();
            Vector2 position = (self.Position - CameraPosition * self.Scroll).Floor();
            
            to this:

            Vector2 ZoomOffset = new Vector2(-160f + BufferWidthOverride * 0.5f, -90f + BufferHeightOverride * 0.5f);
            Vector2 CameraPositionIfZoomWas1f = (level.Camera.Position + self.CameraOffset + ZoomOffset).Floor(); // <- it's really to modify this
            Vector2 position = (self.Position - CameraPositionIfZoomWas1f * self.Scroll + ZoomOffset).Floor(); // but we need to account for it here, too.

            */

            // Correct for dynamic zoom; make the 'origin' of the camera be (-160,-90) from its center instead of its position
            // we do this so that this local variable stores the same value it would have if the camera was zoomed in at the same center position
            cursor.GotoNext(next => next.MatchStloc(0));
            cursor.Index--; // before the floor operation
            cursor.EmitDelegate<Func<Vector2>>(GetParallaxZoomOffset);
            cursor.Emit(OpCodes.Stloc_1); // store the ZoomOffset for later
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.Emit(OpCodes.Call, Vector2_op_Addition);

            // here's the actual 'position' of the parallax. because the 'camera offset' was altered, we need to account for that here.
            // luckily we add the same value again.
            cursor.GotoNext(next => next.MatchStloc(1));
            cursor.Index--; // before the Floor() operation.
            cursor.Emit(OpCodes.Ldloc_1); // ZoomOffset from earlier
            cursor.Emit(OpCodes.Call, Vector2_op_Addition);

            // now we need to modify the clipping rectangles:

            // width:
            cursor.GotoNext(next => next.MatchLdcR4(320f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_1); // ldarg1 = scene
            cursor.Emit(OpCodes.Isinst, typeof(Level)); // as Level
            cursor.EmitDelegate<Func<Level, float>>(GetCameraWidth); // this is not always equal to the buffer size- the buffer may be larger.

            // height:
            cursor.GotoNext(next => next.MatchLdcR4(180f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_1); // ldarg1 = scene
            cursor.Emit(OpCodes.Isinst, typeof(Level)); // as Level
            cursor.EmitDelegate<Func<Level, float>>(GetCameraHeight);
        }

        private static void Parallax_orig_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            MethodInfo Vector2_op_Addition = typeof(Vector2).GetMethod("op_Addition");

            // see Parallax_Render. It's identical.
            cursor.GotoNext(next => next.MatchStloc(0));
            cursor.Index--;
            cursor.EmitDelegate<Func<Vector2>>(GetParallaxZoomOffset);
            cursor.Emit(OpCodes.Stloc_1);
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.Emit(OpCodes.Call, Vector2_op_Addition);
            cursor.GotoNext(next => next.MatchStloc(1));
            cursor.Index--;
            cursor.Emit(OpCodes.Ldloc_1);
            cursor.Emit(OpCodes.Call, Vector2_op_Addition);

            // now we just modify the loop comparisons:
            // inner loop:
            cursor.GotoNext(next => next.MatchLdcR4(180f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_1); // ldarg1 = scene
            cursor.Emit(OpCodes.Isinst, typeof(Level));
            cursor.EmitDelegate<Func<Level, float>>(GetCameraHeight);

            // outer loop:
            cursor.GotoNext(next => next.MatchLdcR4(320f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_1); // ldarg1 = scene
            cursor.Emit(OpCodes.Isinst, typeof(Level));
            cursor.EmitDelegate<Func<Level, float>>(GetCameraWidth);
        }

        private static void _starfield_repeat_renders(MTexture texture, Vector2 position, Color color, Starfield self)
        {
            position = position + (-new Vector2(160f, 90f) + new Vector2(VisibleWidth() * 0.5f, VisibleHeight() * 0.5f)) * (Vector2.One - self.Scroll);
            position.X = mod(position.X, 448f) - 64f;
            position.Y = mod(position.Y, 212f) - 16f;

            int repeatRenderX = (int)Math.Ceiling(VisibleWidth() / 448f);
            int repeatRenderY = (int)Math.Ceiling(VisibleHeight() / 212f);
            if (position.X < 64)
            {
                repeatRenderX++;
            }
            if (position.Y < 16)
            {
                repeatRenderY++;
            }
            for (int x = 0; x < repeatRenderX; x++)
            {
                for (int y = 0; y < repeatRenderY; y++)
                {
                    texture.DrawCentered(position + new Vector2(x * 448f, y * 212f), color);
                }
            }
        }
        private static void Starfield_Render(ILContext il)
        {
            //Starfield
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCallvirt(typeof(MTexture).GetMethod("DrawCentered", new Type[] { typeof(Vector2), typeof(Color) })));
            cursor.Remove();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<MTexture, Vector2, Color, Starfield>>(_starfield_repeat_renders);
        }

        //// BLACKHOLE
        private static void BlackholeBG_BeforeRender(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchLdcI4(320));
            cursor.PopNext();
            cursor.EmitDelegate<Func<int>>(GetVisibleCameraWidth);

            cursor.GotoNext(next => next.MatchLdcI4(180));
            cursor.PopNext();
            cursor.EmitDelegate<Func<int>>(GetVisibleCameraHeight);
        }
        private static void BlackholeBG_Update(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchLdcR4(320f));

            cursor.PopNext(); // remove 320f
            cursor.EmitDelegate<Func<float>>(VisibleWidth);

            cursor.PopNext(); // remove 180f
            cursor.EmitDelegate<Func<float>>(VisibleHeight);
        }

        private static void _rain_fg_repeat_renders(MTexture texture, Vector2 position, Color color, Vector2 scale, float rotation, Scene scene)
        {
            Level level = scene as Level;

            if ((level?.Zoom ?? 3.0f) >= 1f)
            {
                texture.DrawCentered(position, color, scale, rotation);
                return;
            }

            int loops = (int)Math.Max(Math.Ceiling(level.Camera.Viewport.Width / 384f), Math.Ceiling(level.Camera.Viewport.Height / 244f));

            for (int x = 0; x < loops; x++)
            {
                for (int y = 0; y < loops; y++)
                {
                    texture.DrawCentered(position + new Vector2(384 * x, 244f * y), color, scale, rotation);
                }
            }
        }
        private static void RainFG_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchStloc(2)); // initialize i
            cursor.GotoNext(next => next.MatchStloc(2)); // increment i
            cursor.Index -= 4;
            cursor.Remove(); // AAAHH I'm removing the DrawCentered call and replacing it with this!!! wahaha
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<MTexture, Vector2, Color, Vector2, float, Scene>>(_rain_fg_repeat_renders);
        }

        private static void RainFG_Update(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchLdcR4(160f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldarg_1); // ldarg1 = scene
            cursor.Emit(OpCodes.Isinst, typeof(Level));
            cursor.EmitDelegate<Func<Level, float>>(GetHalfCameraWidth);
        }

        // TODO: Make this an IL hook
        private static void StarsBG_Render(On.Celeste.StarsBG.orig_Render orig, StarsBG self, Scene scene)
        {
            float zoom = (scene as Level)?.Zoom ?? 1f;

            if (zoom >= 1f)
            {
                orig(self, scene);
                return;
            }

            Draw.Rect(0f, 0f, BufferWidthOverride, BufferHeightOverride, Color.Black);
            Level level = scene as Level;
            Color color = Color.White;
            int starCount = 100;
            if (level.Session.Dreaming)
            {
                color = Color.Teal * 0.7f;
            }
            else
            {
                starCount /= 2;
            }

            List<List<MTexture>> textures = self.textures;
            StarsBG.Star[] stars = self.stars;
            Color[] colors = self.colors;
            Vector2 center = self.center;
            float falling = self.falling;

            int vanillaScreens = (int)Math.Ceiling(1f / zoom);


            for (int i = 0; i < starCount; i++)
            {
                List<MTexture> list = textures[stars[i].TextureSet];
                int textureIndesx = (int)((Math.Sin(stars[i].Timer) + 1.0) / 2.0 * (double)list.Count);
                textureIndesx %= list.Count;
                Vector2 position = stars[i].Position;
                MTexture chosenTexture = list[textureIndesx];


                Vector2 renderPos;

                if (level.Session.Dreaming)
                {
                    position.Y -= level.Camera.Y - VisibleHeight() * 0.5f;
                    position.Y += falling * stars[i].Rate;
                    position.Y %= 180f;
                    if (position.Y < 0f)
                    {
                        position.Y += 180f;
                    }

                    renderPos = position - new Vector2(160f, 90f) + VisibleDimensions() * 0.5f;
                    renderPos.X = mod(renderPos.X, 320f); // I would add extra buffer to avoid pop-in on the edges, but that's just vanilla behavior- so I'm not going to do that!
                    renderPos.Y = mod(renderPos.Y, 180f); // see Godray_Update() if you want to see such a buffer.

                    for (int j = 0; j < colors.Length; j++)
                    {
                        for (int x = 0; x < vanillaScreens; x++) // this is the whole reason. I don't want to do for loops with IL. I could (probably), but that would be... not worth it.
                            for (int y = 0; y < vanillaScreens; y++)
                                chosenTexture.Draw(renderPos - Vector2.UnitY * j + new Vector2(320f * x, 180f * y), center, colors[j]);
                    }
                }
                else
                {
                    renderPos = position - new Vector2(160f, 90f) + VisibleDimensions() * 0.5f;
                    renderPos.X = mod(renderPos.X, 320f);
                    renderPos.Y = mod(renderPos.Y, 180f);
                }

                for (int x = 0; x < vanillaScreens; x++)
                    for (int y = 0; y < vanillaScreens; y++)
                        chosenTexture.Draw(renderPos + new Vector2(320f * x, 180f * y), center, color);
            }

        }

        // TODO: Make this an IL hook. It's totally doable.
        private static void WindSnowFG_Render(On.Celeste.WindSnowFG.orig_Render orig, WindSnowFG self, Scene scene)
        {
            float zoom = (scene as Level)?.Zoom ?? 1f;

            if (zoom >= 1f)
            {
                orig(self, scene);
                return;
            }

            if (self.Alpha <= 0f)
            {
                return;
            }


            float visibleFade = self.visibleFade;
            float loopHeight = self.loopHeight;
            float loopWidth = self.loopWidth;
            Vector2 scale = self.scale;
            float rotation = self.rotation;

            int repeat_num = (int)Math.Max(Math.Ceiling(VisibleWidth() / loopWidth), Math.Ceiling(VisibleHeight() / loopHeight));

            Color color = self.Color * visibleFade * self.Alpha;
            int draw_limit = (int)(((scene as Level).Wind.Y == 0f) ? ((float)self.positions.Length) : ((float)self.positions.Length * 0.6f));
            int total_drawn = 0;
            Vector2[] positions = self.positions;
            for (int i = 0; i < positions.Length; i++)
            {
                Vector2 position = positions[i];
                position.Y -= (scene as Level).Camera.Y;// + self.CameraOffset.Y - 160f;
                position.Y %= loopHeight;
                if (position.Y < 0f)
                {
                    position.Y += loopHeight;
                }

                position.X -= (scene as Level).Camera.X;// + self.CameraOffset.X - 180f;
                position.X %= loopWidth;
                if (position.X < 0f)
                {
                    position.X += loopWidth;
                }

                if (total_drawn < draw_limit)
                {
                    for (int x = 0; x < repeat_num; x++)
                        for (int y = 0; y < repeat_num; y++)
                            GFX.Game["particles/snow"].DrawCentered(position + new Vector2(loopWidth * x, loopHeight * y), color, scale, rotation);
                }

                total_drawn++;
            }
        }
        private static void Snow_Render(On.Celeste.Snow.orig_Render orig, Snow self, Scene scene)
        {
            float zoom = (scene as Level)?.Zoom ?? 1f;

            if (zoom >= 1f)
            {
                orig(self, scene);
                return;
            }

            float visibleFade = self.visibleFade;
            float linearFade = self.linearFade;

            if (self.Alpha <= 0f || visibleFade <= 0f || linearFade <= 0f) return;

            int vanillaScreens = (int)Math.Ceiling(1f / zoom);

            Color[] colors = self.colors;
            Color[] blendedColors = self.blendedColors;
            Snow.Particle[] particles = self.particles;


            for (int i = 0; i < blendedColors.Length; i++)
            {
                blendedColors[i] = colors[i] * (self.Alpha * visibleFade * linearFade);
            }

            Camera camera = (scene as Level).Camera;
            for (int j = 0; j < particles.Length; j++)
            {
                Vector2 position = particles[j].Position;
                position.X -= camera.X;
                position.X %= 320f;
                if (position.X < 0f)
                {
                    position.X += 320f;
                }
                position.Y -= camera.Y;
                position.Y %= 180f;
                if (position.Y < 0f)
                {
                    position.Y += 180f;
                }
                //Vector2 position = new Vector2(mod(particles[j].Position.X - camera.X - BufferWidthOverride * 0.5f, 320f), mod(particles[j].Position.Y - camera.Y - BufferHeightOverride * 0.5f, 180f));
                Color color = blendedColors[particles[j].Color];

                // we don't need any extra room for the snowflakes because they're opaque 1x1 pixels- there's no overlap and overdraw would have no visible difference
                if (position.X >= 0 && position.Y >= 0)
                    for (int x = 0; x < vanillaScreens; x++)
                        for (int y = 0; y < vanillaScreens; y++)
                            Draw.Pixel.DrawCentered(position + new Vector2(320f * x, 180f * y), color);
            }

        }

        private static void _final_boss_starfield_repeat_streaks(VertexPositionColor[] verts, int vertex_count)
        {
            // the transblack background...
            GFX.DrawVertices(Matrix.Identity, verts, 6);
            // we need to do this to prevent clipping, as otherwise the transblack background would get drawn several times over...
            VertexPositionColor[] repeats = verts.AsSpan(6).ToArray();
            int render_repeats = (int)Math.Max(Math.Ceiling(VisibleWidth() / 320f), Math.Ceiling(VisibleHeight() / 180f));
            for (int x = 0; x < render_repeats; x++)
            {
                for (int y = 0; y < render_repeats; y++)
                {
                    GFX.DrawVertices(Matrix.CreateTranslation(x * 320f, y * 180f, 0.0f), repeats, vertex_count);
                }
            }
        }
        // The geometry loops itself over the camera.
        private static void FinalBossStarfield_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // alter the coordinates of the transblack rectangle...
            cursor.GotoNext(next => next.MatchLdcR4(330f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => BufferWidthOverride + 10);

            cursor.GotoNext(next => next.MatchLdcR4(330f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => BufferWidthOverride + 10);

            cursor.GotoNext(next => next.MatchLdcR4(190f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => BufferHeightOverride + 10);

            cursor.GotoNext(next => next.MatchLdcR4(330f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => BufferWidthOverride + 10);

            cursor.GotoNext(next => next.MatchLdcR4(190f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => BufferHeightOverride + 10);

            cursor.GotoNext(next => next.MatchLdcR4(190f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => BufferHeightOverride + 10);

            // modify when the particles begin repeating...
            cursor.GotoNext(next => next.MatchLdcR4(384f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldc_R4, 320f);

            cursor.GotoNext(next => next.MatchLdcR4(244f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldc_R4, 180f);

            cursor.GotoNext(next => next.MatchRet());
            cursor.Index -= 1;
            // pop null references
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Pop);
            // remove the draw call
            cursor.Remove();
            cursor.EmitDelegate<Action<VertexPositionColor[], int>>(_final_boss_starfield_repeat_streaks);
            // pop the identity matrix
            cursor.Emit(OpCodes.Pop);
        }

        // Used by:
        //CoreStarsFG_Render
        //ReflectionFG_Render
        //StardustFG_Render
        //MirrorFG_Render
        // They are all just 1x1 pixels that mod over the screen.
        // ...and their draw methods are all so similar that I can re-use the same IL hook :P
        private static void _simple_particle_repeat(Vector2 position, float width, float height, Color color)
        {
            int vanillaScreens = (int)Math.Max(Math.Ceiling(VisibleWidth() / 320f), Math.Ceiling(VisibleHeight() / 180f));

            for (int x = 0; x < vanillaScreens; x++)
            {
                for (int y = 0; y < vanillaScreens; y++)
                {
                    Draw.Rect(position + new Vector2(x * 320f, y * 180f), width, height, color);
                }
            }
        }
        private static void SimpleParticleRepeat(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCall(
                typeof(Draw).GetMethod("Rect", new Type[] { typeof(Vector2), typeof(float), typeof(float), typeof(Color) })
            ));
            cursor.Remove();
            cursor.EmitDelegate<Action<Vector2, float, float, Color>>(_simple_particle_repeat);
        }

        // The Borealis is rendered at full res.
        // The particles are looped like all other 1x1 particles.
        private static void _northernlights_ensure_buffer_size(NorthernLights self) => ResizeBufferToZoom(self.buffer);
        private static Matrix _northernlights_swap_render_matrix(Matrix _discard_identity) => Matrix.CreateScale(1.0f / CurrentLevelZoom());

        private static void _northernlights_repeat_particles(Vector2 position, float width, float height, Color color, Camera camera)
        {
            // multiply by 0.8 to account for parallax
            position = position + (-new Vector2(160f, 90f) + (VisibleDimensions() * 0.5f)) * 0.8f;
            position.X = mod(position.X, 320f);
            position.Y = mod(position.Y, 180f);

            int vanillaScreens = (int)Math.Max(Math.Ceiling(VisibleWidth() / 320f), Math.Ceiling(VisibleHeight() / 180f));

            for (int x = 0; x < vanillaScreens; x++)
            {
                for (int y = 0; y < vanillaScreens; y++)
                {
                    Draw.Rect(position + new Vector2(x * 320f, y * 180f), width, height, color);
                }
            }
        }
        private static void NorthernLights_BeforeRender(ILContext il)
        {
            // create a properly sized buffer...
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchLdcI4(320));
            cursor.PopNext();
            cursor.EmitDelegate<Func<int>>(() => BufferWidthOverride);

            cursor.GotoNext(next => next.MatchLdcI4(180));
            cursor.PopNext();
            cursor.EmitDelegate<Func<int>>(() => BufferHeightOverride);

            // we need to ensure the buffer is properly sized...
            cursor.GotoNext(next => next.MatchStloc(0));
            cursor.Index++;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<NorthernLights>>(_northernlights_ensure_buffer_size);

            // replace the identity matrices to render at full scale.
            // I am not rewriting the rendering to render w/ orthogonal perspective
            cursor.GotoNext(next => next.MatchCall<Matrix>("get_Identity"));
            cursor.Index++;
            cursor.EmitDelegate<Func<Matrix, Matrix>>(_northernlights_swap_render_matrix);
            cursor.GotoNext(next => next.MatchCall<Matrix>("get_Identity"));
            cursor.Index++;
            cursor.EmitDelegate<Func<Matrix, Matrix>>(_northernlights_swap_render_matrix);

            
            // modify the particle draws...
            cursor.GotoNext(next => next.MatchCall(
                typeof(Draw).GetMethod("Rect", new Type[] { typeof(Vector2), typeof(float), typeof(float), typeof(Color) })
            ));

            cursor.Emit(OpCodes.Ldloc, 11);

            // replace the call with a new one
            cursor.Remove();
            cursor.EmitDelegate<Action<Vector2, float, float, Color, Camera>>(_northernlights_repeat_particles);
        }

        // yes, there's pop-in.
        // yes, that's vanilla behavior.
        // no, I am not patching that out.
        private static void _dream_stars_repeat_render(Vector2 position, float width, float height, Color color)
        {
            int vanillaScreens = (int)Math.Max(Math.Ceiling(VisibleWidth() / 320f), Math.Ceiling(VisibleHeight() / 180f));

            position = position - new Vector2(160f, 90f) + (VisibleDimensions() * 0.5f);
            position.X = mod(position.X, 320f);
            position.Y = mod(position.Y, 180f);

            for (int x = 0; x < vanillaScreens; x++)
            {
                for (int y = 0; y < vanillaScreens; y++)
                {
                    Draw.HollowRect(position + new Vector2(x * 320f, y * 180f), width, height, color);
                }
            }
        }
        private static void DreamStars_Render(ILContext il)
        {
            //DreamStars
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCall(typeof(Draw).GetMethod("HollowRect", new Type[] { typeof(Vector2), typeof(float), typeof(float), typeof(Color) })));

            cursor.Remove();
            cursor.EmitDelegate<Action<Vector2, float, float, Color>>(_dream_stars_repeat_render);
        }

        // Dream stars move with the camera in the update loop-
        // this accounts for how the camera moves when dynamically zooming
        private static Vector2 _dream_stars_move_with_zoom(Vector2 camera_location) => camera_location + (VisibleDimensions() * 0.5f);
        private static void DreamStars_Update(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchStloc(0));
            cursor.EmitDelegate<Func<Vector2, Vector2>>(_dream_stars_move_with_zoom);
        }

        // the petals loop with a modulo around the camera
        // this corrects the rendering- and accounts for edge cases.
        private static void _petals_repeat_render(MTexture texture, Vector2 position, Color color, float scale, float rotation)
        {
            int repeats = (int)Math.Ceiling(1f / CurrentLevelZoom());
            int vanillaScreensX = repeats;
            int vanillaScreensY = repeats;
            if (position.X < 16)
            {
                vanillaScreensX++;
            }
            if (position.Y < 16)
            {
                vanillaScreensY++;
            }
            for (int x = 0; x < vanillaScreensX; x++)
            {
                for (int y = 0; y < vanillaScreensY; y++)
                {
                    texture.DrawCentered(position + new Vector2(x * 320f, y * 180f), color, scale, rotation);
                }
            }
        }
        private static void Petals_Render(ILContext il)
        {
            //Petals
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchLdcR4(352f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldc_R4, 320f);

            cursor.GotoNext(next => next.MatchLdcR4(212f));
            cursor.PopNext();
            cursor.Emit(OpCodes.Ldc_R4, 180f);

            cursor.GotoNext(next => next.MatchCallvirt<MTexture>("DrawCentered"));
            cursor.Remove();
            cursor.EmitDelegate<Action<MTexture, Vector2, Color, float, float>>(_petals_repeat_render);
        }

        // The planets loop with a modulo around the camera (also)
        // This corrects the rendering by repeating the draw calls until the screen is filled.
        private static void _planets_repeat_render(MTexture texture, Vector2 position, Color color, Planets self)
        {
            Vector2 dim = new Vector2(320f, 180f) / CurrentLevelZoom();
            // scroll is accounted for again- parallax layers should respond to zoom more intensely depending on their scroll.
            position = position + (-new Vector2(160f, 90f) + new Vector2(dim.X * 0.5f, dim.Y * 0.5f)) * (Vector2.One - self.Scroll);
            position.X = mod(position.X, 640f) - 32f; // It's mathematically sound to do this, although a little wasteful. Oh well!
            position.Y = mod(position.Y, 360f) - 32f;

            int repeatRenderX = (int)Math.Ceiling(dim.X / 640f);
            int repeatRenderY = (int)Math.Ceiling(dim.Y / 360f);
            if (position.X < 32)
            {
                repeatRenderX++;
            }
            if (position.Y < 32)
            {
                repeatRenderY++;
            }
            for (int x = 0; x < repeatRenderX; x++)
            {
                for (int y = 0; y < repeatRenderY; y++)
                {
                    texture.DrawCentered(position + new Vector2(x * 640f, y * 360f), color);
                }
            }
        }
        private static void Planets_Render(ILContext il)
        {
            //Planets
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCallvirt<MTexture>("DrawCentered"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Remove();
            cursor.EmitDelegate<Action<MTexture, Vector2, Color, Planets>>(_planets_repeat_render);
        }

        // Tentacles... I just scale your render matrix to render at full res...
        // I don't want to finangle this into looping properly, and I don't want to completely rewrite it.
        // So this is technically rendering at 'infinity', perspective-wise.
        private static Matrix _tentacles_swap_render_matrix(Matrix _discard_identity) => Matrix.CreateScale(1.0f / CurrentLevelZoom());
        private static void Tentacles_Render(ILContext il)
        {
            //Tentacles
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCall<Matrix>("get_Identity"));
            cursor.Index++;
            cursor.EmitDelegate<Func<Matrix, Matrix>>(_tentacles_swap_render_matrix);
        }
        // tentacles respond to the player's position.
        // it normally uses the offset from the player to the camera-
        // this modifies it to be a proportion of the player's position along the screen instead
        private static void Tentacles_Update(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchSub());
            cursor.Index++;
            cursor.EmitDelegate<Func<float>>(CurrentLevelZoom);
            cursor.Emit(OpCodes.Mul);

            cursor.Index += 15;
            cursor.EmitDelegate(CurrentLevelZoom);
            cursor.Emit(OpCodes.Mul);
        }

        private static void FormationBackdrop_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.GotoNext(next => next.MatchLdcR4(322f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => GetBufferWidth() + 2f);

            cursor.GotoNext(next => next.MatchLdcR4(182f));
            cursor.PopNext();
            cursor.EmitDelegate<Func<float>>(() => GetBufferHeight() + 2f);

        }


        private static void AscendManager_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            cursor.ReplaceNextFloat( 340f,
                () => VisibleWidth() + 20
            );
            cursor.ReplaceNextFloat( 200f,
                () => VisibleHeight() + 20
            );
        }

        private static void AscendManager_Routine(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.ReplaceNextFloat( -160f,
                () => -VisibleWidth() * 0.5f);
            cursor.ReplaceNextFloat( -90f,
                () => -VisibleHeight() * 0.5f);
            cursor.ReplaceNextFloat( -160f,
                () => -VisibleHeight());
        }
        private static void _ascend_manager_streaks_repeat_render(MTexture texture, Vector2 position, Color color, Vector2 scale)
        {
            float zoomFactor = 1f / CurrentLevelZoom();
            int repeats = (int)Math.Ceiling(zoomFactor);
            for (int y = 0; y < repeats; y++)
            {
                texture.DrawCentered(position + new Vector2(VisibleWidth() * 0.5f - 160f, 436f * y), color, scale);
            }
        }
        private static void AscendManager_Streaks_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCallvirt<MTexture>("DrawCentered"));
            cursor.Remove();
            cursor.EmitDelegate<Action<MTexture, Vector2, Color, Vector2>>(_ascend_manager_streaks_repeat_render);
            cursor.ReplaceNextFloat( 26f, () => 26f + VisibleWidth() * 0.5f - 160f);//16 / CurrentLevelZoom());
            cursor.ReplaceNextFloat( 200f, () => 20 + VisibleHeight());

            cursor.ReplaceNextFloat( 320f, () => VisibleWidth() * 0.5f + 160f);
            //cursor.ReplaceNextFloat( 16f, () => 16 / CurrentLevelZoom());
            cursor.ReplaceNextFloat( 26f, () => 26f + VisibleWidth() * 0.5f - 160f);
            cursor.ReplaceNextFloat( 200f, () => 20 + VisibleHeight());
        }
        private static void _ascend_manager_clouds_shift_to_center(MTexture texture, Vector2 position, Color color)
        {
            texture.DrawCentered(position + new Vector2(160f * ((1f / CurrentLevelZoom()) - 1f)), color);
        }
        private static void AscendManager_Clouds_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(next => next.MatchCallvirt<MTexture>("DrawCentered"));
            cursor.Remove();
            cursor.EmitDelegate<Action<MTexture, Vector2, Color>>(_ascend_manager_clouds_shift_to_center);
        }
        private static void AscendManager_Fader_Render(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.ReplaceNextFloat( 340f, () => VisibleWidth() + 20f);
            cursor.ReplaceNextFloat( 200f, () => VisibleHeight() + 20f);
        }
        // This would be an IL hook, but there are a few reasons why it's not.
        // 1) it uses explicit vertex-based rendering
        // 2) it uses transparency
        // if it was only (1), I could just repeat the rendering of the verts every so and so
        // if it was only (2), I could hijack the texture draw call to repeat it there instead
        // But no- repeating vertex rendering would make them more opaque.
        private static void Godrays_Update(On.Celeste.Godrays.orig_Update orig, Godrays self, Scene scene)
        {
            Level level = scene as Level;

            bool visible = self.IsVisible(level);
            self.fade = Calc.Approach(self.fade, visible ? 1 : 0, Engine.DeltaTime);
            self.Visible = self.fade > 0f;
            if (!self.Visible)
            {
                return;
            }

            Player entity = level.Tracker.GetEntity<Player>();
            Vector2 downSlightLeft = Calc.AngleToVector(-1.67079639f, 1f);
            Vector2 rightSlightDown = new Vector2(0f - downSlightLeft.Y, downSlightLeft.X);

            int screens = (int)Math.Ceiling(1f / current_buffer_zoom) + 1; // controls some iteration counts below.

            // holy shit is this inefficient.
            // Oh well- it works.
            List<VertexPositionColor> vertices = new List<VertexPositionColor>();

            for (int i = 0; i < self.rays.Length; i++)
            {
                if (self.rays[i].Percent >= 1f)
                {
                    self.rays[i].Reset();
                }

                self.rays[i].Percent += Engine.DeltaTime / self.rays[i].Duration;
                self.rays[i].Y += 8f * Engine.DeltaTime;
                float percent = self.rays[i].Percent;
                float centerX = self.Mod(self.rays[i].X - level.Camera.X * 0.9f, 320f); // this is where the slight deviation occurs at 1f zoom-
                float centerY = self.Mod(self.rays[i].Y - level.Camera.Y * 0.9f, 180f); // a godray will appear on both sides of the screen at once instead of only looping offscreen.
                float width = self.rays[i].Width;
                float length = self.rays[i].Length;
                Vector2 screenPosition = new Vector2((int)centerX, (int)centerY);
                Color color = self.rayColor * Ease.CubeInOut(Calc.Clamp(((percent < 0.5f) ? percent : (1f - percent)) * 2f, 0f, 1f)) * self.fade;



                for (int x = 0; x < screens; x++)
                {
                    for (int y = 0; y < screens; y++)
                    {
                        // this is why I add one to screens- we need both the negative AND positive edges.
                        Vector2 offset = new Vector2(320f * (x - 1), 180f * (y - 1));
                        Vector2 rayScreenPos = offset + screenPosition;

                        // cull offscreen rays. the cutoffs are derived from vanilla's code.
                        if (rayScreenPos.X < -32 || rayScreenPos.Y < -32 || rayScreenPos.X > BufferWidthOverride + 32 || rayScreenPos.Y > BufferHeightOverride + 32) continue;

                        // if the player is near this instance, fade it.
                        Color instanceColor = color;

                        if (entity != null)
                        {
                            float distanceToPlayerSq = (rayScreenPos + level.Camera.Position - entity.Position).LengthSquared();
                            const float CUTOFF = 64f * 64f;
                            if (distanceToPlayerSq < CUTOFF)
                            {
                                instanceColor *= 0.25f + 0.75f * ((float)Math.Sqrt(distanceToPlayerSq) / 64f);
                            }
                        }

                        // push the onscreen ones to the buffer.
                        VertexPositionColor vertexPositionColor = new VertexPositionColor(new Vector3(offset + screenPosition + rightSlightDown * width + downSlightLeft * length, 0f), instanceColor);
                        VertexPositionColor vertexPositionColor2 = new VertexPositionColor(new Vector3(offset + screenPosition - rightSlightDown * width, 0f), instanceColor);
                        VertexPositionColor vertexPositionColor3 = new VertexPositionColor(new Vector3(offset + screenPosition + rightSlightDown * width, 0f), instanceColor);
                        VertexPositionColor vertexPositionColor4 = new VertexPositionColor(new Vector3(offset + screenPosition - rightSlightDown * width - downSlightLeft * length, 0f), instanceColor);
                        vertices.Add(vertexPositionColor);
                        vertices.Add(vertexPositionColor2);
                        vertices.Add(vertexPositionColor3);
                        vertices.Add(vertexPositionColor2);
                        vertices.Add(vertexPositionColor3);
                        vertices.Add(vertexPositionColor4);
                    }
                }
            }

            self.vertices = vertices.ToArray();
            self.vertexCount = vertices.Count;
        }
    }
}
