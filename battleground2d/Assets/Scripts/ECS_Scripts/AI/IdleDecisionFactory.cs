    /// <summary>
    /// Idle + Broken       -> Retreat (formation is falling apart even at rest)
    /// Idle + anything else -> Idle (no-op behavior)
    /// </summary>
    public static class IdleDecisionFactory
    {
        public static FormationBehavior Process(FormationCaptainState state)
        {
            if (state == FormationCaptainState.Broken)
            {
                return FormationBehaviors.Retreat();
            }

            return FormationBehaviors.Idle();
        }
    }
