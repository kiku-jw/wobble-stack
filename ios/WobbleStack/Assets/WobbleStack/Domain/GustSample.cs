namespace WobbleStack.Domain
{
    public readonly struct GustSample
    {
        public GustSample(float restSeconds, float durationSeconds, float force, int direction, float startsAtSeconds)
        {
            RestSeconds = restSeconds;
            DurationSeconds = durationSeconds;
            Force = force;
            Direction = direction < 0 ? -1 : 1;
            StartsAtSeconds = startsAtSeconds;
            EndsAtSeconds = startsAtSeconds + durationSeconds;
        }

        public float RestSeconds { get; }

        public float DurationSeconds { get; }

        public float Force { get; }

        public int Direction { get; }

        public float StartsAtSeconds { get; }

        public float EndsAtSeconds { get; }
    }
}
