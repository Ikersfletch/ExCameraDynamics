using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Components;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.ExCameraDynamics.Code.Entities
{
    public struct CameraFocus
    {
        public Vector2 Center => Position + new Vector2(160f, 90f) / Zoom;
        public Vector2 Dimensions => new Vector2(320f, 180f) / Zoom;
        public int Width => (int)Math.Ceiling(320f / Zoom);
        public int Height => (int)Math.Ceiling(180f / Zoom);

        public Vector2 Position;
        public float Zoom;
        public CameraFocus(Vector2 top_left, float zoom)
        {
            Position = top_left;
            Zoom = zoom;
        }
        public CameraFocus(Level level) : this(level.Camera.Position, level.Zoom) { }

        public static CameraFocus FromCenter(Vector2 center, float zoom)
        {
            return new CameraFocus(center - new Vector2(160f, 90f) / zoom, zoom);
        }

        public static CameraFocus FromFocusPoint(Vector2 cameraPosition, Vector2 zoomFocusPoint, float zoom)
        {
            return new CameraFocus(cameraPosition + zoomFocusPoint - new Vector2(160f, 90f) / zoom, zoom);
        }

        public static CameraFocus FullZoomEval(Level level, bool usePlayerHashSet = false)
        {
            return FullZoomEval(level.Tracker.GetEntity<Player>(), level, usePlayerHashSet);
        }

        public static CameraFocus FullZoomEval(Player player, bool usePlayerHashSet = false)
        {
            return FullZoomEval(player, player.SceneAs<Level>(), usePlayerHashSet);
        }
        public static CameraFocus FullZoomEval(Player player, Level level, bool usePlayerHashSet = false)
        {
            float orig = level.Zoom;
            level.Zoom = player.GetCameraZoomBounds(level, usePlayerHashSet).Nearest;
            Vector2 cameraPos = player.CameraTarget;
            CameraFocus focus = new CameraFocus(cameraPos, level.Zoom);
            level.Zoom = orig;
            return focus;
        }
        public static CameraFocus FullZoomEvalLoading(Player player, Level level, bool usePlayerHashSet = false)
        {
            float orig = level.Zoom;
            level.Zoom = player.GetCameraZoomUnsafe(level, usePlayerHashSet).Nearest;
            Vector2 cameraPos = player.CameraTarget;
            CameraFocus focus = new CameraFocus(cameraPos, level.Zoom);
            level.Zoom = orig;
            return focus;
        }
        public static CameraFocus FromZoomEval(Level level, bool usePlayerHashSet = false)
        {
            Player player = level.Tracker?.GetEntity<Player>();
            return CameraFocusTarget.GetOffsetFocus(level, player.Position, player.GetCameraZoomBounds(usePlayerHashSet));
        }

        public static CameraFocus FromZoomEval(Player player, bool usePlayerHashSet = false)
        {
            return CameraFocusTarget.GetOffsetFocus(player.SceneAs<Level>(), player.Position, player.GetCameraZoomBounds(usePlayerHashSet));
        }

        public static CameraFocus FromZoomEvalAtPoint(Level level, Vector2 worldPosition)
        {
            Player player = level.Tracker?.GetEntity<Player>();
            return CameraFocusTarget.GetOffsetFocus(level, worldPosition, player.GetCameraZoomBounds());
        }

        public CameraFocus Lerp(CameraFocus other, float t) => new CameraFocus(
            Vector2.Lerp(Position, other.Position, t),
            LerpZoom(Zoom, other.Zoom, t)
        //(Zoom * other.Zoom) / (other.Zoom + t * (Zoom - other.Zoom))
        );
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpZoom(float start, float end, float t) => start * end / (end + t * (start - end));

        public static implicit operator CameraFocusWrapper(CameraFocus self) => new CameraFocusWrapper(self);
    }
    public class CameraFocusWrapper : ICameraFocusSource
    {
        public CameraFocus TrueFocus;
        public CameraFocus CameraFocus => TrueFocus;

        public CameraFocusWrapper(CameraFocus trueFocus)
        {
            TrueFocus = trueFocus;
        }
    }
    public interface ICameraFocusSource
    {
        public CameraFocus CameraFocus { get; }
    }


    [Tracked]
    [CustomEntity("ExCameraDynamics/CameraReferenceFrame")]
    public class CameraReferenceFrame : Entity, ICameraFocusSource
    {
        public EntityID ID;
        public string EasyKey;

        public float Zoom = 1.0f;
        public Vector2 AbsoluteCameraPosition
        {
            get
            {
                return Position - new Vector2(160f, 90f) / Zoom;
            }
            set
            {
                Position = value + new Vector2(160f, 90f) / Zoom;
            }
        }
        public CameraFocus CameraFocus
        {
            get
            {
                return new CameraFocus(AbsoluteCameraPosition, Zoom);
            }
        }

        public CameraReferenceFrame(EntityData data, Vector2 offset, EntityID id) : base(offset + data.Position)
        {
            ID = id;
            Zoom = data.Float("zoom", 1.0f);
            EasyKey = data.Attr("easyKey", ID.ToString());
        }

        public static CameraReferenceFrame GetFromEasyKey(Level level, string easyKey)
        {
            foreach (CameraReferenceFrame frame in level.Tracker.GetEntities<CameraReferenceFrame>())
            {
                if (frame.EasyKey == easyKey) return frame;
            }
            return null;
        }
    }

    public class CameraReferenceFrameComponent : Component, ICameraFocusSource
    {
        public Vector2 Focus;

        public float Zoom = 1.0f;

        public CameraReferenceFrameComponent(Vector2 focus, float zoom) : base(false, false)
        {
            Focus = focus;
            Zoom = zoom;
        }

        public Vector2 CameraPosition
        {
            get
            {
                return Focus - new Vector2(160f, 90f) / Zoom;
            }
            set
            {
                Focus = value + new Vector2(160f, 90f) / Zoom;
            }
        }

        public Vector2 AbsoluteFocus
        {
            get
            {
                return (Entity?.Position ?? Vector2.Zero) + Focus;
            }
            set
            {
                Focus = value - (Entity?.Position ?? Vector2.Zero);
            }
        }

        public Vector2 AbsoluteCameraPosition
        {
            get
            {
                return AbsoluteFocus - new Vector2(160f, 90f) / Zoom;
            }
        }

        public CameraFocus CameraFocus
        {
            get
            {
                return new CameraFocus(AbsoluteCameraPosition, Zoom);
            }
        }
    }
}
