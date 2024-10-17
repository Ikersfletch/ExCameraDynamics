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
    [CustomBackdrop("ExCameraDynamics/ZoomParticleDepth")]
    public class ZoomParticleDepth : Backdrop
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
            public float Depth;
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

            public float MinZoomDepth;
            public float MaxZoomDepth;

            public float BaseScale;

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
                Scale = data.BaseScale,
                TextureIndex = Calc.Random.Next(data.TextureCount),
                Tint = Calc.Random.Next(data.TintCount),
                Position = new Vector2(Calc.Random.Next(data.Bounds.Width), Calc.Random.Next(data.Bounds.Height)),
                Velocity = new Vector2(Calc.Random.Range(data.MinSpeedX, data.MaxSpeedX), Calc.Random.Range(data.MinSpeedY, data.MaxSpeedY)),
                Depth = Calc.Random.Range(data.MinZoomDepth, data.MaxZoomDepth),
                Angle = data.BaseAngle - data.AngleSpread * 0.5f + Calc.Random.NextFloat() * data.AngleSpread,
                AngularVelocity = Calc.Random.Range(data.MinAngleSpeed, data.MaxAngleSpeed),
                WindAffect = Calc.Random.Range(data.MinWindAffect, data.MaxWindAffect)
            };
        }
        /// <summary>
        /// Derive this class and override this method to make custom-behavior particle effects that work w/ zoom.
        /// </summary>
        /// <param name="particle"></param>
        /// <param name="deltaTime"></param>
        /// <param name="wind"></param>
        /// <returns></returns>
        public virtual Particle Tick(Particle particle, float deltaTime, Vector2 wind) {
            particle.Position = particle.Position + (particle.Velocity + wind * particle.WindAffect) * deltaTime;
            particle.Angle = particle.Angle + particle.AngularVelocity * deltaTime;
            return particle;
        }
        
        public ZoomParticleDepth(BinaryPacker.Element data)
        {
            Textures = GFX.Game.GetAtlasSubtextures(data.Attr("textures").TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'));//base_texture);
            

            Name = $"ZoomParticle_{Textures[0].AtlasPath}";

            List<Particle> presort_particles = new List<Particle>();



            Bounds = new ParticleBounds
            {
                Width = data.AttrInt("particleSpreadX", 320),
                Height = data.AttrInt("particleSpreadY", 180)
            };

            ParticleInitData init = new ParticleInitData() {
                TextureCount = Textures.Count,
                TintCount = 1,

                MinZoomDepth = data.AttrFloat("minParticleDepth", 0f),
                MaxZoomDepth = data.AttrFloat("maxParticleDepth", 0f),

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

                BaseScale = data.AttrFloat("particleScale", 1f),

                Bounds = Bounds
            };

            for (int i = 0; i < data.AttrInt("particleCount", 100); i++)
            {
                presort_particles.Add(Init(i, init));
            }

            presort_particles.Sort((a, b) => b.Depth.CompareTo(a.Depth));

            Particles = presort_particles.ToArray();

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
            Vector2 cameraPosition = level.Camera.Position + cameraZoomOffset;

            float alpha = Alpha * fadeIn;

            if (FadeX != null)
            {
                alpha *= FadeX.Value(cameraPosition.X);
            }
            if (FadeY != null)
            {
                alpha *= FadeY.Value(cameraPosition.Y);
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

            float cameraDepth = -1f / level.Zoom;
            Vector2 cameraMidpoint = new Vector2(160f, 90f) / level.Zoom;

            // don't have the overhead of loop logic unless necessary
            if (!LoopX && !LoopY)
            {
                for (int i = 0; i < Particles.Length; i++)
                {
                    Vector2 renderPosition = Particles[i].Position + Position - cameraPosition + cameraZoomOffset;
                    float depth = Particles[i].Depth - cameraDepth;

                    if (depth <= 0)
                    {
                        continue;
                    }

                    float partAlpha = alpha;

                    if (FadeZ != null)
                    {
                        partAlpha *= FadeZ.Value(Particles[i].Depth);
                    }

                    if (partAlpha <= 0f)
                    {
                        continue;
                    }

                    float scaleFactor = 1f / (depth * level.Zoom);

                    renderPosition = Vector2.Lerp(cameraMidpoint, renderPosition, scaleFactor);

                    Textures[Particles[i].TextureIndex].DrawCentered(renderPosition, renderColor * partAlpha, Particles[i].Scale * scaleFactor, Particles[i].Angle);
                    //Textures[Particles[i].TextureIndex].DrawCentered(renderPosition, renderColor, Particles[i].Scale, Particles[i].Angle);
                }
                return;
            }

            float screenWidth = 320f / level.Zoom;
            float screenHeight = 180f / level.Zoom;


            for (int i = 0; i < Particles.Length; i++)
            {

                Vector2 renderPosition = Particles[i].Position + Position - cameraPosition + cameraZoomOffset;


                float depth = Particles[i].Depth - cameraDepth;

                if (depth <= 0)
                {
                    continue;
                }

                float partAlpha = alpha;

                if (FadeZ != null)
                {
                    partAlpha *= FadeZ.Value(Particles[i].Depth);
                }

                if (partAlpha <= 0f)
                {
                    continue;
                }


                float scaleFactor = 1f / (depth * level.Zoom);

                renderPosition = Vector2.Lerp(cameraMidpoint, renderPosition, scaleFactor);

                float particleScale = Particles[i].Scale * scaleFactor;

                float perspectiveBoundWidth = Bounds.Width * scaleFactor;
                float perspectiveBoundHeight = Bounds.Height* scaleFactor;

                renderPosition.X = mod(renderPosition.X, perspectiveBoundWidth);
                renderPosition.Y = mod(renderPosition.Y, perspectiveBoundHeight);


                //Textures[Particles[i].TextureIndex].DrawCentered(renderPosition, renderColor, scale);

                int max = MaxTextureDims[Particles[i].TextureIndex];

                bool extraXRepeat = LoopX && renderPosition.X >= (Bounds.Width * scaleFactor) - max * particleScale;
                bool extraYRepeat = LoopY && renderPosition.Y >= (Bounds.Height * scaleFactor) - max * particleScale;

                // we're rendering using DrawCentered- move the center to the relevant position...
                renderPosition += Vector2.One * max * 0.5f * scaleFactor;

                renderPosition.X = (int)Math.Floor(renderPosition.X);
                renderPosition.Y = (int)Math.Floor(renderPosition.Y);

                for (float x = extraXRepeat ? -perspectiveBoundWidth : 0; x < screenWidth; x += perspectiveBoundWidth)
                    for (float y = extraYRepeat ? -perspectiveBoundHeight : 0; y < screenHeight; y += perspectiveBoundHeight)
                        Textures[Particles[i].TextureIndex].DrawCentered(renderPosition + new Vector2(x, y), renderColor * partAlpha, particleScale, Particles[i].Angle);
            }
        }

        private float mod(float a, float b)
        {
            return ((a % b) + b) % b;
        }
    }
}
