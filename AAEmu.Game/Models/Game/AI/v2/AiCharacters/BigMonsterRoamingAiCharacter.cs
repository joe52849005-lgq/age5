﻿using AAEmu.Game.Models.Game.AI.v2.Behaviors.BigMonster;
using AAEmu.Game.Models.Game.AI.v2.Behaviors.Common;
using AAEmu.Game.Models.Game.AI.v2.Framework;

namespace AAEmu.Game.Models.Game.AI.v2.AiCharacters;

public class BigMonsterRoamingAiCharacter : NpcAi
{
    protected override void Build()
    {
        AddBehavior(BehaviorKind.Spawning, new SpawningBehavior());

        AddBehavior(BehaviorKind.Roaming, new RoamingBehavior())
            .SetDefaultBehavior()
            .AddTransition(TransitionEvent.OnAggroTargetChanged, BehaviorKind.BigMonsterAttack)
            .AddTransition(TransitionEvent.OnTalk, BehaviorKind.Talk);

        AddBehavior(BehaviorKind.RunCommandSet, new RunCommandSetBehavior())
            .AddTransition(TransitionEvent.OnAggroTargetChanged, BehaviorKind.BigMonsterAttack)
            .AddTransition(TransitionEvent.OnTalk, BehaviorKind.Talk);

        AddBehavior(BehaviorKind.Talk, new TalkBehavior())
            .AddTransition(TransitionEvent.OnReturnToTalkPos, BehaviorKind.ReturnState)
            .AddTransition(TransitionEvent.OnAggroTargetChanged, BehaviorKind.BigMonsterAttack);

        AddBehavior(BehaviorKind.Alert, new AlertBehavior())
            .AddTransition(TransitionEvent.OnAggroTargetChanged, BehaviorKind.BigMonsterAttack);

        AddBehavior(BehaviorKind.BigMonsterAttack, new BigMonsterAttackBehavior())
            .AddTransition(TransitionEvent.OnNoAggroTarget, BehaviorKind.ReturnState);

        AddBehavior(BehaviorKind.FollowPath, new FollowPathBehavior())
            .AddTransition(TransitionEvent.OnTalk, BehaviorKind.Talk);

        AddBehavior(BehaviorKind.FollowUnit, new FollowUnitBehavior())
            .AddTransition(TransitionEvent.OnAggroTargetChanged, BehaviorKind.BigMonsterAttack)
            .AddTransition(TransitionEvent.OnTalk, BehaviorKind.Talk);

        AddBehavior(BehaviorKind.ReturnState, new ReturnStateBehavior());
        AddBehavior(BehaviorKind.Dead, new DeadBehavior());
        AddBehavior(BehaviorKind.Despawning, new DespawningBehavior());
        AddBehavior(BehaviorKind.Idle, new IdleBehavior());
    }

    // public override void GoToIdle()
    // {
    //     SetCurrentBehavior(BehaviorKind.Roaming);
    // }

    public override void GoToCombat()
    {
        SetCurrentBehavior(BehaviorKind.BigMonsterAttack);
    }
}
