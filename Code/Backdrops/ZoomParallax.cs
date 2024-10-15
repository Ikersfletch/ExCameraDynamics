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
using System.Runtime.CompilerServices;

namespace ExtendedCameraDynamics.Code.Backdrops
{
    [CustomBackdrop("ExCameraDynamics/ZoomParallax")]
    public class ZoomParallax : Backdrop
    {
        public Vector2 CameraOffset = Vector2.Zero;

        public BlendState BlendState = BlendState.AlphaBlend;

        public MTexture Texture;

        public bool DoFadeIn;

        public float Alpha = 1f;

        public float fadeIn = 1f;

        public Fader FadeZ;

        /// <summary>
        /// The amount that zoom affects the scaling <br></br>
        /// 0f => vanilla scaling <br></br>
        /// 1f => always takes the same amount of screenspace <br></br>
        /// </summary>
        public float ScaleRetention { get; set; } = 0f;

        /// <summary>
        /// A constant scaling applied to the texture.
        /// Generally equal to ( 320f / ImageWidth ) for screen-locked stylegrounds
        /// </summary>
        public float BaseScale { get; set; } = 1f;

        public static readonly FieldInfo spriteBatchSamplerState = typeof(SpriteBatch).GetField("samplerState", BindingFlags.Instance | BindingFlags.NonPublic);
        
        public ZoomParallax(BinaryPacker.Element data)
        {
            string id = data.Attr("texture");
            string text = data.Attr("atlas", "game");
            Texture = ((text == "game" && GFX.Game.Has(id)) ? GFX.Game[id] : ((!(text == "gui") || !GFX.Gui.Has(id)) ? GFX.Misc[id] : GFX.Gui[id]));
            //Texture = GFX.Game[data.Attr("texture")];
            Name = Texture.AtlasPath;
            BaseScale = data.AttrFloat("baseScale", 1f);
            ScaleRetention = data.AttrFloat("scaleRetention", 1f);

            // you can fade in/out alongside the zoom.
            data.AttrIf("fadez", (data) => FadeZ = ParseZoomFader(data));
        }

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

                    int zoomMinSign = 1;
                    int zoomMaxSign = 1;
                    if (zoomRange[0][0] == 'n')
                    {
                        zoomMinSign = -1;
                        zoomRange[0] = zoomRange[0].Substring(1);
                    }

                    if (zoomRange[1][0] == 'n')
                    {
                        zoomMaxSign = -1;
                        zoomRange[1] = zoomRange[1].Substring(1);
                    }

                    fader.Add(zoomMinSign * float.Parse(zoomRange[0]), zoomMaxSign * float.Parse(zoomRange[1]), fadeFrom, fadeTo);
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
            return BaseScale / MathF.Pow(level.Zoom, ScaleRetention);
        }

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
                float effectiveTextureWidth = Texture.Width * renderScale;
                float effectiveTextureHeight = Texture.Height * renderScale;

                Vector2 relativePosition = (Position - CameraPositionIfZoomWas1f * Scroll + zoomOffset).Floor();

                /*
                relativePosition -= CameraZoomHooks.GetCameraDimensions(level) * 0.5f;
                relativePosition *= renderScale;
                relativePosition += CameraZoomHooks.GetCameraDimensions(level) * 0.5f;
                relativePosition = relativePosition.Floor();
                */
                // (((relativePosition - dimensions) * renderScale) + dimensions)
                // relativePosition * renderScale - dimensions * renderScale + dimensions
                // relativePosition * renderScale + dimenstions ( 1 - renderScale )
                // = Lerp(dimensions, relativePosition, renderScale)

                Vector2 dimensions = CameraZoomHooks.GetCameraDimensions(level) * 0.5f;
                relativePosition = Vector2.Lerp(dimensions, relativePosition, renderScale).Floor();


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

                int clipWidth = (int)Math.Ceiling((LoopX ? (CameraZoomHooks.GetCameraWidth(level) - relativePosition.X) : effectiveTextureWidth) / BaseScale);
                int clipHeight = (int)Math.Ceiling((LoopY ? (CameraZoomHooks.GetCameraHeight(level) - relativePosition.Y) : effectiveTextureHeight) / BaseScale);
                Rectangle clipRect = new Rectangle(FlipX ? (-clipWidth) : 0, FlipY ? (-clipHeight) : 0, clipWidth, clipHeight);
                float scaleFix = Texture.ScaleFix;
                Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, relativePosition, clipRect, color, 0f, (-Texture.DrawOffset / scaleFix), renderScale * scaleFix, flip, 0f);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
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
            float effectiveTextureWidth = Texture.Width * renderScale;
            float effectiveTextureHeight = Texture.Height * renderScale;

            Vector2 relativePosition = (Position - CameraPositionIfZoomWas1f * Scroll + zoomOffset);

            /*
            relativePosition -= CameraZoomHooks.GetCameraDimensions(level) * 0.5f;
            relativePosition *= renderScale;
            relativePosition += CameraZoomHooks.GetCameraDimensions(level) * 0.5f;
            relativePosition = relativePosition.Floor();
            */
            // (((relativePosition - dimensions) * renderScale) + dimensions)
            // relativePosition * renderScale - dimensions * renderScale + dimensions
            // relativePosition * renderScale + dimenstions ( 1 - renderScale )
            // = Lerp(dimensions, relativePosition, renderScale)

            Vector2 dimensions = CameraZoomHooks.GetCameraDimensions(level) * 0.5f;
            relativePosition = Vector2.Lerp(dimensions, relativePosition, renderScale).Floor();


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

            float camWidth = CameraZoomHooks.GetVisibleCameraWidth();
            float camHeight = CameraZoomHooks.GetVisibleCameraHeight();

            for (float x = relativePosition.X; x < camWidth; x += effectiveTextureWidth)
            {
                for (float y = relativePosition.Y; y < camHeight; y += effectiveTextureHeight)
                {
                    //Texture.Draw()
                    float scaleFix = Texture.ScaleFix;
                    Draw.SpriteBatch.Draw(Texture.Texture.Texture_Safe, new Vector2(x, y), Texture.ClipRect, color, 0f, -Texture.DrawOffset / scaleFix, renderScale * scaleFix, flip, 0f);
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
