    /// <summary>
    /// March + Broken                 -> Retreat
    /// March + Pressured/Collapsing    -> CautiousMarch
    /// March + anything else           -> NormalMarch
    /// </summary>
    public static class MarchDecisionFactory
    {
        public static FormationBehavior Process(FormationCaptainState state)
        {
            switch (state)
            {
                case FormationCaptainState.Broken:
                    return FormationBehaviors.Retreat();

                case FormationCaptainState.Collapsing:
                case FormationCaptainState.Pressured:
                    return FormationBehaviors.CautiousMarch();

                default:
                    return FormationBehaviors.NormalMarch();
            }
        }
    }
