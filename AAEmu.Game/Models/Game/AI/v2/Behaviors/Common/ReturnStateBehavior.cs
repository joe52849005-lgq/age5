﻿using System;

using AAEmu.Game.Core.Packets.G2C;
using AAEmu.Game.Models.Game.Models;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Units.Movements;
using AAEmu.Game.Models.Game.Units.Static;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.AI.v2.Behaviors.Common;

public class ReturnStateBehavior : BaseCombatBehavior
{
    private DateTime _timeoutTime;
    private bool _enter;

    public override void Enter()
    {
        // TODO : Autodisable

        if (!Ai.Owner.AggroTable.IsEmpty)
            Ai.Owner.ClearAllAggro();

        Ai.Owner.SetTarget(null);
        // TODO: Ai.Owner.DisableAggro();

        Ai.Owner.IsInBattle = false;
        Ai.Owner.CurrentGameStance = GameStanceType.Relaxed;
        Ai.Owner.CurrentAlertness = MoveTypeAlertness.Idle;
        Ai.Owner.BroadcastPacket(new SCUnitModelPostureChangedPacket(Ai.Owner, Ai.Owner.AnimActionId, false), false);

        // Ai.AiPathPointsRemaining.Clear(); // Remove whatever path we're on
        // Ai.Owner.Simulation.TargetPosition = Vector3.Zero; // And reset expected target

        //var needRestorationOnReturn = true; // TODO: Use params & alertness values
        //if (needRestorationOnReturn)
        // StartSkill RETURN SKILL TYPE
        Ai.Owner.Buffs.AddBuff((uint)BuffConstants.NpcReturn, Ai.Owner);
        if (Ai.Param == null || Ai.Param.RestorationOnReturn)
        {
            Ai.Owner.PostUpdateCurrentHp(Ai.Owner, Ai.Owner.Hp, Ai.Owner.MaxHp, KillReason.Unknown);
            Ai.Owner.Hp = Ai.Owner.MaxHp;
            Ai.Owner.Mp = Ai.Owner.MaxMp;
            Ai.Owner.BroadcastPacket(new SCUnitPointsPacket(Ai.Owner.ObjId, Ai.Owner.Hp, Ai.Owner.Mp, Ai.Owner.HighAbilityRsc), true);
        }

        //var alwaysTeleportOnReturn = false; // TODO: get from params
        //if (alwaysTeleportOnReturn)
        if (Ai.Param is { AlwaysTeleportOnReturn: true })
        {
            OnCompletedReturn();
            return;
        }

        //var goReturnState = true; // TODO: get from params
        //if (!goReturnState)
        if (Ai.Param is { GoReturnState: false })
        {
            OnCompletedReturnNoTeleport();
        }

        _timeoutTime = DateTime.UtcNow.AddSeconds(20);
        _enter = true;
    }

    public override void Tick(TimeSpan delta)
    {
        if (!_enter)
            return; // not initialized yet Enter()

        var moveSpeed = Ai.GetRealMovementSpeed(Ai.Owner.BaseMoveSpeed);
        var moveFlags = Ai.GetRealMovementFlags(moveSpeed);
        moveSpeed *= (delta.Milliseconds / 1000.0);
        Ai.Owner.MoveTowards(Ai.IdlePosition, (float)moveSpeed, moveFlags);

        var distanceToIdle = MathUtil.CalculateDistance(Ai.IdlePosition, Ai.Owner.Transform.World.Position);
        if (distanceToIdle < 1.0f)
        {
            OnCompletedReturnNoTeleport();
            return;
        }

        if (DateTime.UtcNow > _timeoutTime)
            OnCompletedReturn();
    }

    private void OnCompletedReturn()
    {
        var distanceToIdle = MathUtil.CalculateDistance(Ai.IdlePosition, Ai.Owner.Transform.World.Position);
        if (distanceToIdle > 2 * 2)
        {
            Ai.Owner.MoveTowards(Ai.IdlePosition, 1000000.0f);
            Ai.Owner.StopMovement();
        }

        OnCompletedReturnNoTeleport();
    }

    public void OnCompletedReturnNoTeleport()
    {
        // TODO: Handle return signal override
        Ai.GoToIdle();
        // Ai.GoToDefaultBehavior();
    }

    public override void Exit()
    {
        // TODO: Ai.Owner.EnableAggro();
        Ai.Owner.BroadcastPacket(new SCUnitModelPostureChangedPacket(Ai.Owner, Ai.Owner.AnimActionId, true), false);
        Ai.Owner.Buffs.RemoveBuff((uint)BuffConstants.NpcReturn);
        _enter = false;
    }
}
