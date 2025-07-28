using Celeste;
using Celeste.Mod;
using Celeste.Mod.Backdrops;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;

namespace ExtendedCameraDynamics.Code.Backdrops
{
    /// <summary>
    /// Like Vanilla planets / petals, but customizable.
    /// </summary>
    [CustomBackdrop("ExCameraDynamics/ZoomParticleParallax")]
    public class ZoomParticleParallax : Backdrop
    {
        public struct Particle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public int Tint;
            public float Angle;
            public float AngularVelocity;
            public int TextureIndex;
            public float Scale;
            public Vector2 Scroll;
            public float WindAffect;
        }

        public struct ParticleBounds
        {
            public int Width;
            public int Height;
        }

        public class ParticleInitData
        {
            public int TextureCount;
            public int TintCount;

            public float MinScrollX;
            public float MaxScrollX;

            public float MinScrollY;
            public float MaxScrollY;

            public float BaseAngle;
            public float AngleSpread;

            public float MinAngleSpeed;
            public float MaxAngleSpeed;

            public float MinSpeedX;
            public float MaxSpeedX;

            public float MinSpeedY;
            public float MaxSpeedY;

            public float MinWindAffect;
            public float MaxWindAffect;

            public ParticleBounds Bounds;
        }

        public BlendState BlendState = BlendState.AlphaBlend;
        public List<MTexture> Textures;
        public int[] MaxTextureDims;
        public Particle[] Particles;
        public ParticleBounds Bounds;
        public float Alpha = 1f;
        public bool DoFadeIn;
        public float fadeIn = 1f;
        public Fader FadeZ;

        public virtual Particle Init(
            int i,
            ParticleInitData data
        )
        {
            return new Particle
            {
                Scale = 1,
                TextureIndex = Calc.Random.Next(data.TextureCount),
                Tint = Calc.Random.Next(data.TintCount),
                Position = new Vector2(Calc.Random.Next(data.Bounds.Width), Calc.Random.Next(data.Bounds.Height)),
                Velocity = new Vector2(Calc.Random.Range(data.MinSpeedX, data.MaxSpeedX), Calc.Random.Range(data.MinSpeedY, data.MaxSpeedY)),
                Scroll = new Vector2(Calc.Random.Range(data.MinScrollX, data.MaxScrollX), Calc.Random.Range(data.MinScrollY, data.MaxScrollY)),
                Angle = data.BaseAngle - data.AngleSpread * 0.5f + Calc.Random.NextFloat() * data.AngleSpread,
                AngularVelocity = Calc.Random.Range(data.MinAngleSpeed, data.MaxAngleSpeed),
                WindAffect = Calc.Random.Range(data.MinWindAffect, data.MaxWindAffect)
            };
        }
        public virtual Particle Tick(Particle particle, float deltaTime, Vector2 wind) {
            particle.Position = particle.Position + (particle.Velocity + wind * particle.WindAffect) * deltaTime;
            particle.Angle = particle.Angle + particle.AngularVelocity * deltaTime;
            return particle;
        }

        /*
        public Vector2 ParticleDimensions(int textureID, float angle, float scale)
        {
            MTexture texture = Textures[textureID];
            Vector2 dims = new Vector2(
                texture.Width * scale,
                texture.Height * scale
            );
            float cos = Math.Abs((float)Math.Cos(angle));
            float sin = Math.Abs((float)Math.Sin(angle));
            return 
                new Vector2(
                    (float)Math.Ceiling(dims.X * cos + dims.Y * sin),
                    (float)Math.Ceiling(dims.X * sin + dims.Y * cos)
                );
        }
        */

        public ZoomParticleParallax(BinaryPacker.Element data)
        {
            Textures = GFX.Game.GetAtlasSubtextures(data.Attr("textures").TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'));//base_texture);
            

            Name = $"ZoomParticle_{Textures[0].AtlasPath}";
            Particles = new Particle[data.AttrInt("particleCount", 100)];

            Bounds = new ParticleBounds
            {
                Width = data.AttrInt("particleSpreadX", 320),
                Height = data.AttrInt("particleSpreadY", 180)
            };

            ParticleInitData init = new ParticleInitData() {
                TextureCount = Textures.Count,
                TintCount = 1,

                MinScrollX = data.AttrFloat("minParticleScrollX", 0f),
                MaxScrollX = data.AttrFloat("maxParticleScrollX", 0f),

                MinScrollY = data.AttrFloat("minParticleScrollY", 0f),
                MaxScrollY = data.AttrFloat("maxParticleScrollY", 0f),

                MinSpeedX = data.AttrFloat("minParticleSpeedX", 0f),
                MaxSpeedX = data.AttrFloat("maxParticleSpeedX", 0f),

                MinSpeedY = data.AttrFloat("minParticleSpeedY", 0f),
                MaxSpeedY = data.AttrFloat("maxParticleSpeedY", 0f),

                BaseAngle = data.AttrFloat("baseParticleAngle", 0f),
                AngleSpread = data.AttrFloat("particleAngleSpread", 0f),

                MinAngleSpeed = data.AttrFloat("minParticleAngleSpeed", 0f),
                MaxAngleSpeed = data.AttrFloat("maxParticleAngleSpeed", 0f),

                MinWindAffect = data.AttrFloat("minParticleWindAffect", 0f),
                MaxWindAffect = data.AttrFloat("maxParticleWindAffect", 0f),

                Bounds = Bounds
            };

            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i] = Init(i, init);
            }

