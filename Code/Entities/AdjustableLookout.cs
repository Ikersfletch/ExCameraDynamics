using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Entities;
using Celeste.Mod.ExCameraDynamics.Code.Hooks;
using Celeste.Mod.ExCameraDynamics.Code.Module;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ExtendedCameraDynamics.Code.Entities
{
    [CustomEntity("ExCameraDynamics/AdjustableLookout")]
    public class AdjustableLookout : Entity
    {
        public class Hud : Entity
        {
            public float MaxZoom;
            public float MinZoom;

            public bool OnlyY;

            public float Easer;

            public float timerUp;

            public float timerDown;

            public float timerLeft;

            public float timerRight;

            public float multUp;

            public float multDown;

            public float multLeft;

            public float multRight;

            public float left;

            public float right;

            public float up;

            public float down;

            public Vector2 aim;

            public float zoomcheck;

            public float inputcheck = 1f;

            public MTexture halfDot = GFX.Gui["dot"].GetSubtexture(0, 0, 64, 32);
            public Hud()
            {
                AddTag(Tags.HUD);
            }

            public override void Update()
            {
                Level level = SceneAs<Level>();
                Vector2 position = level.Camera.Position;
                Rectangle bounds = level.Bounds;
                int cameraWidth = (int)Math.Ceiling(320 / level.Zoom);
                int cameraHeight = (cameraWidth * 9) >> 4;
                bool overlappingLeft  = base.Scene.CollideCheck<LookoutBlocker>(new Rectangle((int)(position.X - 8f), (int)position.Y, cameraWidth, cameraHeight));
                bool overlappingRight = base.Scene.CollideCheck<LookoutBlocker>(new Rectangle((int)(position.X + 8f), (int)position.Y, cameraWidth, cameraHeight));
                bool overlappingUp    = base.Scene.CollideCheck<LookoutBlocker>(new Rectangle((int)position.X, (int)(position.Y - 8f), cameraWidth, cameraHeight));
                bool overlappingDown  = base.Scene.CollideCheck<LookoutBlocker>(new Rectangle((int)position.X, (int)(position.Y + 8f), cameraWidth, cameraHeight));
                left  = Calc.Approach(left, (!overlappingLeft && position.X > (float)(bounds.Left + 2)) ? 1 : 0, Engine.DeltaTime * 8f);
                right = Calc.Approach(right, (!overlappingRight && position.X + (float)cameraWidth < (float)(bounds.Right - 2)) ? 1 : 0, Engine.DeltaTime * 8f);
                up    = Calc.Approach(up, (!overlappingUp && position.Y > (float)(bounds.Top + 2)) ? 1 : 0, Engine.DeltaTime * 8f);
                down  = Calc.Approach(down, (!overlappingDown && position.Y + (float)cameraHeight < (float)(bounds.Bottom - 2)) ? 1 : 0, Engine.DeltaTime * 8f);
                aim = Input.Aim.Value;
                if (aim.X < 0f)
                {
                    multLeft = Calc.Approach(multLeft, 0f, Engine.DeltaTime * 2f);
                    timerLeft += Engine.DeltaTime * 12f;
                }
                else
                {
                    multLeft = Calc.Approach(multLeft, 1f, Engine.DeltaTime * 2f);
                    timerLeft += Engine.DeltaTime * 6f;
                }

                if (aim.X > 0f)
                {
                    multRight = Calc.Approach(multRight, 0f, Engine.DeltaTime * 2f);
                    timerRight += Engine.DeltaTime * 12f;
                }
                else
                {
                    multRight = Calc.Approach(multRight, 1f, Engine.DeltaTime * 2f);
                    timerRight += Engine.DeltaTime * 6f;
                }

                if (aim.Y < 0f)
                {
                    multUp = Calc.Approach(multUp, 0f, Engine.DeltaTime * 2f);
                    timerUp += Engine.DeltaTime * 12f;
                }
                else
                {
                    multUp = Calc.Approach(multUp, 1f, Engine.DeltaTime * 2f);
                    timerUp += Engine.DeltaTime * 6f;
                }

                if (aim.Y > 0f)
                {
                    multDown = Calc.Approach(multDown, 0f, Engine.DeltaTime * 2f);
                    timerDown += Engine.DeltaTime * 12f;
                }
                else
                {
                    multDown = Calc.Approach(multDown, 1f, Engine.DeltaTime * 2f);
                    timerDown += Engine.DeltaTime * 6f;
                }

                if (Input.GrabCheck)
                {
                    zoomcheck = Calc.Approach(zoomcheck, 1f, Engine.DeltaTime * 5f);
                    inputcheck = Calc.Approach(inputcheck, 0f, Engine.DeltaTime * 5f);
                }
                else
                {
                    zoomcheck = Calc.Approach(zoomcheck, 0f, Engine.DeltaTime * 5f);

                    if (inputcheck < 1.0f)
                    {
                        inputcheck = Calc.Approach(inputcheck, 0f, Engine.DeltaTime * 5f);
                    }
                }

                base.Update();
            }
            public override void Render()
            {

                Level level = base.Scene as Level;
                float alpha = Ease.CubeInOut(Easer);
                Color outlineColor = Color.White * alpha;
                int xEdge = (int)(80f * alpha);
                int yEdge = (int)(80f * alpha * 0.5625f);
                int outlineWidth = 8;
                if (level.FrozenOrPaused || level.RetryPlayerCorpse != null)
                {
                    outlineColor *= 0.25f;
                }

                Draw.Rect(xEdge, yEdge, 1920 - xEdge * 2 - outlineWidth, outlineWidth, outlineColor);
                Draw.Rect(xEdge, yEdge + outlineWidth, outlineWidth + 2, 1080 - yEdge * 2 - outlineWidth, outlineColor);
                Draw.Rect(1920 - xEdge - outlineWidth - 2, yEdge, outlineWidth + 2, 1080 - yEdge * 2 - outlineWidth, outlineColor);
                Draw.Rect(xEdge + outlineWidth, 1080 - yEdge - outlineWidth, 1920 - xEdge * 2 - outlineWidth, outlineWidth, outlineColor);


                float h = 1080 - yEdge * 2 - 300;
                float zt = ((320f / level.Zoom) - (320f / MaxZoom)) / ((320f / MinZoom) - (320f / MaxZoom));

                zt = Math.Clamp(zt, 0, 1);

                MTexture arrow = GFX.Gui["towerarrow"];

                Draw.Rect(xEdge + 75f, yEdge + 150, 2, h, Color.Yellow * alpha);
                Draw.Rect(xEdge + 50, yEdge + 150 + (h * zt), 50, 1, Color.Yellow * alpha);
                arrow.DrawJustified(new Vector2(xEdge + 90f, yEdge + 150 + (h * zt)), new Vector2(0f, 0.5f), Color.Yellow * alpha * zoomcheck, 0.3f);

                MTexture summit = GFX.Gui["lookout/summit"];

                summit?.DrawCentered(new Vector2(xEdge + 75f, yEdge + 110), Color.Yellow * alpha, 0.8f);
                summit?.DrawCentered(new Vector2(xEdge + 75f, 1080 - yEdge - 120), Color.Yellow * alpha, 0.4f);

                GFX.Gui["menu/mapsearch"]?.DrawJustified(new Vector2(xEdge + 125f, yEdge + 125), new Vector2(0.5f, 0.5f), Color.Yellow * alpha * inputcheck, 0.5f);
                Input.GuiButton(Input.Grab, Input.PrefixMode.Attached).DrawJustified(new Vector2(xEdge+125f, yEdge+125), new Vector2(0f, 0f), Color.White * alpha * inputcheck, 0.5f);


                if (level.FrozenOrPaused || level.RetryPlayerCorpse != null)
                {
                    return;
                }

                outlineColor = Color.White * (1f - zoomcheck) * alpha;

                float upY = (float)yEdge * up - (float)(Math.Sin(timerUp) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, multUp)) - (1f - multUp) * 12f;
                arrow.DrawCentered(new Vector2(960f, upY), outlineColor * up, 1f, MathF.PI / 2f);


                float downY = 1080f - (float)yEdge * down + (float)(Math.Sin(timerDown) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, multDown)) + (1f - multDown) * 12f;
                arrow.DrawCentered(new Vector2(960f, downY), outlineColor * down, 1f, 3f * MathF.PI / 2f);

                if (!OnlyY)
                {
                    float lbase = left;
                    float lmult = multLeft;
                    float ltimer = timerLeft;
                    float rbase = right;
                    float rmult = multRight;
                    float rtimer = timerRight;
                    if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
                    {
                        lbase = right;
                        lmult = multRight;
                        ltimer = timerRight;
                        rbase = left;
                        rmult = multLeft;
                        rtimer = timerLeft;
                    }

                    float leftX = (float)xEdge * lbase - (float)(Math.Sin(ltimer) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, lmult)) - (1f - lmult) * 12f;
                    arrow.DrawCentered(new Vector2(leftX, 540f), outlineColor * lbase);

                    float rightX = 1920f - (float)xEdge * rbase + (float)(Math.Sin(rtimer) * 18.0 * (double)MathHelper.Lerp(0.5f, 1f, rmult)) + (1f - rmult) * 12f;
                    arrow.DrawCentered(new Vector2(rightX, 540f), outlineColor * rbase, 1f, MathF.PI);
                }


            }
        }

        public TalkComponent talk;

        public Hud hud;

        public Sprite sprite;

        public Tween lightTween;

        public bool interacting;

        public bool onlyY;

        public string animPrefix = "";

        public float MaxZoom = 1f;
        public float MinZoom = 1f;

        private float zoomRange;

        public AdjustableLookout(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            base.Depth = -8500;
            Add(talk = new TalkComponent(new Rectangle(-24, -8, 48, 8), new Vector2(-0.5f, -20f), Interact));
            talk.PlayerMustBeFacing = false;
            onlyY = data.Bool("onlyY");
            MaxZoom = data.Float("maxZoom");
            MinZoom = data.Float("minZoom");

            zoomRange = (1f / MinZoom) - (1f / MaxZoom);

            base.Collider = new Hitbox(4f, 4f, -2f, -4f);
            VertexLight vertexLight = new VertexLight(new Vector2(-1f, -11f), Color.White, 0.8f, 16, 24);
            Add(vertexLight);
            lightTween = vertexLight.CreatePulseTween();
            Add(lightTween);
            Add(sprite = GFX.SpriteBank.Create("lookout"));
            sprite.OnFrameChange = (string s) =>
            {
                if (s.EndsWith("idle") && sprite.CurrentAnimationFrame == sprite.CurrentAnimationTotalFrames - 1)
                {
                    lightTween.Start();
                }
            };
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

            animPrefix = ExCameraInterop.RunCustomLookout(sprite, player, animPrefix);
        }

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
                //sprite.Active = interacting || entity.StateMachine.State != 11;
                if (!sprite.Active)
                {
                    sprite.SetAnimationFrame(0);
                }
            }
        }

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
            if (player.Facing == Facings.Right)
            {
                sprite.Play(animPrefix + "lookRight");
            }
            else
            {
                sprite.Play(animPrefix + "lookLeft");
            }
            CameraZoomHooks.AutomaticZooming = false;

            PlayerSprite playerSprite = player.Sprite;
            PlayerHair hair = player.Hair;
            bool visible = false;
            hair.Visible = false;
            playerSprite.Visible = visible;
            yield return 0.2f;
            Scene.Add(hud = new Hud());
            hud.OnlyY = onlyY;
            hud.MinZoom = MinZoom;
            hud.MaxZoom = MaxZoom;  
            Audio.Play("event:/ui/game/lookout_on");
            while ((hud.Easer = Calc.Approach(hud.Easer, 1f, Engine.DeltaTime * 3f)) < 1f)
            {
                level.ScreenPadding = (int)(Ease.CubeInOut(hud.Easer) * 16f);
                yield return null;
            }

            float accel = 800f;
            float maxspd = 240f;
            Vector2 cameraSpeed = Vector2.Zero;
            Vector2 lastInputDir = Vector2.Zero;
            Vector2 CameraCenter() => level.Camera.Position + new Vector2(160f, 90f) / level.Zoom;
            Vector2 camStartCenter = CameraCenter();


            int zoomInput = 0;
            bool lastGrabCheck = false;
            float zoomT = Calc.Map(1f / level.Zoom, 1f / MaxZoom, 1f / MinZoom, 0f, 1f);

            CameraFocus camStartFocus = new CameraFocus(level);
            while (!Input.MenuCancel.Pressed && !Input.MenuConfirm.Pressed && !Input.Dash.Pressed && !Input.Jump.Pressed && interacting)
            {

                Vector2 inputDir = Input.Aim.Value;

                if (onlyY)
                {
                    inputDir.X = 0f;
                }

                float beforeZoomT = zoomT;


                if (Input.GrabCheck)
                {
                    zoomInput = Math.Sign(inputDir.Y);

                    zoomT += zoomInput * Engine.DeltaTime / zoomRange;
                    zoomT = Math.Clamp(zoomT, 0f, 1f);

                    inputDir.Y = 0f;
                    inputDir.X = 0f;
                }
                else
                {
                    zoomInput = 0;
                }

                if (Math.Sign(inputDir.X) != Math.Sign(lastInputDir.X) || Math.Sign(inputDir.Y) != Math.Sign(lastInputDir.Y) || lastGrabCheck != Input.GrabCheck)
                {
                    Audio.Play("event:/game/general/lookout_move", Position);
                }

                lastInputDir = inputDir;
                lastGrabCheck = Input.GrabCheck;

                if (sprite.CurrentAnimationID != "lookLeft" && sprite.CurrentAnimationID != "lookRight")
                {
                    if (inputDir.X == 0f)
                    {
                        if (inputDir.Y == 0f)
                        {
                            sprite.Play(animPrefix + "looking");
                        }
                        else if (inputDir.Y > 0f)
                        {
                            sprite.Play(animPrefix + "lookingDown");
                        }
                        else
                        {
                            sprite.Play(animPrefix + "lookingUp");
                        }
                    }
                    else if (inputDir.X > 0f)
                    {
                        if (inputDir.Y == 0f)
                        {
                            sprite.Play(animPrefix + "lookingRight");
                        }
                        else if (inputDir.Y > 0f)
                        {
                            sprite.Play(animPrefix + "lookingDownRight");
                        }
                        else
                        {
                            sprite.Play(animPrefix + "lookingUpRight");
                        }
                    }
                    else if (inputDir.X < 0f)
                    {
                        if (inputDir.Y == 0f)
                        {
                            sprite.Play(animPrefix + "lookingLeft");
                        }
                        else if (inputDir.Y > 0f)
                        {
                            sprite.Play(animPrefix + "lookingDownLeft");
                        }
                        else
                        {
                            sprite.Play(animPrefix + "lookingUpLeft");
                        }
                    }
                }





                if (Input.GrabCheck)
                {
                    if (zoomInput == 0)
                    {
                        goto skip_set;
                    }

                    float newZoom = CameraFocus.LerpZoom(MaxZoom, MinZoom, zoomT);
                    float camWidth = 320f / newZoom;
                    float camHeight = 180f / newZoom;

                    Vector2 cameraPos = CameraCenter() - new Vector2(160, 90) / newZoom;

                    List<Entity> blockers = Scene.Tracker.GetEntities<LookoutBlocker>();

                    cameraPos.X = Calc.Clamp(cameraPos.X, level.Bounds.Left, level.Bounds.Right - camWidth);
                    cameraPos.Y = Calc.Clamp(cameraPos.Y, level.Bounds.Top, level.Bounds.Bottom - camHeight);

                    foreach (Entity item in blockers)
                    {
                        if (cameraPos.X + camWidth > item.Left && cameraPos.Y + camHeight > item.Top && cameraPos.X < item.Right && cameraPos.Y < item.Bottom)
                        {
                            zoomT = beforeZoomT;
                            goto skip_set;
                        }
                    }

                    level.ForceCameraTo(new CameraFocus(cameraPos, newZoom));
                skip_set:;
                }
                

                {
                    Vector2 cameraPos = level.Camera.Position;
                    float camWidth = 320f / level.Zoom;
                    float camHeight = 180f / level.Zoom;
                    if (!Input.GrabCheck)
                        cameraSpeed += accel * inputDir * Engine.DeltaTime;

                    if (inputDir.X == 0f)
                    {
                        cameraSpeed.X = Calc.Approach(cameraSpeed.X, 0f, accel * 2f * Engine.DeltaTime);
                    }

                    if (inputDir.Y == 0f)
                    {
                        cameraSpeed.Y = Calc.Approach(cameraSpeed.Y, 0f, accel * 2f * Engine.DeltaTime);
                    }

                    if (cameraSpeed.Length() > maxspd)
                    {
                        cameraSpeed = cameraSpeed.SafeNormalize(maxspd);
                    }

                    Vector2 beforeMoving = cameraPos;
                    List<Entity> blockers = Scene.Tracker.GetEntities<LookoutBlocker>();

                    { // x bounds
                        cameraPos.X += cameraSpeed.X * Engine.DeltaTime;

                        if (cameraPos.X < (float)level.Bounds.Left || cameraPos.X + camWidth > (float)level.Bounds.Right)
                        {
                            cameraSpeed.X = 0f;
                        }

                        cameraPos.X = Calc.Clamp(cameraPos.X, level.Bounds.Left, level.Bounds.Right - camWidth);
                        foreach (Entity item in blockers)
                        {
                            if (cameraPos.X + camWidth > item.Left && cameraPos.Y + camHeight > item.Top && cameraPos.X < item.Right && cameraPos.Y < item.Bottom)
                            {
                                cameraPos.X = beforeMoving.X;
                                cameraSpeed.X = 0f;
                                break;
                            }
                        }
                    }

                    { // y bounds
                        cameraPos.Y += cameraSpeed.Y * Engine.DeltaTime;

                        if (cameraPos.Y < (float)level.Bounds.Top || cameraPos.Y + camHeight > (float)level.Bounds.Bottom)
                        {
                            cameraSpeed.Y = 0f;
                        }

                        cameraPos.Y = Calc.Clamp(cameraPos.Y, level.Bounds.Top, level.Bounds.Bottom - camHeight);
                        foreach (Entity item2 in blockers)
                        {
                            if (cameraPos.X + camWidth > item2.Left && cameraPos.Y + camHeight > item2.Top && cameraPos.X < item2.Right && cameraPos.Y < item2.Bottom)
                            {
                                cameraPos.Y = beforeMoving.Y;
                                cameraSpeed.Y = 0f;
                                break;
                            }
                        }
                    }

                    //level.Camera.Position = cameraPos;
                    level.ForceCameraTo(new CameraFocus(cameraPos, level.Zoom));
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
            float endDistance = (camStartCenter - nowPosition.Center).Length();
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
