
    /// <summary>
    /// Pure data returned by the decision factories. Factories never move
    /// units or attack enemies directly - they only describe what should
    /// happen, and existing movement/combat systems execute it.
    /// </summary>
    public struct FormationBehavior
    {
        public FormationBehaviorType Type;

        public float Aggression;
        public float MoveSpeedMultiplier;

        public bool MaintainFormation;
        public bool AllowPursuit;
        public bool RequestSupport;
    }

