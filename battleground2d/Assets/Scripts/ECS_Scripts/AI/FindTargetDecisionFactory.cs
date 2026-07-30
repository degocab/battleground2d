    /// <summary>
    /// FindTarget + Broken                 -> Retreat
    /// FindTarget + Pressured/Collapsing    -> CautiousMove (fall back while searching)
    /// FindTarget + anything else           -> Search
    /// </summary>
    public static class FindTargetDecisionFactory
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
                    return FormationBehaviors.Search();
            }
        }
    }
