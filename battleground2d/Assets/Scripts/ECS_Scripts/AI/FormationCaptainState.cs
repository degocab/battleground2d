    /// <summary>
    /// Represents how the formation is currently doing.
    /// This is answered purely from battlefield data (control, intensity,
    /// morale, alive units) and knows nothing about the current order.
    /// </summary>
    public enum FormationCaptainState
    {
        Idle,
        Holding,
        SlightEdge,
        Winning,
        Pressured,
        Collapsing,
        Broken,
        Unknown
    }


//public enum FormationCaptainState : byte
//{
//    Idle = 0,
//    Holding = 1,
//    Pressured = 2,
//    Winning = 3,
//    Collapsing = 4,
//    Broken = 5,
//    SlightEdge = 6,
//    Unknown = 7
//}