using Monocle;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.ExCameraDynamics.Code.Interop;

[ModImportName("MotionSmoothing")]
public static class MotionSmoothingImports
{
    public static Func<VirtualRenderTarget, VirtualRenderTarget> GetResizableBuffer;

	public static Action ReloadLargeTextures;
}