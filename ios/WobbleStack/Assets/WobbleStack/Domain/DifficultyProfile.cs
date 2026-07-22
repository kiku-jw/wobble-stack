namespace WobbleStack.Domain
{
    public readonly struct DifficultyProfile
    {
        public DifficultyProfile(
            DifficultyId id,
            string label,
            string note,
            float forceMin,
            float forceMax,
            float restMin,
            float restMax,
            float durationMin,
            float durationMax)
        {
            Id = id;
            Label = label;
            Note = note;
            ForceMin = forceMin;
            ForceMax = forceMax;
            RestMin = restMin;
            RestMax = restMax;
            DurationMin = durationMin;
            DurationMax = durationMax;
        }

        public DifficultyId Id { get; }

        public string Label { get; }

        public string Note { get; }

        public float ForceMin { get; }

        public float ForceMax { get; }

        public float RestMin { get; }

        public float RestMax { get; }

        public float DurationMin { get; }

        public float DurationMax { get; }
    }
}
