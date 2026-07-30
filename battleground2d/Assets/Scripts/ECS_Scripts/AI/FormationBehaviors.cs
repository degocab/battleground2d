    /// <summary>
    /// Simple named constructors for each behavior. Keep these dumb and
    /// declarative - all the "which behavior for which state" logic lives
    /// in the per-order decision factories, not here.
    /// </summary>
    public static class FormationBehaviors
    {
        public static FormationBehavior NormalAttack()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.NormalAttack,
                Aggression = 1f,
                MoveSpeedMultiplier = 1f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = false
            };
        }

        public static FormationBehavior DefensiveAttack()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.DefensiveAttack,
                Aggression = 0.5f,
                MoveSpeedMultiplier = 0.8f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = true
            };
        }

        public static FormationBehavior FightingWithdrawal()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.FightingWithdrawal,
                Aggression = 0.25f,
                MoveSpeedMultiplier = 0.7f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = true
            };
        }

        public static FormationBehavior Retreat()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.Retreat,
                Aggression = 0f,
                MoveSpeedMultiplier = 1.2f,
                MaintainFormation = false,
                AllowPursuit = false,
                RequestSupport = true
            };
        }

        public static FormationBehavior NormalMove()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.NormalMove,
                Aggression = 0f,
                MoveSpeedMultiplier = 1f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = false
            };
        }

        public static FormationBehavior CautiousMove()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.CautiousMove,
                Aggression = 0f,
                MoveSpeedMultiplier = 0.7f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = true
            };
        }

        public static FormationBehavior HoldPosition()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.HoldPosition,
                Aggression = 0.6f,
                MoveSpeedMultiplier = 0f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = false
            };
        }

        public static FormationBehavior DefensiveHold()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.DefensiveHold,
                Aggression = 0.3f,
                MoveSpeedMultiplier = 0f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = true
            };
        }

        public static FormationBehavior FollowTarget()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.FollowTarget,
                Aggression = 0.5f,
                MoveSpeedMultiplier = 1f,
                MaintainFormation = true,
                AllowPursuit = true,
                RequestSupport = false
            };
        }

        public static FormationBehavior CautiousFollow()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.CautiousFollow,
                Aggression = 0.2f,
                MoveSpeedMultiplier = 0.7f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = true
            };
        }

        public static FormationBehavior NormalCharge()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.NormalCharge,
                Aggression = 1f,
                MoveSpeedMultiplier = 1.5f,
                MaintainFormation = false,
                AllowPursuit = true,
                RequestSupport = false
            };
        }

        public static FormationBehavior NormalMarch()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.NormalMarch,
                Aggression = 0f,
                MoveSpeedMultiplier = 1f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = false
            };
        }

        public static FormationBehavior CautiousMarch()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.CautiousMarch,
                Aggression = 0f,
                MoveSpeedMultiplier = 0.6f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = true
            };
        }

        public static FormationBehavior Search()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.Search,
                Aggression = 0.3f,
                MoveSpeedMultiplier = 0.9f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = false
            };
        }

        public static FormationBehavior Idle()
        {
            return new FormationBehavior
            {
                Type = FormationBehaviorType.None,
                Aggression = 0f,
                MoveSpeedMultiplier = 0f,
                MaintainFormation = true,
                AllowPursuit = false,
                RequestSupport = false
            };
        }
    }
