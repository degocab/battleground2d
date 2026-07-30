    /// <summary>
    /// Defend + Broken                 -> Retreat
    /// Defend + Pressured/Collapsing    -> DefensiveHold
    /// Defend + anything else           -> HoldPosition
    /// </summary>
    public static class DefendDecisionFactory
    {
        public static FormationBehavior Process(FormationCaptainState state)
        {
            switch (state)
            {
                case FormationCaptainState.Broken:
                    return FormationBehaviors.Retreat();

                case FormationCaptainState.Collapsing:
                case FormationCaptainState.Pressured:
                    return FormationBehaviors.DefensiveHold();

                default:
                    return FormationBehaviors.HoldPosition();
            }
        }
    }
