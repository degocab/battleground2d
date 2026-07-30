    /// <summary>
    /// Answers exactly one question: "How is my formation doing?"
    /// It never decides what order to execute or what behavior to run.
    /// Tune the thresholds freely; the shape of the method should stay stable.
    /// </summary>
    public static class CaptainStateFactory
    {
        public static FormationCaptainState DetermineState(
            float control,
            float intensity,
            float morale,
            int aliveUnits)
        {
            if (aliveUnits <= 0)
            {
                return FormationCaptainState.Broken;
            }

            if (intensity <= 0.05f)
            {
                return FormationCaptainState.Idle;
            }

            //if (morale <= 0.15f)
            //{
            //    return FormationCaptainState.Broken;
            //}

            if (control <= -0.75f)
            {
                return FormationCaptainState.Collapsing;
            }

            if (control <= -0.25f)
            {
                return FormationCaptainState.Pressured;
            }

            if (control >= 0.65f)
            {
                return FormationCaptainState.Winning;
            }

            if (control >= 0.20f)
            {
                return FormationCaptainState.SlightEdge;
            }

            return FormationCaptainState.Holding;
        }
    }
