    /// <summary>
    /// MoveTo + Broken                 -> Retreat
    /// MoveTo + Pressured/Collapsing    -> CautiousMove
    /// MoveTo + anything else           -> NormalMove
    /// </summary>
    public static class MoveDecisionFactory
    {
        public static FormationBehavior Process(FormationCaptainState state)
        {
            switch (state)
            {
                case FormationCaptainState.Broken:
                    return FormationBehaviors.Retreat();

                case FormationCaptainState.Collapsing:
                case FormationCaptainState.Pressured:
                    return FormationBehaviors.CautiousMove();

                default:
                    return FormationBehaviors.NormalMove();
            }
        }
    }
