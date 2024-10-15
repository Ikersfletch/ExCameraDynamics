using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.ExCameraDynamics.Code.Module;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ExtendedCameraDynamics.Code.Entities
{
    [CustomEntity("ExCameraDynamics/ReferenceFrameLookout")]
    public class ReferenceFrameLookout : Entity
    {
        public TalkComponent talk;

        public Lookout.Hud hud;

        public Sprite sprite;

        public Tween lightTween;

        public bool interacting;

        public bool onlyY;

        public List<CameraReferenceFrame> nodes;

        public int node;

        public float nodePercent;

        public string animPrefix = "";

        public string[] referenceFrameEasyKeys;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public ReferenceFrameLookout(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            base.Depth = -8500;
            Add(talk = new TalkComponent(new Rectangle(-24, -8, 48, 8), new Vector2(-0.5f, -20f), Interact));
            talk.PlayerMustBeFacing = false;
            onlyY = data.Bool("onlyY");
            base.Collider = new Hitbox(4f, 4f, -2f, -4f);
            VertexLight vertexLight = new VertexLight(new Vector2(-1f, -11f), Color.White, 0.8f, 16, 24);
            Add(vertexLight);
            lightTween = vertexLight.CreatePulseTween();
            Add(lightTween);
            Add(sprite = GFX.SpriteBank.Create("lookout"));
            sprite.OnFrameChange = [MethodImpl(MethodImplOptions.NoInlining)] (string s) =>
            {
                switch (s)
                {
                    case "idle":
                    case "badeline_idle":
                    case "nobackpack_idle":
                        if (sprite.CurrentAnimationFrame == sprite.CurrentAnimationTotalFrames - 1)
                        {
                            lightTween.Start();
                        }

                        break;
                }
            };

            referenceFrameEasyKeys = data.Attr("easyKeys").Split(',');
            for (int i = 0; i < referenceFrameEasyKeys.Length; i++)
            {
                referenceFrameEasyKeys[i] = referenceFrameEasyKeys[i].Trim();
            }
        }
        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            Level level = scene as Level;

            if (nodes == null)
            {
                nodes = new List<CameraReferenceFrame>(referenceFrameEasyKeys.Length);
            } else
            {
                nodes.Clear();
            }

            for (int i = 0; i < referenceFrameEasyKeys.Length; i ++)
            {
                CameraReferenceFrame frame = CameraReferenceFrame.GetFromEasyKey(level, referenceFrameEasyKeys[i]);
                nodes.Add(frame);
            }
        }
        public override void Removed(Scene scene)
        {
            base.Removed(scene);
            if (interacting)
            {
                Player entity = scene.Tracker.GetEntity<Player>();
                if (entity != null)
                {
                    entity.StateMachine.State = 0;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Interact(Player player)
        {
            if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineAsBadeline || SaveData.Instance.Assists.PlayAsBadeline)
            {
                animPrefix = "badeline_";
            }
            else if (player.DefaultSpriteMode == PlayerSpriteMode.MadelineNoBackpack)
            {
                animPrefix = "nobackpack_";
            }
            else
            {
                animPrefix = "";
            }

            Coroutine coroutine = new Coroutine(LookRoutine(player));
            coroutine.RemoveOnComplete = true;
            Add(coroutine);
            interacting = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void StopInteracting()
        {
            interacting = false;
            sprite.Play(animPrefix + "idle");
        }
        public override void Update()
        {
            if (talk.UI != null)
            {
                talk.UI.Visible = !CollideCheck<Solid>();
            }

            base.Update();
            Player entity = base.Scene.Tracker.GetEntity<Player>();
            if (entity != null)
            {
                sprite.Active = interacting || entity.StateMachine.State != 11;
                if (!sprite.Active)
                {
                    sprite.SetAnimationFrame(0);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IEnumerator LookRoutine(Player player)
        {
            Level level = SceneAs<Level>();
            SandwichLava sandwichLava = Scene.Entities.FindFirst<SandwichLava>();
            if (sandwichLava != null)
            {
                sandwichLava.Waiting = true;
            }

            if (player.Holding != null)
            {
                player.Drop();
            }

            player.StateMachine.State = 11;
            yield return player.DummyWalkToExact((int)X, walkBackwards: false, 1f, cancelOnFall: true);
            if (Math.Abs(X - player.X) > 4f || player.Dead || !player.OnGround())
            {
                if (!player.Dead)
                {
                    player.StateMachine.State = 0;
                }

                yield break;
            }

            Audio.Play("event:/game/general/lookout_use", Position);
            sprite.Play(animPrefix + (player.Facing == Facings.Right ? "lookRight" : "lookLeft"));

            PlayerSprite playerSprite = player.Sprite;
            PlayerHair hair = player.Hair;
            bool visible = false;
            hair.Visible = false;
            playerSprite.Visible = visible;
            yield return 0.2f;
            Scene.Add(hud = new Lookout.Hud());
            hud.TrackMode = nodes != null;
            hud.OnlyY = onlyY;
            nodePercent = 0f;
            node = 0;
            Audio.Play("event:/ui/game/lookout_on");
            while ((hud.Easer = Calc.Approach(hud.Easer, 1f, Engine.DeltaTime * 3f)) < 1f)
            {
                level.ScreenPadding = (int)(Ease.CubeInOut(hud.Easer) * 16f);
                yield return null;
            }

            const float MAX_SPEED = 240f;
            Vector2 lastDir = Vector2.Zero;
            CameraFocus camStartFocus = new CameraFocus(level);

            while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed && interacting)
            {
                Vector2 directionInput = Input.Aim.Value;
                if (onlyY)
                {
                    directionInput.X = 0f;
                }

                if (Math.Sign(directionInput.X) != Math.Sign(lastDir.X) || Math.Sign(directionInput.Y) != Math.Sign(lastDir.Y))
                {
                    Audio.Play("event:/game/general/lookout_move", Position);
                }

                lastDir = directionInput;
                if (sprite.CurrentAnimationID != "lookLeft" && sprite.CurrentAnimationID != "lookRight")
                {
                    if (directionInput.X == 0f)
                    {
                        if (directionInput.Y == 0f)
                        {
                            sprite.Play(animPrefix + "looking");
                        }
                        else if (directionInput.Y > 0f)
                        {
                            sprite.Play(animPrefix + "lookingDown");
                        }
                        else
                        {
                            sprite.Play(animPrefix + "lookingUp");
                        }
                    }
                    else if (directionInput.X > 0f)
                    {
                        if (directionInput.Y == 0f)
                        {
                            sprite.Play(animPrefix + "lookingRight");
                        }
                        else if (directionInput.Y > 0f)
                        {
                            sprite.Play(animPrefix + "lookingDownRight");
                        }
                        else
                        {
                            sprite.Play(animPrefix + "lookingUpRight");
                        }
                    }
                    else if (directionInput.X < 0f)
                    {
                        if (directionInput.Y == 0f)
                        {
                            sprite.Play(animPrefix + "lookingLeft");
                        }
                        else if (directionInput.Y > 0f)
                        {
                            sprite.Play(animPrefix + "lookingDownLeft");
                        }
                        else
                        {
                            sprite.Play(animPrefix + "lookingUpLeft");
                        }
                    }
                }


                {
                    CameraFocus previousFrame = ((node <= 0) ? camStartFocus : nodes[node - 1].CameraFocus);
                    CameraFocus nodeFrame = nodes[node].CameraFocus;
                    float distance = (previousFrame.Center - nodeFrame.Center).Length();
                    if (nodePercent < 0.25f && node > 0)
                    {
                        //Lookout
                        CameraFocus begin = ((node <= 1) ? camStartFocus : nodes[node - 2].CameraFocus).Lerp(previousFrame, 0.75f);
                        CameraFocus end = previousFrame.Lerp(nodeFrame, 0.25f);
                        SimpleCurve simpleCurve = new SimpleCurve(begin.Center, end.Center, previousFrame.Center);
                        float percent = 0.5f + nodePercent / 0.25f * 0.5f;
                        Vector2 center = simpleCurve.GetPoint(percent);
                        level.ForceCameraTo(
                            CameraFocus.FromCenter(
                                center, 
                                CameraFocus.LerpZoom(begin.Zoom, end.Zoom, percent)
                            )
                        );
                    }
                    else if (nodePercent > 0.75f && node < nodes.Count - 1)
                    {
                        CameraFocus begin = previousFrame.Lerp(nodeFrame, 0.75f);
                        CameraFocus end = nodeFrame.Lerp(nodes[node + 1].CameraFocus, 0.25f);
                        SimpleCurve simpleCurve2 = new SimpleCurve(begin.Center, end.Center, nodeFrame.Center);
                        float percent = (nodePercent - 0.75f) / 0.25f * 0.5f;
                        Vector2 center = simpleCurve2.GetPoint(percent);
                        level.ForceCameraTo(
                            CameraFocus.FromCenter(
                                center,
                                CameraFocus.LerpZoom(begin.Zoom, end.Zoom, percent)
                            )
                        );
                    }
                    else
                    {
                        level.ForceCameraTo(previousFrame.Lerp(nodeFrame, nodePercent));
                    }

                    nodePercent -= directionInput.Y * (MAX_SPEED / distance) * Engine.DeltaTime;
                    if (nodePercent < 0f)
                    {
                        if (node > 0)
                        {
                            node--;
                            nodePercent = 1f;
                        }
                        else
                        {
                            nodePercent = 0f;
                        }
                    }
                    else if (nodePercent > 1f)
                    {
                        if (node < nodes.Count - 1)
                        {
                            node++;
                            nodePercent = 0f;
                        }
                        else
                        {
                            nodePercent = 1f;
                        }
                    }

                    float currentDistance = 0f;
                    float totalDistance = 0f;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        float interNodeDistance = (((i == 0) ? camStartFocus.Center : nodes[i - 1].CameraFocus.Center) - nodes[i].CameraFocus.Center).Length();
                        totalDistance += interNodeDistance;
                        if (i < node)
                        {
                            currentDistance += interNodeDistance;
                        }
                        else if (i == node)
                        {
                            currentDistance += interNodeDistance * nodePercent;
                        }
                    }

                    hud.TrackPercent = currentDistance / totalDistance;
                }

                yield return null;
            }

            PlayerSprite playerSprite2 = player.Sprite;
            PlayerHair hair2 = player.Hair;
            visible = true;
            hair2.Visible = true;
            playerSprite2.Visible = visible;
            sprite.Play(animPrefix + "idle");
            Audio.Play("event:/ui/game/lookout_off");
            while ((hud.Easer = Calc.Approach(hud.Easer, 0f, Engine.DeltaTime * 3f)) > 0f)
            {
                level.ScreenPadding = (int)(Ease.CubeInOut(hud.Easer) * 16f);
                yield return null;
            }

            CameraFocus nowPosition = new CameraFocus(level);
            float endDistance = (camStartFocus.Center - nowPosition.Center).Length();
            if (endDistance > 600f)
            {
                new FadeWipe(Scene, wipeIn: false).Duration = 0.5f;
                float mult = 64f / endDistance;
                for (float wipePercent = 0f; wipePercent < 1f; wipePercent += Engine.DeltaTime / 0.5f)
                {
                    level.ForceCameraTo(nowPosition.Lerp(camStartFocus, Ease.CubeIn(Math.Clamp(wipePercent * mult, 0f, 1f))));
                    yield return null;
                }
                level.ForceCameraTo(camStartFocus.Lerp(nowPosition, 32f / endDistance));
                new FadeWipe(Scene, wipeIn: true);
            }

            Audio.SetMusicParam("escape", 0f);
            level.ScreenPadding = 0f;
            level.ResetZoom();
            Scene.Remove(hud);
            interacting = false;
            player.StateMachine.State = 0;
            yield return null;
        }

        public override void SceneEnd(Scene scene)
        {
            base.SceneEnd(scene);
        }
    }
}
