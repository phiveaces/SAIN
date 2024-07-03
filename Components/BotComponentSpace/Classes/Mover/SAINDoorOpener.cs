using Comfort.Common;
using EFT;
using EFT.Interactive;
using JetBrains.Annotations;
using SAIN.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

using DoorStruct = GStruct19;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINDoorOpener
    {
        public SAINDoorOpener(BotComponent sain, BotOwner bot)
        {
            Bot = sain;
            this.BotOwner = bot;
        }

        private BotComponent Bot;

        public bool CanOpenDoorNow => _nextPosibleDoorOpenTime > Time.time;

        public List<NavMeshDoorLink> FindDoorsOnPath(NavMeshPath path)
        {
            doorsOnPath.Clear();
            List<NavMeshDoorLink> list = this.BotOwner.CellData.CurrentDoorLinks();
            return null;
        }

        private readonly List<NavMeshDoorLink> doorsOnPath = new List<NavMeshDoorLink>();

        public bool Update()
        {
            if (!SAINPlugin.LoadedPreset.GlobalSettings.General.NewDoorOpening ||
                Bot.EnemyController.AtPeace == true)
            {
                return BotOwner.DoorOpener.Update();
            }

            if (BotOwner.DoorOpener.Interacting)
            {
                BotOwner.Steering.LookToDirection(Bot.LookDirection);
                //this.BotOwner.Steering.SetYAngle(0f);
                if (this._traversingEnd < Time.time)
                {
                    BotOwner.DoorOpener.Interacting = false;
                    if (!Bot.Mover.SprintController.Running)
                    {
                        BotOwner.Mover.MovementResume();
                    }
                }
                return this._lastFrameLink;
            }
            if (this._searchCloseDoorTime < Time.time)
            {
                List<NavMeshDoorLink> list = this.BotOwner.CellData.CurrentDoorLinks();
                this.NearDoor = false;
                DoorStruct? gstruct = this.findDoorToInteract(list, null);
                if (gstruct == null)
                {
                    return true;
                }
                this._currLink = gstruct.Value.LinkDoor;
                this._currentDoor = null;
                this._searchCloseDoorTime = Time.time + 0.25f;
                bool lastFrameLink = true;
                if (gstruct.Value.CurDist < 27.039997f)
                {
                    lastFrameLink = this.method_1(this._currLink);
                }
                this._lastFrameLink = lastFrameLink;
                if (gstruct.Value.CurDist < 16f)
                {
                    Vector3 targetDest;
                    if (Bot.Mover.SprintController.Running)
                    {
                        targetDest = Bot.Mover.SprintController.CurrentCornerDestination();
                    }
                    else
                    {
                        targetDest = BotOwner.Mover.RealDestPoint;
                    }
                    bool wantToOpen;
                    if (!(wantToOpen = this.CheckWantToOpen(targetDest, gstruct.Value)) && list.Count > 1)
                    {
                        gstruct = this.findDoorToInteract(list, new DoorStruct?(gstruct.Value));
                        if (gstruct == null)
                        {
                            return true;
                        }
                        this._currLink = gstruct.Value.LinkDoor;

                        wantToOpen = this.CheckWantToOpen(targetDest, gstruct.Value);
                    }
                    if (wantToOpen)
                    {
                        if (this._currLink.ShallInteract())
                        {
                            if (!this._shallStartInteract)
                            {
                                this._shallStartInteract = true;
                            }
                            this.TraverseDoorLink();
                        }
                        else if (this._refreshWayPeriod < Time.time
                            && !Bot.Mover.SprintController.Running)
                        {
                            this._refreshWayPeriod = Time.time + 2f;
                            this.BotOwner.GoToPoint(this.BotOwner.Mover.LastDestination(), false, -1f, false, false, false, true);
                        }
                    }
                }
            }
            return this._lastFrameLink;
        }

        public void TraverseDoorLink()
        {
            this._currentDoor = this._currLink.Door;
            this.TryInteract();
        }

        private void doDefaultInteract(Door door, EInteractionType Etype)
        {
            BotOwner.GetPlayer.CurrentManagedState.StartDoorInteraction(door, new InteractionResult(Etype), new Action(doorOpenDone));
        }

        private void doorOpenDone()
        {
            //_traversingEnd = 0f;
        }

        private bool shallKickOpen(Door door, EInteractionType Etype)
        {
            if (Bot.Enemy == null)
            {
                return false;
            }
            if (Etype != EInteractionType.Open)
            {
                return false;
            }

            bool shallKick = Bot.Info.PersonalitySettings.General.KickOpenAllDoors;
            if (!shallKick)
            {
                shallKick = Bot.Enemy.TimeSinceSeen < 1f;
            }
            if (shallKick)
            {
                var breakInParameters = door.GetBreakInParameters(Bot.Position);
                return door.BreachSuccessRoll(breakInParameters.InteractionPosition);
            }
            return false;
        }

        public void Interact(Door door, EInteractionType Etype)
        {
            BotOwner.DoorOpener.Interacting = true;

            bool noAnimation = door.interactWithoutAnimation;
            //EDoorState snap = door.Snap;

            if (shallKickOpen(door, Etype) || Etype == EInteractionType.Breach)
            {
                Etype = EInteractionType.Breach;
            }
            else
            {
                door.interactWithoutAnimation = true;
                //door.Snap = EDoorState.None;
            }

            if (Etype == EInteractionType.Breach || ModDetection.ProjectFikaLoaded)
            {
                _traversingEnd = Time.time + 1.5f;
                doDefaultInteract(door, Etype);
                door.interactWithoutAnimation = noAnimation;
                //door.Snap = snap;
                return;
            }

            _traversingEnd = Time.time + 0.45f;
            bool inverted = false;
            if (ShallInvertDoorAngle(door))
            {
                inverted = true;
                door.OpenAngle = -door.OpenAngle;
            }

            EDoorState state = EDoorState.None;
            switch (Etype)
            {
                case EInteractionType.Open:
                    state = EDoorState.Open;
                    break;

                case EInteractionType.Close:
                    state = EDoorState.Shut;
                    break;

                default:
                    break;
            }

            if (state != EDoorState.None)
            {
                //Logger.LogWarning("Opening Door");
                var result = new InteractionResult(Etype);
                door.method_3(state);
                //BotOwner.GetPlayer.vmethod_0(door, result, null);
                Singleton<BotEventHandler>.Instance?.PlaySound(Bot.Player, Bot.Position, 30f, AISoundType.step);
            }
            door.interactWithoutAnimation = noAnimation;
            //door.Snap = snap;

            if (inverted)
            {
                door.OpenAngle = -door.OpenAngle;
            }
        }

        private bool ShallInvertDoorAngle(Door door)
        {
            var interactionParameters = door.GetInteractionParameters(BotOwner.Position);
            if (interactionParameters.AnimationId == door.PushID)
            {
                return false;
            }
            return true;
        }

        public DoorStruct? findDoorToInteract(List<NavMeshDoorLink> list, [CanBeNull] DoorStruct? exclude)
        {
            DoorStruct? result = null;
            float num = 2f;
            for (int i = 0; i < list.Count; i++)
            {
                NavMeshDoorLink navMeshDoorLink = list[i];
                Vector3 a;
                if (navMeshDoorLink.Door.DoorState == EDoorState.Open)
                {
                    a = navMeshDoorLink.MidClose;
                    num = 1f;
                }
                else
                {
                    a = navMeshDoorLink.MidOpen;
                }
                if (exclude == null || navMeshDoorLink.Id != exclude.Value.LinkDoor.Id)
                {
                    float sqrMagnitude = (a - this.BotOwner.Transform.position).sqrMagnitude;
                    if (sqrMagnitude < num)
                    {
                        num = sqrMagnitude;
                        this._currLink = navMeshDoorLink;
                        DoorStruct value = new DoorStruct
                        {
                            LinkDoor = navMeshDoorLink,
                            CurDist = sqrMagnitude
                        };
                        result = new DoorStruct?(value);
                    }
                }
            }
            return result;
        }

        private DoorStruct? findDoors(List<NavMeshDoorLink> list, [CanBeNull] DoorStruct? exclude)
        {
            DoorStruct? result = null;
            float num = 6f;
            for (int i = 0; i < list.Count; i++)
            {
                NavMeshDoorLink navMeshDoorLink = list[i];
                Vector3 a;
                if (navMeshDoorLink.Door.DoorState == EDoorState.Open)
                {
                    a = navMeshDoorLink.MidClose;
                }
                else
                {
                    a = navMeshDoorLink.MidOpen;
                }
                if (exclude == null || navMeshDoorLink.Id != exclude.Value.LinkDoor.Id)
                {
                    float sqrMagnitude = (a - this.BotOwner.Transform.position).sqrMagnitude;
                    if (sqrMagnitude < num)
                    {
                        num = sqrMagnitude;
                        this._currLink = navMeshDoorLink;
                        DoorStruct value = new DoorStruct
                        {
                            LinkDoor = navMeshDoorLink,
                            CurDist = sqrMagnitude
                        };
                        result = new DoorStruct?(value);
                    }
                }
            }
            return result;
        }

        public bool method_1(NavMeshDoorLink link)
        {
            GClass297 gclass = null;
            EDoorState doorState = link.Door.DoorState;
            if (doorState != EDoorState.Shut)
            {
                if (doorState == EDoorState.Open)
                {
                    gclass = link.SegmentClose;
                }
            }
            else
            {
                gclass = link.SegmentOpen;
            }
            if (gclass == null)
            {
                return true;
            }
            Vector3 vector = GClass760.Rotate90(gclass.a - gclass.b, GClass760.SideTurn.right);
            if (Vector3.Dot(vector, this.BotOwner.LookDirection) < 0f)
            {
                vector = -vector;
            }
            return !Vector.IsAngLessNormalized(Vector.NormalizeFastSelf(this.BotOwner.LookDirection), Vector.NormalizeFastSelf(vector), 0.9396926f);
        }

        public void TryInteract()
        {
            float time = Time.time;
            float num = time - this._comeToDoorLast;
            this.BotOwner.Mover.SprintPause(0.5f);
            //this.BotOwner.Mover.MovementPause(pauseTime);

            if (num > 3f)
            {
                _comeToDoorLast = time;
                //return;
            }
            if (!_currentDoor.Operatable)
            {
                // || BotOwner.DoorOpener.CanOpenDoorNow
                return;
            }
            if (_currentDoor.DoorState == EDoorState.Interacting)
            {
                _nextPosibleDoorOpenTime = Time.time + 0.1f;
                return;
            }
            _shallStartInteract = false;
            if (_currentDoor.DoorState != EDoorState.Shut)
            {
                if (_currentDoor.DoorState == EDoorState.Open)
                {
                    _nextPosibleDoorOpenTime = Time.time + 1f;
                    Interact(_currentDoor, EInteractionType.Close);
                }
                return;
            }
            _nextPosibleDoorOpenTime = Time.time + 4f;
            Interact(_currentDoor, EInteractionType.Open);
        }

        // Token: 0x060010AF RID: 4271 RVA: 0x0004CED4 File Offset: 0x0004B0D4
        public bool CheckWantToOpen(Vector3 goTo, DoorStruct infoStruct)
        {
            NavMeshDoorLink linkDoor = infoStruct.LinkDoor;
            Vector3 vector = this.BotOwner.Transform.position + Vector3.up;
            if (Mathf.Abs(vector.y - infoStruct.LinkDoor.Open1.y) >= 0.5f)
            {
                return false;
            }
            this.NearDoor = true;
            if (linkDoor.Door.DoorState == EDoorState.Interacting)
            {
                if (this._nextWaitInteractedDoor < Time.time)
                {
                    this._nextWaitInteractedDoor = Time.time + 3f;
                    this.BotOwner.Mover.SprintPause(1f);
                }
                return false;
            }
            GClass297 gclass = null;
            DoorActionType doorActionType = DoorActionType.none;
            if (linkDoor.Door.DoorState == EDoorState.Open)
            {
                gclass = linkDoor.SegmentClose;
                doorActionType = DoorActionType.wantClose;
                if ((linkDoor.MidClose - vector).sqrMagnitude > 6f)
                {
                    return false;
                }
            }
            if (linkDoor.Door.DoorState == EDoorState.Shut)
            {
                float sqrMagnitude = (linkDoor.MidOpen - vector).sqrMagnitude;
                doorActionType = DoorActionType.wantOpen;
                gclass = linkDoor.SegmentOpen;
                if (sqrMagnitude > 6f)
                {
                    return false;
                }
            }
            if (gclass == null)
            {
                return false;
            }
            DoorActionType doorActionType2 = DoorActionType.none;
            Vector3 v = vector - goTo;
            v.y = 0f;
            Vector3 a = Vector.NormalizeFastSelf(v);
            Vector3 b = a * 0.25f;
            Vector3 b2 = a * 0.5f;
            Vector3 a2 = vector + b;
            Vector3 b3 = goTo - b2;
            if (GClass301.GetCrossPoint(new GClass297(a2, b3), gclass) != null)
            {
                doorActionType2 = doorActionType;
            }
            if (doorActionType2 != DoorActionType.wantClose)
            {
                if (doorActionType2 == DoorActionType.wantOpen)
                {
                    if (linkDoor.Door.DoorState == EDoorState.Shut)
                    {
                        return true;
                    }
                }
            }
            else if (linkDoor.Door.DoorState == EDoorState.Open)
            {
                return true;
            }
            return false;
        }

        public bool NearDoor;
        public bool _lastFrameLink;
        private NavMeshDoorLink _currLink;
        private Door _currentDoor;
        private readonly BotOwner BotOwner;
        private float _nextPosibleDoorOpenTime;
        private float _traversingEnd;
        private float _searchCloseDoorTime;
        private float _comeToDoorLast;
        private float _nextWaitInteractedDoor;
        private bool _shallStartInteract;
        private float _refreshWayPeriod;
    }
}