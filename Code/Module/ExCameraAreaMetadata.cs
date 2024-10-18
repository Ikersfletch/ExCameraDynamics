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
            if (!(asset?.PathVirtual?.StartsWith("Maps") ?? false)) return null;
            if (!(asset?.TryDeserialize(out ExCameraYaml meta) ?? false)) return null;
            return meta?.ExCameraMetaData;
        }
        public bool EnableExtendedCamera { get; set; } = false;
        public float RestingZoomFactor { get; set; } = 1f;
        public void FillInSession()
        { 
            if (EnableExtendedCamera)
            {
                CameraZoomHooks.Hook();
                CameraZoomHooks.SetRestingZoomFactor(RestingZoomFactor);
            }
            else
            {
                CameraZoomHooks.Unhook();
                CameraZoomHooks.SetRestingZoomFactor(1f);
            }

        }
    }
}
