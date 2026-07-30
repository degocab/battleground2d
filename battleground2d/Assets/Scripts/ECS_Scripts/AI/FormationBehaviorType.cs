    /// <summary>
    /// What the formation will actually do. Distinct from OrderType,
    /// which is only what the commander requested.
    /// Keep this list small; add entries only when a decision factory
    /// genuinely needs a new, distinct behavior.
    /// </summary>
    public enum FormationBehaviorType
    {
        None,

        NormalAttack,
        DefensiveAttack,
        FightingWithdrawal,

        NormalMove,
        CautiousMove,

        HoldPosition,
        DefensiveHold,

        FollowTarget,
        CautiousFollow,

        NormalCharge,

        NormalMarch,
        CautiousMarch,

        Search,

        Retreat
    }
