    /// <summary>
    /// Answers exactly one question: "Which decision factory handles this
    /// order?" Contains almost no tactical logic of its own - all the real
    /// state-to-behavior mapping lives in the per-order factories below.
    /// </summary>
    public static class TacticalDecisionFactory
    {
        public static FormationBehavior Process(
            OrderType order,
            FormationCaptainState state)
        {
            switch (order)
            {
                case OrderType.Attack:
                    return AttackDecisionFactory.Process(state);

                case OrderType.MoveTo:
                    return MoveDecisionFactory.Process(state);

                case OrderType.Defend:
                    return DefendDecisionFactory.Process(state);

                case OrderType.Follow:
                    return FollowDecisionFactory.Process(state);

                case OrderType.Charge:
                    return ChargeDecisionFactory.Process(state);

                case OrderType.March:
                    return MarchDecisionFactory.Process(state);

                case OrderType.FindTarget:
                    return FindTargetDecisionFactory.Process(state);

                case OrderType.Idle:
                default:
                    return IdleDecisionFactory.Process(state);
            }
        }
    }
