namespace WobbleStack.Domain
{
    public sealed class GustScheduler
    {
        private readonly SeededRandom _random;
        private readonly DifficultyProfile _profile;
        private bool _isFirst = true;

        public GustScheduler(uint seed, DifficultyId difficulty)
        {
            _random = new SeededRandom(seed);
            _profile = WobbleStackRules.GetDifficultyProfile(difficulty);
        }

        public GustSample Next(float previousEndsAtSeconds)
        {
            float restSeconds = WobbleStackRules.Lerp(_profile.RestMin, _profile.RestMax, _random.NextFloat());
            if (_isFirst && restSeconds < WobbleStackRules.FirstGustRestSeconds)
            {
                restSeconds = WobbleStackRules.FirstGustRestSeconds;
            }

            _isFirst = false;
            float durationSeconds = WobbleStackRules.Lerp(_profile.DurationMin, _profile.DurationMax, _random.NextFloat());
            float force = WobbleStackRules.Lerp(_profile.ForceMin, _profile.ForceMax, _random.NextFloat());
            int direction = _random.NextFloat() < 0.5f ? -1 : 1;
            float startsAtSeconds = previousEndsAtSeconds + restSeconds;
            return new GustSample(restSeconds, durationSeconds, force, direction, startsAtSeconds);
        }
    }
}
