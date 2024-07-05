namespace SAIN.SAINComponent.Classes.Search
{
    public struct SearchReasonsStruct
    {
        public WantSearchReasonsStruct WantSearchReasons;
        public ENotSearchReason NotSearchReason;
        public EPathCalcFailReason PathCalcFailReason;

        public enum ENotSearchReason
        {
            None,
            DontWantTo,
            PathCalcFailed,
            NullDestination,
        }

        public struct WantSearchReasonsStruct
        {
            public ENotWantToSearchReason NotWantToSearchReason;
            public EWantToSearchReason WantToSearchReason;
            public ECantStartReason CantStartReason;
        }

        public enum ENotWantToSearchReason
        {
            None,
            NullEnemy,
            NullLastKnown,
            EnemyNotSeenOrHeard,
            WontSearchFromAudio,
            CantStart,
            ShallNotSearch,
        }

        public enum EWantToSearchReason
        {
            None,
            BeingStealthy,
            NewSearch_Looting,
            NewSearch_PowerLevel,
            NewSearch_EnemyNotSeen,
            NewSearch_EnemyNotHeard,
            ContinueSearch_Looting,
            ContinueSearch_PowerLevel,
            ContinueSearch_EnemyNotSeen,
            ContinueSearch_EnemyNotHeard,
        }

        public enum ECantStartReason
        {
            None,
            Suppressed,
            EnemyVisible,
            WontSearchForEnemy,
        }
    }
}