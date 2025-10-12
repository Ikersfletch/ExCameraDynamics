using Monocle;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.ExCameraDynamics.Code.Module
{
    internal class ExCameraRemoveLilacIslesZoomoutPrivileges : Scene
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool LilacIsleCheck()
        {
            try
            {
                // I don't want to compile against steamworks, just in case other versions would break.
                // So we're doing this instead!!
                // (isn't it wonderful?)

                Assembly steamworks = null;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.GetName().Name == "Steamworks.NET")
                    {
                        steamworks = a;
                        goto TestLilacIsle;
                    }
                }
                // Not using steam version.
                return false;

            TestLilacIsle:


                MethodInfo getSteamID = steamworks?.GetType("Steamworks.SteamUser")?.GetMethod("GetSteamID");
                MethodInfo getAccountID = getSteamID?.ReturnType?.GetMethod("GetAccountID");
                FieldInfo m_AccountID = getAccountID?.ReturnType?.GetField("m_AccountID");

                // maybe wrong assembly? Updated assembly?
                // either way- don't bother.
                if (m_AccountID == null)
                {
                    return false;
                }

                return (uint)(m_AccountID.GetValue(getAccountID.Invoke(getSteamID.Invoke(null, null), null))) == 891063634u;
            }
            catch (Exception _)
            {
                return false;
            }
        }
    }
}