            MaxTextureDims = new int[Textures.Count];
            for (int i = 0; i < MaxTextureDims.Length; i++)
            {
                MaxTextureDims[i] = (int)Math.Ceiling(new Vector2(Textures[i].Width, Textures[i].Height).Length());
            }

            data.AttrIf("fadez", (data) => FadeZ = ZoomParallax.ParseZoomFader(data));
        }

        public override void Update(Scene scene)
        {
            base.Update(scene);
            Vector2 wind = (scene as Level).Wind;
            Position += Speed * Engine.DeltaTime;
            Position += WindMultiplier * wind * Engine.DeltaTime;
            if (DoFadeIn)
            {
                fadeIn = Calc.Approach(fadeIn, Visible ? 1 : 0, Engine.DeltaTime);
            }
            else
            {
                fadeIn = (Visible ? 1 : 0);
            }
            for (int i = 0; i < Particles.Length; i++)
            {
                Particles[i] = Tick(Particles[i], Engine.DeltaTime, wind);
            }
        }

        public override void Render(Scene scene)
        {
            base.Render(scene);

            Level level = scene as Level;
            Vector2 cameraZoomOffset = CameraZoomHooks.GetParallaxZoomOffset();
            // this is as if level.Zoom == 1f
            Vector2 cameraPosition = level.Camera.Position + cameraZoomOffset + CameraZoomHooks.CameraFloatingDecimal;

            float alpha = Alpha * fadeIn;

            if (FadeX != null)
            {
                alpha *= FadeX.Value(cameraPosition.X);
            }
            if (FadeY != null)
            {
                alpha *= FadeY.Value(cameraPosition.Y);
            }
            if (FadeZ != null)
            {
                alpha *= FadeZ.Value(level.Zoom);
            }

            // Don't render what's invisible.
            if (alpha <= 0f)
            {
                return;
            }


            Color renderColor = Color;
            if (alpha <= 1f)
            {
                renderColor *= alpha;
            }


            // don't have the overhead of loop logic unless necessary
            if (!LoopX && !LoopY)
            {
                for (int i = 0; i < Particles.Length; i++)
                {
                    Vector2 renderPosition = Particles[i].Position + Position - cameraPosition * Particles[i].Scroll + cameraZoomOffset;
                    Textures[Particles[i].TextureIndex].DrawCentered(renderPosition, renderColor, Particles[i].Scale, Particles[i].Angle);
                    //Textures[Particles[i].TextureIndex].DrawCentered(renderPosition, renderColor, Particles[i].Scale, Particles[i].Angle);
                }
                return;
            }


            for (int i = 0; i < Particles.Length; i++)
            {

                Vector2 renderPosition = Particles[i].Position + Position - cameraPosition * Particles[i].Scroll + cameraZoomOffset;

                renderPosition.X = mod(renderPosition.X, Bounds.Width);
                renderPosition.Y = mod(renderPosition.Y, Bounds.Height);

                float scale = Particles[i].Scale;

                int max = MaxTextureDims[Particles[i].TextureIndex];

                bool extraXRepeat = LoopX && renderPosition.X >= Bounds.Width - max * scale;
                bool extraYRepeat = LoopY && renderPosition.Y >= Bounds.Height - max * scale;

                // we're rendering using DrawCentered- move the center to the relevant position...
                renderPosition += Vector2.One * max * 0.5f * scale;

                renderPosition.X = (int)Math.Floor(renderPosition.X);
                renderPosition.Y = (int)Math.Floor(renderPosition.Y);

                for (int x = extraXRepeat ? -Bounds.Width : 0; x < 320f / level.Zoom; x += Bounds.Width)
                    for (int y = extraYRepeat ? -Bounds.Height : 0; y < 180f / level.Zoom; y += Bounds.Height)
                        Textures[Particles[i].TextureIndex].DrawCentered(renderPosition + new Vector2(x, y), renderColor, scale, Particles[i].Angle);
            }
        }

        private float mod(float a, float b)
        {
            return ((a % b) + b) % b;
        }
    }
}
