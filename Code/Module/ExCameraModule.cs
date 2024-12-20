using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.ExCameraDynamics.Code.Module;
using Monocle;
using MonoMod.ModInterop;
using System;
namespace Celeste.Mod.ExCameraDynamics
{
    public class ExCameraModule : EverestModule
    {
        public static ExCameraModule Instance;
        public ExCameraModule()
        {
            Instance = this;
        }
        public void LoadCameraIntoSession(Session session)
        {
            if (session == null)
            {
                CameraZoomHooks.Unhook();
                return;
            }   

            ExCameraAreaMetadata meta = ExCameraAreaMetadata.TryGetCameraMetadata(session);

            if (meta == null)
            {
                // The camera shouldn't be loaded- unload it if needed.
                CameraZoomHooks.Unhook();
            } 
            else
            {
                meta.FillInSession();
            }
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            bool loaded = CameraZoomHooks.HooksEnabled;
            if (isFromLoader)
                try
                {
                    LoadCameraIntoSession(level.Session);
                    CameraZoomHooks.MostRecentTriggerBounds = null;

                } catch
                {
                    // undo the hooks that did load if there was a failure
                    CameraZoomHooks.Unhook();
                    throw;
                }
        }
        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            CameraZoomHooks.Unhook();
        }

        public override void Load()
        {
            typeof(ExCameraInterop).ModInterop();
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            Everest.Events.Level.OnExit += Level_OnExit;
        }


        public override void Unload()
        {
            CameraZoomHooks.Unhook();
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
            Everest.Events.Level.OnExit -= Level_OnExit;
        }
    }
}
