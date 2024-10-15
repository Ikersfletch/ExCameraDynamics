using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.ExCameraDynamics.Code.Module;
using Mono.Cecil.Cil;
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
        public void LoadIntoLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
        {
            orig(self, playerIntro, isFromLoader);
            if (isFromLoader)
                LoadCameraIntoSession(self.Session);
        }

        public override void Load()
        {
            typeof(ExCameraInterop).ModInterop();
            On.Celeste.Level.LoadLevel += LoadIntoLevel;
        }

        public override void Unload()
        {
            CameraZoomHooks.Unhook();
            On.Celeste.Level.LoadLevel -= LoadIntoLevel;
        }
    }
}
