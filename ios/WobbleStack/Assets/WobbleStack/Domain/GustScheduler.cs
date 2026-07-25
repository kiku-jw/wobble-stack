namespace WobbleStack.Domain
{
    public sealed class GustScheduler
    {
        private readonly SeededRandom _random;
        private bool _isFirst = true;

        public GustScheduler(uint seed)
        {
            _random = new SeededRandom(seed);
        }

        public GustSample Next(float previousEndsAtSeconds)
        {
            float restSeconds = WobbleStackRules.Lerp(
                WobbleStackRules.GustRestMin,
                WobbleStackRules.GustRestMax,
                _random.NextFloat());
            if (_isFirst && restSeconds < WobbleStackRules.FirstGustRestSeconds)
            {
                restSeconds = WobbleStackRules.FirstGustRestSeconds;
            }

            _isFirst = false;
            float durationSeconds = WobbleStackRules.Lerp(
                WobbleStackRules.GustDurationMin,
                WobbleStackRules.GustDurationMax,
                _random.NextFloat());
            float force = WobbleStackRules.SampleGustForce(_random.NextFloat());
            int direction = _random.NextFloat() < 0.5f ? -1 : 1;
            float startsAtSeconds = previousEndsAtSeconds + restSeconds;
            return new GustSample(restSeconds, durationSeconds, force, direction, startsAtSeconds);
        }
    }
}
