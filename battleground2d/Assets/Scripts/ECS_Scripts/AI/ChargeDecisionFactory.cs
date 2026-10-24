    /// <summary>
    /// Attack + Broken       -> Retreat
    /// Attack + Collapsing   -> FightingWithdrawal
    /// Attack + anything else -> NormalCharge
    /// </summary>
    public static class ChargeDecisionFactory
    {
        public static FormationBehavior Process(FormationCaptainState state)
        {
            if (state == FormationCaptainState.Broken)
            {
                return FormationBehaviors.Retreat();
            }

            if (state == FormationCaptainState.Collapsing)
            {
                return FormationBehaviors.FightingWithdrawal();
            }

            return FormationBehaviors.NormalCharge();
        }
    }
