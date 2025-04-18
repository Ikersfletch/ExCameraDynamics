using Celeste.Mod.ExCameraDynamics.Code.Components;
using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.ExCameraDynamics.Code.Module;
using Microsoft.Xna.Framework;
using MonoMod.ModInterop;
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

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow)
        {
            CameraZoomHooks.Unhook();
        }

        public override void Load()
        {
            typeof(ExCameraInterop).ModInterop();
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            //Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            Everest.Events.Level.OnExit += Level_OnExit;
        }

        private void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level level, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            // This needs to be called before `Everest.Events.Level.OnLoadLevel`
            // Hence, hooking the method instead of using the event :(
            if (isFromLoader)
            {
                try
                {
                    LoadCameraIntoSession(level.Session);
                    CameraZoomHooks.MostRecentTriggerBounds = null;

                }
                catch
                {
                    // undo the hooks that did load if there was a failure
                    CameraZoomHooks.Unhook();
                    throw;
                }
            }

            orig(level, playerIntro, isFromLoader);

            if (isFromLoader && CameraZoomHooks.HooksEnabled)
            {
                if (level.Tracker.GetEntity<Player>() is Player p)
                {
                    level.ForceCameraTo(CameraFocus.FullZoomEvalLoading(p, level));
                    CameraZoomHooks.AutomaticZooming = true;
                }
            }
        }

        public override void Unload()
        {
            CameraZoomHooks.Unhook();
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            Everest.Events.Level.OnExit -= Level_OnExit;
        }
    }
}
