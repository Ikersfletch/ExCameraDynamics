using Celeste;
using Celeste.Mod;
using Celeste.Mod.Backdrops;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Globalization;
using System.Reflection;

namespace ExtendedCameraDynamics.Code.Backdrops
{
    [CustomBackdrop("ExCameraDynamics/ZoomParallaxDepth")]
    public class ZoomParallaxDepth : Backdrop
    {
        public Vector2 CameraOffset = Vector2.Zero;

        public BlendState BlendState = BlendState.AlphaBlend;

        public MTexture Texture;

        public bool DoFadeIn;

        public float Alpha = 1f;

        public float fadeIn = 1f;

        public Fader FadeZ;

        /// <summary>
        /// The camera z coord is calculated as -1f / level.Zoom.
        /// </summary>
        public float ApparentDepth { get; set; } = 0f;

        /// <summary>
        /// A constant scaling applied to the texture.
        /// Generally equal to ( 320f / ImageWidth ) for screen-locked stylegrounds
        /// </summary>
        public float BaseScale { get; set; } = 1f;

        public static readonly FieldInfo spriteBatchSamplerState = typeof(SpriteBatch).GetField("samplerState", BindingFlags.Instance | BindingFlags.NonPublic);
        
        public ZoomParallaxDepth(BinaryPacker.Element data)
        {
            Texture = GFX.Game[data.Attr("texture")];
            Name = Texture.AtlasPath;
            BaseScale = data.AttrFloat("baseScale", 1f);
            ApparentDepth = data.AttrFloat("apparentDepth", 1f);

            // you can fade in/out alongside the zoom.
            data.AttrIf("fadez", (data) => FadeZ = ParseZoomFader(data));
        }

        // from MapData.cs, but only accepts positive values--zoom should never be negative!
        public static Fader ParseZoomFader(string attribute)
        {
            Fader fader = new Fader();
            string[] segments = attribute.Split(new char[1] { ':' });
            for (int i = 0; i < segments.Length; i++)
            {
                string[] segment = segments[i].Split(new char[1] { ',' });
                if (segment.Length == 2)
                {
                    string[] zoomRange = segment[0].Split(new char[1] { '-' });
                    string[] alphaRange = segment[1].Split(new char[1] { '-' });
                    float fadeFrom = float.Parse(alphaRange[0], CultureInfo.InvariantCulture);
                    float fadeTo = float.Parse(alphaRange[1], CultureInfo.InvariantCulture);
                    fader.Add(float.Parse(zoomRange[0]), float.Parse(zoomRange[1]), fadeFrom, fadeTo);
                }
            }
            return fader;
        }

        public override void Update(Scene scene)
        {
//            Celeste.MapData

            base.Update(scene);
            Position += Speed * Engine.DeltaTime;
            Position += WindMultiplier * (scene as Level).Wind * Engine.DeltaTime;
            if (DoFadeIn)
            {
                fadeIn = Calc.Approach(fadeIn, Visible ? 1 : 0, Engine.DeltaTime);
            }
            else
            {
                fadeIn = (Visible ? 1 : 0);
            }
        }
        public override void Render(Scene scene)
        {
            if (Texture.IsPacked || spriteBatchSamplerState.GetValue(Draw.SpriteBatch) != SamplerState.PointWrap)
            {
                RenderByRepetition(scene);
                return;
            }

            RenderByClipping(scene);
        }

        public float FinalScaleFactor(Level level)
        {
            //float relativeDepth = ApparentDepth + 1f / level.Zoom;
            if (ApparentDepth + 1f / level.Zoom <= 0f)
            {
                // 'behind' the camera, so don't render anything.
                return 0f;
            }
            // remove the zoom factor
            //return (BaseScale / relativeDepth) / level.Zoom;
            return BaseScale / (ApparentDepth * level.Zoom + 1f);
        }

