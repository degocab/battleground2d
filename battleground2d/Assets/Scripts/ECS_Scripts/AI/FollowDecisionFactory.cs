    /// <summary>
    /// Follow + Broken                 -> Retreat
    /// Follow + Pressured/Collapsing    -> CautiousFollow
    /// Follow + anything else           -> FollowTarget
    /// </summary>
    public static class FollowDecisionFactory
    {
        public static FormationBehavior Process(FormationCaptainState state)
        {
            switch (state)
            {
                case FormationCaptainState.Broken:
                    return FormationBehaviors.Retreat();

                case FormationCaptainState.Collapsing:
                case FormationCaptainState.Pressured:
                    return FormationBehaviors.CautiousFollow();

                default:
                    return FormationBehaviors.FollowTarget();
            }
        }
    }
