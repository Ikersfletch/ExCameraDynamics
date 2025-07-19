using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Triggers;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace ExtendedCameraDynamics.Code.Triggers
{
    [CustomEntity("ExCameraDynamics/CameraFrameTargetTrigger")]
    public class CameraFrameTargetTrigger : Trigger
    {
        [TrackedAs(typeof(CameraTargetTrigger))]
        private class CameraMover : CameraTargetTrigger
        {
            public ICameraFocusSource Focus;
            public CameraZoomer Zoomer;
            public CameraMover(EntityData data, Vector2 offset) : base(data, offset) {}
            public override void OnStay(Player player)
            {
                if (string.IsNullOrEmpty(DeleteFlag) || !SceneAs<Level>().Session.GetFlag(DeleteFlag) || Focus == null)
                {
                    player.CameraAnchor = Focus.CameraFocus.Center;
                    float t = MathHelper.Clamp(LerpStrength * GetPositionLerp(player, PositionMode), 0f, 1f);
                    Zoomer.T = t;
                    player.CameraAnchorLerp = Vector2.One * t;
                    player.CameraAnchorIgnoreX = YOnly;
                    player.CameraAnchorIgnoreY = XOnly;
                }
            }
        }

        private class CameraZoomer : CameraZoomTrigger
        {
            public float T = 1f;
            public ICameraFocusSource Focus;

            public CameraZoomer(Vector2 position, int width, int height, float start_zf, float end_zf, Boundary boundary = Boundary.SetsNearest, Mode zoom_mode = Mode.Start, string delete_flag = "") : base(position, width, height, start_zf, end_zf, boundary, zoom_mode, delete_flag)
            {
            }

            public override float GetZoom(Vector2 position)
            {
                return CameraFocus.LerpZoom(EndZF, Focus.CameraFocus.Zoom, T);
            }
        }

        private CameraMover _targetTrigger;
        private CameraZoomer _zoomTrigger;
        private string _easyKey;

        public CameraFrameTargetTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            _easyKey = data.String("easyKey");

            EntityData target_data = new EntityData();

            target_data.Values = new Dictionary<string, object>();
            target_data.Values.Add("lerpStrength", data.Float("lerpStrength"));

            CameraZoomTrigger.Mode lerpMode = data.Enum<CameraZoomTrigger.Mode>("lerpMode", CameraZoomTrigger.Mode.Start);

            switch (lerpMode) {
                case CameraZoomTrigger.Mode.TopToBottom:
                    target_data.Values.Add("positionMode", PositionModes.TopToBottom);
                    break;
                case CameraZoomTrigger.Mode.BottomToTop:
                    target_data.Values.Add("positionMode", PositionModes.BottomToTop);
                    break;
                case CameraZoomTrigger.Mode.LeftToRight:
                    target_data.Values.Add("positionMode", PositionModes.LeftToRight);
                    break;
                case CameraZoomTrigger.Mode.RightToLeft:
                    target_data.Values.Add("positionMode", PositionModes.RightToLeft);
                    break;
                default:
                    target_data.Values.Add("positionMode", PositionModes.NoEffect);
                    break;
            }
            target_data.Values.Add("xOnly", data.Bool("xOnly"));
            target_data.Values.Add("yOnly", data.Bool("yOnly"));

            string delete_flag = data.String("deleteFlag", "");

            target_data.Values.Add("deleteFlag", delete_flag);

            target_data.Width = data.Width;
            target_data.Height = data.Height;
            target_data.Position = data.Position;

            target_data.Nodes = [data.Position + offset];

            _targetTrigger = new CameraMover(target_data, offset);
            _zoomTrigger = new CameraZoomer(data.Position + offset, data.Width, data.Height, data.Float("zoomStart", 1f), data.Float("zoomStart", 1f), CameraZoomTrigger.Boundary.SetsNearest, lerpMode, delete_flag);

            _targetTrigger.Zoomer = _zoomTrigger;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            scene.Add(_targetTrigger, _zoomTrigger);
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            _targetTrigger.Focus = _zoomTrigger.Focus = CameraReferenceFrame.GetFromEasyKey(scene as Level, _easyKey);

            if (_targetTrigger.Focus == null)
            {
                scene.Remove(_zoomTrigger, _targetTrigger);
                _targetTrigger = null;
                _zoomTrigger = null;
            }
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            scene.Remove(_zoomTrigger, _targetTrigger);
        }
    }
}