        // Render the full backdrop by changing the clipping rectangle on the one texture draw call.
        public void RenderByClipping(Scene scene)
        {
            Level level = scene as Level;

            Vector2 zoomOffset = CameraZoomHooks.GetParallaxZoomOffset();
            Vector2 CameraPositionIfZoomWas1f = (level.Camera.Position + this.CameraOffset + zoomOffset).Floor();

            float alpha = fadeIn * Alpha * FadeAlphaMultiplier;
            if (FadeX != null)
            {
                alpha *= FadeX.Value(CameraPositionIfZoomWas1f.X + 160f);
            }

            if (FadeY != null)
            {
                alpha *= FadeY.Value(CameraPositionIfZoomWas1f.Y + 90f);
            }

            if (FadeZ != null)
            {
                alpha *= FadeZ.Value(level.Zoom);
            }

            Color color = Color;
            if (alpha < 1f)
            {
                color *= alpha;
            }

            // if not fully transparent...
            if (color.A > 1)
            {
                float renderScale = FinalScaleFactor(level);

                if (renderScale <= 0f) return;

                float effectiveTextureWidth = Texture.Width * renderScale;
                float effectiveTextureHeight = Texture.Height * renderScale;

                CameraPositionIfZoomWas1f = (level.Camera.Position + CameraZoomHooks.CameraFloatingDecimal + this.CameraOffset + zoomOffset);

                Vector2 relativePosition = Vector2.Lerp(
                    CameraZoomHooks.GetCameraDimensions(level) * 0.5f,
                    Position - CameraPositionIfZoomWas1f * Scroll + zoomOffset,
                    renderScale
                ).Floor();

                // mod the positions if it's supposed to loop...
                if (LoopX)
                {
                    relativePosition.X = (relativePosition.X % effectiveTextureWidth - effectiveTextureWidth) % effectiveTextureWidth;
                }

                if (LoopY)
                {
                    relativePosition.Y = (relativePosition.Y % effectiveTextureHeight - effectiveTextureHeight) % effectiveTextureHeight;
                }

                // flip them as necessary...
                SpriteEffects flip = SpriteEffects.None;
                if (FlipX)
                {
                    flip |= SpriteEffects.FlipHorizontally;
                }

                if (FlipY)
                {
                    flip |= SpriteEffects.FlipVertically;
                }

                int clipWidth = (int)Math.Ceiling((LoopX ? (CameraZoomHooks.GetCameraWidth(level) + 1 - relativePosition.X) : effectiveTextureWidth) / BaseScale);
                int clipHeight = (int)Math.Ceiling((LoopY ? (CameraZoomHooks.GetCameraHeight(level) + 1 - relativePosition.Y) : effectiveTextureHeight) / BaseScale);
                Rectangle clipRect = new Rectangle(FlipX ? (-clipWidth) : 0, FlipY ? (-clipHeight) : 0, clipWidth, clipHeight);
                float scaleFix = Texture.ScaleFix;
                Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, relativePosition, clipRect, color, 0f, (-Texture.DrawOffset / scaleFix), renderScale * scaleFix, flip, 0f);
            }
        }
        
        // Render the full backdrop by repeating the draw call many times across the screen.
        public void RenderByRepetition(Scene scene)
        {
            Level level = scene as Level;

            Vector2 zoomOffset = CameraZoomHooks.GetParallaxZoomOffset();
            Vector2 CameraPositionIfZoomWas1f = (level.Camera.Position + this.CameraOffset + zoomOffset).Floor();

            float alpha = fadeIn * Alpha * FadeAlphaMultiplier;
            if (FadeX != null)
            {
                alpha *= FadeX.Value(CameraPositionIfZoomWas1f.X + 160f);
            }

            if (FadeY != null)
            {
                alpha *= FadeY.Value(CameraPositionIfZoomWas1f.Y + 90f);
            }

            if (FadeZ != null)
            {
                alpha *= FadeZ.Value(level.Zoom);
            }


            Color color = Color;
            if (alpha < 1f)
            {
                color *= alpha;
            }

            if (color.A <= 1)
            {
                return;
            }

            float renderScale = FinalScaleFactor(level);

            if (renderScale <= 0f) return;

            float effectiveTextureWidth = Texture.Width * renderScale;
            float effectiveTextureHeight = Texture.Height * renderScale;

            CameraPositionIfZoomWas1f = (level.Camera.Position + CameraZoomHooks.CameraFloatingDecimal + this.CameraOffset + zoomOffset);

            Vector2 relativePosition = Vector2.Lerp(
                CameraZoomHooks.GetCameraDimensions(level) * 0.5f,
                Position - CameraPositionIfZoomWas1f * Scroll + zoomOffset,
                renderScale
            );//.Floor();


            if (LoopX)
            {
                while (relativePosition.X < 0f)
                {
                    relativePosition.X += effectiveTextureWidth;
                }

                while (relativePosition.X > 0f)
                {
                    relativePosition.X -= effectiveTextureWidth;
                }
            }

            if (LoopY)
            {
                while (relativePosition.Y < 0f)
                {
                    relativePosition.Y += effectiveTextureHeight;
                }

                while (relativePosition.Y > 0f)
                {
                    relativePosition.Y -= effectiveTextureHeight;
                }
            }

            SpriteEffects flip = SpriteEffects.None;
            if (FlipX && FlipY)
            {
                flip = SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically;
            }
            else if (FlipX)
            {
                flip = SpriteEffects.FlipHorizontally;
            }
            else if (FlipY)
            {
                flip = SpriteEffects.FlipVertically;
            }

            for (float x = relativePosition.X; x < CameraZoomHooks.GetVisibleCameraWidth() + 1; x += effectiveTextureWidth)
            {
                for (float y = relativePosition.Y; y < CameraZoomHooks.GetVisibleCameraHeight() + 1; y += effectiveTextureHeight)
                {
                    //Texture.Draw()
                    float scaleFix = Texture.ScaleFix;
                    Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, new Vector2(x,y), Texture.ClipRect, color, 0f, -Texture.DrawOffset / scaleFix, renderScale * scaleFix, flip, 0f);
                    //Texture.Draw(new Vector2(x, y), Vector2.Zero, color, renderScale, 0f, flip);
                    if (!LoopY)
                    {
                        break;
                    }
                }

                if (!LoopX)
                {
                    break;
                }
            }
        }
    }
}
