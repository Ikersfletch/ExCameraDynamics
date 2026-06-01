using Celeste.Mod;

namespace ExtendedCameraDynamics.Code.Module
{
    public class ExCameraSettings : EverestModuleSettings
    {
        public enum BufferMode { 
            Dynamic,
            Static1440p,
            Static1080p,
            Static720p
        }

        public enum OffGrid
        {
            Subtle,
            ActiveZoom,
            ZoomIn,
            Never,
            Always,
        }

        [SettingSubText("Dynamic (default): Buffers are resized dynamically. Only uses memory as needed.\nCan cause flickering issues when stressed.\n\nStatic1440p: Force buffers to 2560x1440, or 1440p.\n\nStatic1080p: Force buffers to 1920x1080, or 1080p (Breaks Zoom < 0.1667)\n\nStatic720p: Force buffers to 1280x720, or 720p (Breaks Zoom < 0.25)")]
        public BufferMode BufferResizing { get; set; } = BufferMode.Dynamic;

        [SettingSubText("Dislocates the pixel grid to reduce apparent jitter on gameplay sprites,\nbut can cause extra jitter on backdrops.\n\nSubtle (default): when zoomed in OR actively zooming.\n\nActive Zoom: Only when actively zooming in or out.\n\nZoomIn: Only when zoomed in.\n\nNever: Always keep the pixels aligned to the screen. Best visual compatibility.\n\nAlways: Always use off-grid rendering. Can cause artifacts at the edges of the screen.")]
        public OffGrid OffGridRendering { get; set; } = OffGrid.Subtle;
    }
}
