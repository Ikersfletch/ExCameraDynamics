using Celeste.Mod.ExCameraDynamics.Code.Module;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Celeste.Mod.ExCameraDynamics.Code.Hooks
{
    public static partial class CameraZoomHooks
    {
        // All methods that IL.Celeste doesn't do for me.
        // Therefore, these are manual IL Hooks- Monomod does them for me! (Thanks monomod!!)
        private static ILHook IL_Player_get_CameraTarget;
        private static ILHook IL_Player_orig_Update;
        private static ILHook IL_Level_orig_LoadLevel;
        private static ILHook IL_TalkComponentUI_Render;
        private static ILHook IL_Parallax_orig_Render;
        //private static ILHook IL_AscendManager_Streaks_ctor;
        private static ILHook IL_AscendManager_Streaks_Render;
        //private static ILHook IL_AscendManager_Clouds_ctor;
        private static ILHook IL_AscendManager_Clouds_Render;
        private static ILHook IL_AscendManager_Fader_Render;
        private static ILHook IL_AscendManager_Routine;
        private static ILHook IL_BadelineBoost_BoostRoutine;
        private static ILHook IL_FlingBird_DoFlingRoutine;
        private static ILHook IL_Lookout_LookRoutine;
        private static ILHook IL_Cassette_CollectRoutine;

        // This one works fine.
        private static ILHook IL_Level_ZoomBack;

        // These two don't--and I'm not fixing them, either
        // Instead, CameraFocus methods should be used to directly move & zoom the camera itself.
        private static ILHook IL_Level_ZoomAcross;
        private static ILHook IL_Level_ZoomTo;

        // Alters the transition routine to properly interpolate the camera
        private static ILHook IL_Level_TransitionRoutine;

        // Enables the "focus_camera" text command in language files:
        // { focus_camera <string easyKey> <float duration> }
        private static ILHook IL_Textbox_RunRoutine;

        // If the hooks are currently active.
        private static bool hooks_enabled = false;
        public static bool HooksEnabled => hooks_enabled;

        // Enables all hooks.
        public static void Hook()
        {
            if (hooks_enabled) return;
            CreateHooks();
        }
        // Disables all hooks.
        public static void Unhook()
        {
            if (!hooks_enabled) return;
            DestroyHooks();
        }
        // Actually hooks the methods. This one is private for a reason.
        private static void CreateHooks()
        {
            if (ExCameraRemoveTouhoesZoomoutPrivileges.TouhoeCheck()) {
                return;
            }
            hooks_enabled = true;
            IL_Player_get_CameraTarget = new ILHook(typeof(Player).GetProperty("CameraTarget", BindingFlags.Instance | BindingFlags.Public).GetAccessors(false)[0], PlayerCameraTarget);
            IL_Player_orig_Update = new ILHook(typeof(Player).GetMethod("orig_Update", BindingFlags.Instance | BindingFlags.Public), PlayerCameraInterpolation);
            IL_Level_orig_LoadLevel = new ILHook(typeof(Level).GetMethod("orig_LoadLevel", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), Level_LoadLevel_orig);
            IL_TalkComponentUI_Render = new ILHook(typeof(TalkComponent.TalkComponentUI).GetMethod("Render"), CorrectTalkUI);
            IL_Parallax_orig_Render = new ILHook(typeof(Parallax).GetMethod("orig_Render"), Parallax_orig_Render);
            //IL_AscendManager_Streaks_ctor = new ILHook(typeof(AscendManager.Streaks).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(AscendManager) }, null), AscendManager_Streaks_ctor);
            IL_AscendManager_Streaks_Render = new ILHook(typeof(AscendManager.Streaks).GetMethod("Render", BindingFlags.Public | BindingFlags.Instance), AscendManager_Streaks_Render);
            //IL_AscendManager_Clouds_ctor = new ILHook(typeof(AscendManager.Clouds).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(AscendManager) }, null), AscendManager_Clouds_ctor);
            IL_AscendManager_Clouds_Render = new ILHook(typeof(AscendManager.Clouds).GetMethod("Render", BindingFlags.Public | BindingFlags.Instance), AscendManager_Clouds_Render);
            IL_AscendManager_Fader_Render = new ILHook(typeof(AscendManager.Fader).GetMethod("Render", BindingFlags.Public | BindingFlags.Instance), AscendManager_Fader_Render);

            IL_AscendManager_Routine = new ILHook(
                typeof(AscendManager).GetNestedTypes(BindingFlags.NonPublic)[1].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                AscendManager_Routine
            );

            IL_BadelineBoost_BoostRoutine = new ILHook(
                typeof(BadelineBoost).GetNestedTypes(BindingFlags.NonPublic)[2].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                BadelineBoost_BoostRoutine
            );

            IL_FlingBird_DoFlingRoutine = new ILHook(
                typeof(FlingBird).GetNestedTypes(BindingFlags.NonPublic)[2].GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance),
                FlingBird_DoFlingRoutine
            );

            IL_Lookout_LookRoutine = new ILHook(
                typeof(Lookout).GetNestedTypes(BindingFlags.NonPublic)[1].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                Lookout_LookRoutine
            );

            IL_Cassette_CollectRoutine = new ILHook(
                typeof(Cassette).GetNestedTypes(BindingFlags.NonPublic)[1].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                Cassette_CollectRoutine
            );

            IL_Level_ZoomTo = new ILHook(
                typeof(Level).GetNestedTypes(BindingFlags.NonPublic)[16].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                Level_ZoomTo
            );

            IL_Level_ZoomAcross = new ILHook(
                typeof(Level).GetNestedTypes(BindingFlags.NonPublic)[17].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                Level_ZoomAcross
            );

            IL_Level_ZoomBack = new ILHook(
                typeof(Level).GetNestedTypes(BindingFlags.NonPublic)[18].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                Level_ZoomBack
            );

            IL_Level_TransitionRoutine = new ILHook(
                typeof(Level).GetNestedTypes(BindingFlags.NonPublic)[2].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                Level_TransitionRoutine
            );

            IL_Textbox_RunRoutine = new ILHook(
                typeof(Textbox).GetNestedTypes(BindingFlags.NonPublic)[0].GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                Textbox_RunRoutine
            );

            On.Celeste.GameplayBuffers.Create += GameplayBuffers_Create;
            IL.Celeste.Level.Render += Level_Render;
            IL.Celeste.Level.IsInCamera += Level_IsInCamera;
            On.Celeste.Level.InsideCamera += Level_InsideCamera;
            On.Celeste.Level.BeforeRender += Level_BeforeRender;
            On.Celeste.Level.WorldToScreen += Level_WorldToScreen;
            On.Celeste.Level.ScreenToWorld += Level_ScreenToWorld;
            IL.Celeste.Level.EnforceBounds += Level_EnforceBounds;
            On.Celeste.Level.ResetZoom += Level_ResetZoom;
            IL.Celeste.DustEdges.BeforeRender += DustEdges_BeforeRender;
            IL.Celeste.LightingRenderer.BeforeRender += LightingRenderer_BeforeRender;
            IL.Celeste.BloomRenderer.Apply += BloomRenderer_Apply;
            IL.Celeste.LightningRenderer.OnRenderBloom += LightningRenderer_RenderBloom;
            On.Celeste.CrystalStaticSpinner.InView += CrystalStaticSpinner_InView;
            IL.Celeste.DisplacementRenderer.BeforeRender += DisplacementRenderer_BeforeRender;
            IL.Celeste.Parallax.Render += Parallax_Render;
            IL.Celeste.RainFG.Render += RainFG_Render;
            IL.Celeste.RainFG.Update += RainFG_Update;
            IL.Celeste.Audio.Position += Audio_Position;
            IL.Celeste.AscendManager.Render += AscendManager_Render;
            On.Celeste.Godrays.Update += Godrays_Update;
            IL.Celeste.Starfield.Render += Starfield_Render;
            IL.Celeste.StardustFG.Render += SimpleParticleRepeat;
            On.Celeste.WindSnowFG.Render += WindSnowFG_Render;
            IL.Celeste.BlackholeBG.BeforeRender += BlackholeBG_BeforeRender;
            IL.Celeste.BlackholeBG.Update += BlackholeBG_Update;
            On.Celeste.StarsBG.Render += StarsBG_Render;
            On.Celeste.Snow.Render += Snow_Render;
            IL.Celeste.FormationBackdrop.Render += FormationBackdrop_Render;
            IL.Celeste.BigWaterfall.Render += BigWaterfall_Render;
            IL.Celeste.BigWaterfall.RenderPositionAtCamera += BigWaterfall_RenderPositionAtCamera;
            IL.Celeste.FinalBossStarfield.Render += FinalBossStarfield_Render;
            IL.Celeste.CoreStarsFG.Render += SimpleParticleRepeat;
            IL.Celeste.ReflectionFG.Render += SimpleParticleRepeat;
            IL.Celeste.MirrorFG.Render += SimpleParticleRepeat;
            IL.Celeste.HeatWave.Render += SimpleParticleRepeat;
            IL.Celeste.NorthernLights.BeforeRender += NorthernLights_BeforeRender;
            IL.Celeste.DreamStars.Render += DreamStars_Render;
            IL.Celeste.DreamStars.Update += DreamStars_Update;
            IL.Celeste.Petals.Render += Petals_Render;
            IL.Celeste.Planets.Render += Planets_Render;
            IL.Celeste.Tentacles.Render += Tentacles_Render;
            IL.Celeste.Tentacles.Update += Tentacles_Update;
            IL.Celeste.Lightning.InView += Lightning_InView;
            IL.Celeste.BirdTutorialGui.Render += BirdTutorialGui_Render;
            IL.Celeste.AngryOshiro.Render += AngryOshiro_Render;
            IL.Celeste.MemorialText.Render += MemorialText_Render;
            IL.Celeste.FancyText.Parse += FancyText_Parse;
            On.Monocle.Camera.CameraToScreen += Camera_CameraToScreen;
            IL.Celeste.SpotlightWipe.Render += SpotlightWipe_Render;
            //IL.Celeste.BackdropRenderer.Render += BackdropRenderer_Render;
            IL.Celeste.SummitGem.BgFlash.Render += SummitGem_BgFlash_Render;
            //IL.Celeste.CameraTargetTrigger.ctor += CameraTargetTrigger_Ctor;
            hooks_enabled = true;
        }


        private static void DestroyHooks()
        {
            hooks_enabled = false;
            IL_Player_get_CameraTarget.Dispose();
            IL_Player_get_CameraTarget = null;

            IL_Player_orig_Update.Dispose();
            IL_Player_orig_Update = null;

            IL_Level_orig_LoadLevel.Dispose();
            IL_Level_orig_LoadLevel = null;

            IL_TalkComponentUI_Render.Dispose();
            IL_TalkComponentUI_Render = null;

            IL_Parallax_orig_Render.Dispose();
            IL_Parallax_orig_Render = null;

            //IL_AscendManager_Streaks_ctor.Dispose();
            //IL_AscendManager_Streaks_ctor = null;

            IL_AscendManager_Streaks_Render.Dispose();
            IL_AscendManager_Streaks_Render = null;

            //IL_AscendManager_Clouds_ctor.Dispose();
            //IL_AscendManager_Clouds_ctor = null;

            IL_AscendManager_Clouds_Render.Dispose();
            IL_AscendManager_Clouds_Render = null;

            IL_AscendManager_Fader_Render.Dispose();
            IL_AscendManager_Fader_Render = null;

            IL_AscendManager_Routine.Dispose();
            IL_AscendManager_Routine = null;

            IL_BadelineBoost_BoostRoutine.Dispose();
            IL_BadelineBoost_BoostRoutine = null;

            IL_FlingBird_DoFlingRoutine.Dispose();
            IL_FlingBird_DoFlingRoutine = null;

            IL_Lookout_LookRoutine.Dispose();
            IL_Lookout_LookRoutine = null;

            IL_Cassette_CollectRoutine.Dispose();
            IL_Cassette_CollectRoutine = null;


            IL_Level_ZoomBack.Dispose();
            IL_Level_ZoomBack = null;
            IL_Level_ZoomTo.Dispose();
            IL_Level_ZoomTo = null;
            IL_Level_ZoomAcross.Dispose();
            IL_Level_ZoomAcross = null;
            IL_Level_TransitionRoutine.Dispose();
            IL_Level_TransitionRoutine = null;

            IL_Textbox_RunRoutine.Dispose();
            IL_Textbox_RunRoutine = null;

            On.Celeste.GameplayBuffers.Create -= GameplayBuffers_Create;
            IL.Celeste.Level.Render -= Level_Render;
            IL.Celeste.Level.IsInCamera -= Level_IsInCamera;
            On.Celeste.Level.InsideCamera -= Level_InsideCamera;
            On.Celeste.Level.BeforeRender -= Level_BeforeRender;
            On.Celeste.Level.WorldToScreen -= Level_WorldToScreen;
            On.Celeste.Level.ScreenToWorld -= Level_ScreenToWorld;
            IL.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
            On.Celeste.Level.ResetZoom -= Level_ResetZoom;
            IL.Celeste.DustEdges.BeforeRender -= DustEdges_BeforeRender;
            IL.Celeste.LightingRenderer.BeforeRender -= LightingRenderer_BeforeRender;
            IL.Celeste.BloomRenderer.Apply -= BloomRenderer_Apply;
            IL.Celeste.LightningRenderer.OnRenderBloom -= LightningRenderer_RenderBloom;
            On.Celeste.CrystalStaticSpinner.InView -= CrystalStaticSpinner_InView;
            IL.Celeste.DisplacementRenderer.BeforeRender -= DisplacementRenderer_BeforeRender;
            IL.Celeste.RainFG.Render -= RainFG_Render;
            IL.Celeste.RainFG.Update -= RainFG_Update;
            IL.Celeste.Audio.Position -= Audio_Position;
            IL.Celeste.AscendManager.Render -= AscendManager_Render;
            On.Celeste.Godrays.Update -= Godrays_Update;
            IL.Celeste.Starfield.Render -= Starfield_Render;
            IL.Celeste.StardustFG.Render -= SimpleParticleRepeat;
            On.Celeste.WindSnowFG.Render -= WindSnowFG_Render;
            IL.Celeste.BlackholeBG.BeforeRender -= BlackholeBG_BeforeRender;
            IL.Celeste.BlackholeBG.Update -= BlackholeBG_Update;
            On.Celeste.StarsBG.Render -= StarsBG_Render;
            On.Celeste.Snow.Render -= Snow_Render;
            IL.Celeste.FormationBackdrop.Render -= FormationBackdrop_Render;
            IL.Celeste.BigWaterfall.Render -= BigWaterfall_Render;
            IL.Celeste.BigWaterfall.RenderPositionAtCamera -= BigWaterfall_RenderPositionAtCamera;
            IL.Celeste.Parallax.Render -= Parallax_Render;
            IL.Celeste.FinalBossStarfield.Render -= FinalBossStarfield_Render;
            IL.Celeste.CoreStarsFG.Render -= SimpleParticleRepeat;
            IL.Celeste.ReflectionFG.Render -= SimpleParticleRepeat;
            IL.Celeste.MirrorFG.Render -= SimpleParticleRepeat;
            IL.Celeste.HeatWave.Render -= SimpleParticleRepeat;
            IL.Celeste.NorthernLights.BeforeRender -= NorthernLights_BeforeRender;
            IL.Celeste.DreamStars.Render -= DreamStars_Render;
            IL.Celeste.DreamStars.Update -= DreamStars_Update;
            IL.Celeste.Petals.Render -= Petals_Render;
            IL.Celeste.Planets.Render -= Planets_Render;
            IL.Celeste.Tentacles.Render -= Tentacles_Render;
            IL.Celeste.Tentacles.Update -= Tentacles_Update;
            IL.Celeste.Lightning.InView -= Lightning_InView;
            IL.Celeste.BirdTutorialGui.Render -= BirdTutorialGui_Render;
            IL.Celeste.AngryOshiro.Render -= AngryOshiro_Render;
            IL.Celeste.MemorialText.Render -= MemorialText_Render;
            IL.Celeste.FancyText.Parse -= FancyText_Parse;
            On.Monocle.Camera.CameraToScreen -= Camera_CameraToScreen;
            IL.Celeste.SpotlightWipe.Render -= SpotlightWipe_Render;
            //IL.Celeste.BackdropRenderer.Render -= BackdropRenderer_Render;
            IL.Celeste.SummitGem.BgFlash.Render -= SummitGem_BgFlash_Render;
            //IL.Celeste.CameraTargetTrigger.ctor -= CameraTargetTrigger_Ctor;
            hooks_enabled = false;
        }
    }
}
