using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.ExCameraDynamics.Code.Components
{
    [Tracked]
    public class CameraFocusTarget : Component
    {
        public Vector2 AbsolutePosition
        {
            get => (Entity?.Position ?? Vector2.Zero) + Position;
            set => Position = value - (Entity?.Position ?? Vector2.Zero);
        }
        public Vector2 Position;
        public float Weight;
        public CameraFocusTarget(Vector2 position, float weight) : base(true, true)
        {
            Position = position;
            Weight = weight;
        }

        public void SetOffset(Vector2 offset)
        {
            Position = offset;
        }
        public void SetWeight(float weight)
        {
            Weight = weight;
        }

        public static Vector2 GetWeightedAverage(Level level, Vector2 playerPosition)
        {

            if (level == null) return playerPosition;

            Vector2 runningPos = playerPosition;
            float runningWeight = 1f;

            List<Component> comp = level.Tracker.GetComponents<CameraFocusTarget>();

            if (comp.Count <= 0) return playerPosition;

            foreach (CameraFocusTarget pulls in level.Tracker.GetComponents<CameraFocusTarget>())
            {
                if (!pulls.Active || pulls.Weight <= 0f) continue;

                runningPos += pulls.AbsolutePosition * pulls.Weight;
                runningWeight += pulls.Weight;
            }

            return runningPos / runningWeight;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="level"></param>
        /// <param name="playerPosition"></param>
        /// <param name="minZoomInFactor"></param>
        public static CameraFocus GetFocusTarget(Level level, Vector2 playerPosition, Vector2 weightedAverage, ZoomBounds bounds)
        {
            {
                Vector2 difference = weightedAverage - playerPosition;

                // want to start zooming out when as the player starts hitting the edge of the screen
                float zoom = Math.Clamp(
                    Math.Min(
                        difference.X == 0f ? float.MaxValue : 120f / Math.Abs(difference.X),
                        difference.Y == 0f ? float.MaxValue : 50f / Math.Abs(difference.Y)
                        ),
                    bounds.Furthest, bounds.Nearest // but clamp the zoom within these bounds, because infinite zoomout just isn't feasible, even if this was a 3D game.
                    );
                // want to clamp the camera to the player when they getting within a margin from the edge of the screen once the camera begins to hit its max zoomout
                Vector2 maxStrayDist = new Vector2(110f, 40f) / zoom;// (float)Math.Min(1f, zoom);
                return new CameraFocus(Vector2.Clamp(weightedAverage, playerPosition - maxStrayDist, playerPosition + maxStrayDist), zoom);
            }
        }
        /// <summary>
        /// Gets the value to set Camera.Position to.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="playerPosition"></param>
        /// <param name="minZoomout"></param>
        /// <param name="maxZoomout"></param>
        /// <returns></returns>
        public static CameraFocus GetOffsetFocus(Level level, Vector2 playerPosition, ZoomBounds bounds)
        {
            if (level == null) return new CameraFocus(playerPosition, bounds.Nearest);
            CameraFocus baseTarget = GetFocusTarget(level, playerPosition, GetWeightedAverage(level, playerPosition), bounds);
            //if (baseTarget.Zoom >= 1f) return new CameraFocus(baseTarget.Focus - new Vector2(160f, 90f), 1f);
            return new CameraFocus(baseTarget.Position - new Vector2(160f, 90f) / baseTarget.Zoom, baseTarget.Zoom);
        }
    }
}
