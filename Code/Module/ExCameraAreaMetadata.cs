using Celeste.Mod.ExCameraDynamics.Code.Hooks;

namespace Celeste.Mod.ExCameraDynamics.Code.Module
{

    public class ExCameraYaml
    {
        public ExCameraAreaMetadata ExCameraMetaData { get; set; } = new ExCameraAreaMetadata();
    }
    public class ExCameraAreaMetadata
    {
        public static ExCameraAreaMetadata TryGetCameraMetadata(Session session)
        {
            if (!Everest.Content.TryGet($"Maps/{session.MapData.Filename}.meta", out ModAsset asset))
                return null;
            if (!asset.PathVirtual.StartsWith("Maps")) return null;
            if (!asset.TryDeserialize(out ExCameraYaml meta)) return null;
            if (meta.ExCameraMetaData == null) return null;
            return meta.ExCameraMetaData;
        }
        public bool EnableExtendedCamera { get; set; } = false;
        public void FillInSession()
        { 
            if (EnableExtendedCamera)
            {
                CameraZoomHooks.Hook();
            }
            else
            {
                CameraZoomHooks.Unhook();
            }
        }
    }
}