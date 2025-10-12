using Celeste;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.Registry.DecalRegistryHandlers;
using Microsoft.Xna.Framework;
using Monocle;
using System.Xml;

namespace ExtendedCameraDynamics.Code.Backdrops
{
    internal class ZoomDecalDepth : DecalRegistryHandler
    {
        internal class DecalRepeater : Entity
        {
            private float z;
            private Decal decal;
            private Level level;
            private Decal.DecalImage image;
            private Vector2 baseScale;
            private Vector2 basePos;

            public DecalRepeater(Decal orig, float z)
            {
                this.z = z;
                this.decal = orig;
                Active = false;
                Visible = true;

                level = orig.Scene as Level;

                baseScale = orig.Scale;
                basePos = orig.Position;
                Depth = (int)(z * 500f);

                if (z > 0f)
                {
                    Depth += Depths.BGTerrain;
                }
                else if (z == 0f)
                {
                    Depth += 10;
                }
            }

            public override void Render()
            {
                if (image == null)
                {
                    image = decal.Get<Decal.DecalImage>();
                    image.Visible = false;
                }

                float apparent_dist = z + 1f / level.Zoom;

                if (apparent_dist < 0f)
                {
                    return;
                }

                Vector2 pos = decal.Position;

                float scale = 1f / (z * level.Zoom + 1f);
                decal.Scale = baseScale * scale;
                decal.Position = level.Camera.Position + Vector2.Lerp(new Vector2(160f, 90f) / level.Zoom, basePos - level.Camera.Position, scale);
                image.Render();

                decal.Position = pos;
            }
        }
        internal class ApparentDepth : Component
        {
            private Decal.DecalImage image;
            private Decal decal;
            private Level level;
            private float z = 0f;
            private Vector2 basePos;
            private Vector2 baseScale;
            public ApparentDepth(Decal decal, float z, bool setDepth = true) : base(false, true)
            {
                this.z = z;
                this.decal = decal;
                basePos = decal.Position;
                baseScale = decal.Scale;
                level = decal.Scene as Level;

                if (setDepth)
                {
                    decal.Depth = (int)(z* 500f);

                    if (z > 0f)
                    {
                        decal.Depth += Depths.BGTerrain;
                    }
                    else if (z == 0f)
                    {
                        decal.Depth += 10;
                    }
                }
            }

            public override void Render()
            {
                if (image == null)
                {
                    image = decal.Get<Decal.DecalImage>();
                    image.Visible = false;
                }

                float apparent_dist = z + 1f / level.Zoom;

                if (apparent_dist < 0f)
                {
                    return;
                }

                float scale = 1f / (z * level.Zoom + 1f);
                decal.Scale = baseScale * scale;
                decal.Position = level.Camera.Position + Vector2.Lerp(new Vector2(160f, 90f) / level.Zoom, basePos - level.Camera.Position, scale);
                image.Render();
            }
        }

        private float? z = 0f;
        private int repeat = 0;
        private float step = 0f;
        private bool override_depth = true;
        public override string Name => "excam.z";

        public override void ApplyTo(Decal decal)
        {
            if (z == null)
                return;

            ApparentDepth d;

            decal.Add(d = new ApparentDepth(decal, z.Value, override_depth));

            for (int i = 1; i <= repeat; i++)
            {
                decal.Scene.Add(new DecalRepeater(decal, z.Value + step * i));
            }

            d.Update();
        }

        public override void Parse(XmlAttributeCollection xml)
        {
            z = GetNullable<float>(xml, "z");
            override_depth = Get<bool>(xml, "set_depth", true);



            string depths = Get<string>(xml, "repeat", "");
            string[] vals = depths.Split(",");

            if (vals.Length != 2) {
                return;
            }

            repeat = int.Parse(vals[0]);

            if (repeat <= 0) {
                return;
            }

            float tot = float.Parse(vals[1]);

            if (tot == 0f)
            {
                repeat = 0;
                return;
            }

            if (tot > 0f)
            {
                z += tot;
                tot = -tot;
            }

            step = tot / (float) repeat;
        }
    }
}
