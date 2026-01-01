using System;
using System.Net;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.ExCameraDynamics.Code.Interop;

[ModImportName("MotionSmoothing")]
public static class MotionSmoothingImports
{
    public static Func<VirtualRenderTarget, VirtualRenderTarget> GetResizableBuffer;

	public static Action ReloadLargeTextures;
}