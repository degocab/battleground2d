    /// <summary>
    /// Attack + Holding/SlightEdge/Winning/Idle/Unknown -> NormalAttack
    /// Attack + Pressured                                -> DefensiveAttack
    /// Attack + Collapsing                                -> FightingWithdrawal
    /// Attack + Broken                                    -> Retreat
    /// </summary>
    public static class AttackDecisionFactory
    {
        public static FormationBehavior Process(FormationCaptainState state)
        {
            switch (state)
            {
                case FormationCaptainState.Broken:
                    return FormationBehaviors.Retreat();

                case FormationCaptainState.Collapsing:
                    return FormationBehaviors.FightingWithdrawal();

                case FormationCaptainState.Pressured:
                    return FormationBehaviors.DefensiveAttack();

                default:
                    return FormationBehaviors.NormalAttack();
            }
        }
    }
