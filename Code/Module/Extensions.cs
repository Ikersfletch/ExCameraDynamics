using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.ExCameraDynamics.Code.Triggers;
using ExtendedCameraDynamics.Code.Triggers;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.ExCameraDynamics
{
    public static class CalcPlus
    {
        public static int Dot(this Point a, Point b) => a.X * b.X + a.Y * b.Y;
        public static Point Add(this Point a, Point b) => new Point(a.X + b.X, a.Y + b.Y);
        public static Point Sub(this Point a, Point b) => new Point(a.X - b.X, a.Y - b.Y);
        public static Point Mul(this Point a, Point b) => new Point(a.X * b.X, a.Y * b.Y);
        public static Point Mul(this Point a, int b) => new Point(a.X * b, a.Y * b);
        public static Point Neg(this Point a) => new Point(-a.X, -a.Y);
        public static Point Signs(this Point a) => new Point(Math.Sign(a.X), Math.Sign(a.Y));
        public static Point ToPoint(this Vector2 a) => new Point((int)a.X, (int)a.Y);
        public static Point TopLeft(this Rectangle a) => new Point(a.Left, a.Top);
        public static Point TopRight(this Rectangle a) => new Point(a.Right, a.Top);
        public static Point BottomLeft(this Rectangle a) => new Point(a.Left, a.Bottom);
        public static Point BottomRight(this Rectangle a) => new Point(a.Right, a.Bottom);
        public static Rectangle Shifted(this Rectangle a, Point offset) => new Rectangle(a.X + offset.X, a.Y + offset.Y, a.Width, a.Height);
        public static Rectangle Scaled(this Rectangle a, int scale) => new Rectangle(a.X * scale, a.Y * scale, a.Width * scale, a.Height * scale);
        public static Rectangle Buffer(this Rectangle a, int buffer) => new Rectangle(a.X - buffer, a.Y - buffer, a.Width + 2 * buffer, a.Height + 2 * buffer);
        public static Rectangle CreateRect(Vector2 topLeft, Vector2 bottomRight) => CreateRect(topLeft.ToPoint(), bottomRight.ToPoint());
        public static Rectangle CreateRect(Point topLeft, Point bottomRight) => new Rectangle(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        public static Color Lerp(this Color a, Color to, float amt)
        {
            float r = ((float)a.R * (1 - amt)) + ((float)to.R * amt);
            float g = ((float)a.G * (1 - amt)) + ((float)to.G * amt);
            float b = ((float)a.B * (1 - amt)) + ((float)to.B * amt);

            return new Color(r / 255, g / 255, b / 255);
        }
        /// <summary>
        /// Uses Engine.DeltaTime, assumes the timeframe for a lerp is 1/60.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static float GetTDelta(float t)
        {
            return 1f - (float)Math.Pow(1 - t, 60f * Engine.DeltaTime);
        }
        public static float GetTDelta(float t, float expectedDelta, float realDelta)
        {
            /**
             * lerp(A,B,t) = lerp(lerp(A,B,t1),B,t2)
             * find relationship of t, t1, t2.
             * 
             * A * (1-t) + (B * t) = (B * t2) + ( A * (1 - t1) + B * t1 ) * ( 1 - t2 )
             * A * (1-t) + (B * t) = (B * t2) + A * (1 - t1) * ( 1 - t2 ) + B * t1 * ( 1 - t2 )
             * A * (1-t) + (B * t) = (B * t2) + A * (1 - t1 - t2 + t1t2 ) + B * t1 - B * t1 * t2
             * A * (1-t) + (B * t) = (B * t2) + A * (1 - t1 - t2 + t1t2 ) + B * t1 - B * t1 * t2
             * - At + Bt = Bt2 - At1 - At2 + At1t2 + Bt1 - Bt1t2
             * t * (B - A) = t1(B - A) + t2(B - A) + t1t2(A - B)
             * t * (B - A) = t1 * (B - A) + t2 * (B - A) - t1t2 * (B - A)
             * 
             * therefore, for two lerps to be equivalent to one:
             * t = t1 + t2 - t1 * t2
             * 
             * this makes sense, because lerping by 0.5t for both would come up short, as the second lerp is proportional to the shrunken magnitude...
             * 
             * But I need t1 to equal t2, as td:
             * 
             * t = td + td - td * td
             * t = 2 * td - td^2
             * 
             * ... 
             * not helpful.
             * 
             * Magnitude of (A - B) gets multiplied by ( 1 - t ) by lerping.
             * 
             * Magnitude of (A - B) gets multiplied by ( 1 - td ) in the first lerp,
             * Magnitude of (A - B) gets multiplied by ( 1 - td ) in the second lerp.
             * 
             * C * (1 - t) = C * ( 1 - td) * ( 1 - td )
             * 1 - t = 1 - 2td + td^2
             * t = 2td - td^2
             * 
             * Okay, so that's the same result for the t's, so this is the same relationship.
             * 
             * What about an arbitrary number of lerps?
             * 
             * C * (1 - t) = C * ( 1 - td) ^ lerps
             * 
             * let s = 1 - t and sd = 1 - td...
             * 
             * C * s = C * sd ^ lerps
             * 
             * s = sd ^ lerps
             * 
             * lerps = ( designedDeltaTime / realDeltaTime )
             * for Celeste, that's:
             * 
             * lerps = ( [1/60] / [ Engine.DeltaTime ] )
             * or:
             * 
             * lerps = 1 / (60 * Engine.DeltaTime)
             * 
             * but that's not what I'm doing here, soo....
             * 
             * s = sd ^ ( designedDeltaTime / realDeltaTime )
             * 
             * s ^ ( realDeltaTime / designedDeltaTime ) = sd
             * 
             * ( 1 - t ) ^ ( realDeltaTime / designedDeltaTime ) = 1 - td
             * 
             * 1 - ( 1 - t ) ^ ( realDeltaTime / designedDeltaTime ) = td
             * 
             * that's what I'm looking for.
             * 
             * */
            return 1f - MathF.Pow(1 - t, realDelta / expectedDelta);
        }
        private static float Lerp(float A, float B, float t) => A * (1f - t) + B * t;
        public static float LerpDelta(float A, float B, float t) => Lerp(A, B, GetTDelta(t));
        public static float LerpDelta(float A, float B, float t, float expectedDelta, float realDelta) => Lerp(A, B, GetTDelta(t, expectedDelta, realDelta));
        public static Vector2 Lerp(this Vector2 A, Vector2 B, float t) => new Vector2(Lerp(A.X, B.X, t), Lerp(A.Y, B.Y, t));
        public static Vector2 LerpDelta(Vector2 A, Vector2 B, float t) => A.Lerp(B, GetTDelta(t));
        public static Vector2 LerpDelta(Vector2 A, Vector2 B, float t, float expectedDelta, float realDelta) => A.Lerp(B, GetTDelta(t, expectedDelta, realDelta));

        public static void Append<T>(this HashSet<T> set, IEnumerable<T> onto)
        {
            foreach (T t in onto) set.Add(t);
        }


        /// <summary>
        /// Gets the target zoom based on triggers and the like.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static ZoomBounds GetCameraZoomBounds(this Player player, bool usePlayerHashSet = false)
        {
            return player.GetCameraZoomBounds(player.SceneAs<Level>(), usePlayerHashSet);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="level"></param>
        /// <returns>value.X => minimum zoom factor<br></br>value.Y => maximum zoom factor</returns>
        public static ZoomBounds GetCameraZoomBounds(this Player player, Level level, bool usePlayerHashSet = false)
        {
            if (!player.InControl || (level?.InCutscene ?? true)) return new ZoomBounds(level?.Zoom ?? CameraZoomHooks.RestingZoomFactor);

            return GetCameraZoomUnsafe(player, level, usePlayerHashSet);
        }

        public static ZoomBounds GetCameraZoomUnsafe(this Player player, Level level, bool usePlayerHashSet = false)
        {
            if (CameraZoomHooks.TriggerZoomOverride > 0f)
            {
                return new ZoomBounds(CameraZoomHooks.TriggerZoomOverride);
            }

            float zoomNearestFactor = -1f;
            float zoomFurthestFactor = -1f;
            Vector2 playerCenter = player.Center;

            if (usePlayerHashSet)
            {
                // this mimics vanilla behavior with CameraTargetTriggers.
                if (CameraZoomHooks.MostRecentTriggerBounds.HasValue)
                {
                    return CameraZoomHooks.MostRecentTriggerBounds.Value;
                }

                foreach (Trigger baseTrigger in player.triggersInside)
                {
                    CameraZoomTrigger trigger = baseTrigger as CameraZoomTrigger;
                    if (!(trigger?.IsActive(level) ?? false)) continue;
                    if (trigger.ZoomBoundary == CameraZoomTrigger.Boundary.SetsNearest)
                    {
                        if (zoomNearestFactor > 0) continue;
                        zoomNearestFactor = trigger.GetZoom(playerCenter);
                        if (zoomFurthestFactor > 0) break; // both have been set, so stop looping
                        continue;
                    }
                    if (zoomFurthestFactor > 0) continue;
                    zoomFurthestFactor = trigger.GetZoom(playerCenter);
                    if (zoomNearestFactor > 0) break; // both have been set, stop looping
                }
            }
            else
            {
                foreach (CameraZoomTrigger trigger in level.Tracker.Entities[typeof(CameraZoomTrigger)])
                {
                    if (!trigger.IsActive(level) || !trigger.CollideCheck(player)) continue;
                    if (trigger.ZoomBoundary == CameraZoomTrigger.Boundary.SetsNearest)
                    {
                        if (zoomNearestFactor > 0) continue;
                        zoomNearestFactor = trigger.GetZoom(playerCenter);
                        if (zoomFurthestFactor > 0) break; // both have been set, so stop looping
                        continue;
                    }
                    if (zoomFurthestFactor > 0) continue;
                    zoomFurthestFactor = trigger.GetZoom(playerCenter);
                    if (zoomNearestFactor > 0) break; // both have been set, stop looping
                }
            }
            

            return new ZoomBounds(zoomFurthestFactor, zoomNearestFactor);
        }

        public static float GetNearestZoomPossible(this Player player, Level level)
        {
            if (CameraZoomHooks.TriggerZoomOverride > 0f)
            {
                return CameraZoomHooks.TriggerZoomOverride;
            }

            float nearest_so_far = CameraZoomHooks.RestingZoomFactor;
            foreach (CameraZoomTrigger trigger in level.Tracker.Entities[typeof(CameraZoomTrigger)])
            {
                if (!trigger.IsActive(level) || !trigger.CollideCheck(player)) continue;
                if (trigger.ZoomMode == CameraZoomTrigger.Mode.Start)
                {
                    nearest_so_far = Math.Min(nearest_so_far, trigger.ZoomFactorStart);
                    continue;
                }
                nearest_so_far = Math.Min(nearest_so_far, Math.Min(trigger.ZoomFactorEnd, trigger.ZoomFactorStart));
            }
            return nearest_so_far;
        }

        public static float GetTriggerZoomAtPosition(Level level, Vector2 position)
        {
            if (CameraZoomHooks.TriggerZoomOverride > 0f)
            {
                return CameraZoomHooks.TriggerZoomOverride;
            }

            float nearest_so_far = CameraZoomHooks.RestingZoomFactor;
            foreach (CameraZoomTrigger trigger in level.Tracker.Entities[typeof(CameraZoomTrigger)])
            {
                if (!trigger.IsActive(level) || !trigger.CollidePoint(position)) continue;
                if (trigger.ZoomMode == CameraZoomTrigger.Mode.Start)
                {
                    nearest_so_far = Math.Min(nearest_so_far, trigger.ZoomFactorStart);
                    continue;
                }
                nearest_so_far = trigger.GetZoom(position);
            }
            return nearest_so_far;
        }

        public static float GetTriggerSnapSpeed(this Player player)
        {
            return player.CollideFirst<CameraSnapTrigger>()?.SnapSpeed ?? CameraZoomHooks.CameraSnapSpeed;
        }

        /// <summary>
        /// Moves the cursor ahead by one and emits pop.
        /// I use this in place of ILCursor.Remove() for simple value replacements.
        /// </summary>
        /// <param name="cursor"></param>
        public static void PopNext(this ILCursor cursor)
        {
            cursor.Index++;
            cursor.Emit(OpCodes.Pop);
        }

        // IL niceties
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MatchesSequence(Instruction start, params Predicate<Instruction>[] sequence)
        {
            Instruction current = start;
            for (int i = 0; i < sequence.Length; i++)
            {
                if (!sequence[i].Invoke(current))
                {
                    return false;
                }
                current = current.Next;
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReplaceNextFloat(this ILCursor cursor, float target, Func<float> dynamic_delegate)
        {
            cursor.GotoNext(next => next.MatchLdcR4(target));
            cursor.PopNext();
            cursor.EmitDelegate(dynamic_delegate);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReplaceNextInt(this ILCursor cursor, int target, Func<int> dynamic_delegate)
        {
            cursor.GotoNext(next => next.MatchLdcI4(target));
            cursor.PopNext();
            cursor.EmitDelegate(dynamic_delegate);
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach(T t in enumerable) action(t);
        }
    }
}
