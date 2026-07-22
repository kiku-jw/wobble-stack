namespace WobbleStack.Domain
{
    public static class WobbleStackRules
    {
        public const float GravityScale = 0.00105f;
        public const float MaxPlatformAngle = 0.46f;
        public const float CounterAuthority = 0.72f;
        public const int MinCreatureCount = 3;
        public const int MaxCreatureCount = 5;

        private static readonly DifficultyProfile GentleProfile = new DifficultyProfile(
            DifficultyId.Gentle,
            "Gentle",
            "Light gusts · room to recover",
            0.000025f,
            0.00005f,
            3.2f,
            5f,
            3.6f,
            4.6f);

        private static readonly DifficultyProfile NormalProfile = new DifficultyProfile(
            DifficultyId.Normal,
            "Normal",
            "Random gusts · real pressure",
            0.000065f,
            0.000105f,
            2.2f,
            3.8f,
            3.8f,
            5f);

        private static readonly DifficultyProfile WildProfile = new DifficultyProfile(
            DifficultyId.Wild,
            "Wild",
            "Heavy gusts · little recovery",
            0.00011f,
            0.000135f,
            1.3f,
            2.6f,
            4f,
            5.4f);

        public static DifficultyProfile GetDifficultyProfile(DifficultyId difficulty)
        {
            switch (difficulty)
            {
                case DifficultyId.Gentle:
                    return GentleProfile;
                case DifficultyId.Normal:
                    return NormalProfile;
                case DifficultyId.Wild:
                    return WildProfile;
                default:
                    return NormalProfile;
            }
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        public static int ClampCreatureCount(int value)
        {
            if (value < MinCreatureCount)
            {
                return MinCreatureCount;
            }

            return value > MaxCreatureCount ? MaxCreatureCount : value;
        }

        public static float Lerp(float min, float max, float progress)
        {
            return min + ((max - min) * progress);
        }

        public static float GetRequiredCounterAngle(float force, float gravityScale)
        {
            return MathUtility.Atan(force / gravityScale);
        }

        public static float GetGustEnvelope(float progress)
        {
            float bounded = Clamp(progress, 0f, 1f);

            if (bounded < 0.38f)
            {
                return SmoothStep(bounded / 0.38f);
            }

            if (bounded <= 0.75f)
            {
                return 1f;
            }

            return SmoothStep((1f - bounded) / 0.25f);
        }

        public static float GetEffectiveGustAcceleration(
            float force,
            int direction,
            float envelope,
            float platformAngle,
            float gravityScale,
            float counterAuthority)
        {
            float activeEnvelope = Clamp(envelope, 0f, 1f);
            if (activeEnvelope == 0f)
            {
                return 0f;
            }

            float windAcceleration = force < 0f ? 0f : force * Sign(direction);
            float platformAcceleration = MathUtility.Tan(platformAngle) * gravityScale * counterAuthority;
            return (windAcceleration + platformAcceleration) * activeEnvelope;
        }

        public static bool CanTransition(GamePhase from, GamePhase to)
        {
            switch (from)
            {
                case GamePhase.Ready:
                    return to == GamePhase.Playing;
                case GamePhase.Playing:
                    return to == GamePhase.Paused || to == GamePhase.Failing;
                case GamePhase.Paused:
                    return to == GamePhase.Playing || to == GamePhase.Ready;
                case GamePhase.Failing:
                    return to == GamePhase.Results;
                case GamePhase.Results:
                    return to == GamePhase.Ready || to == GamePhase.Playing;
                default:
                    return false;
            }
        }

        private static float SmoothStep(float value)
        {
            float bounded = Clamp(value, 0f, 1f);
            return bounded * bounded * (3f - (2f * bounded));
        }

        private static int Sign(int value)
        {
            if (value < 0)
            {
                return -1;
            }

            return value > 0 ? 1 : 0;
        }
    }

    internal static class MathUtility
    {
        public static float Atan(float value)
        {
            return System.Convert.ToSingle(System.Math.Atan(value));
        }

        public static float Tan(float value)
        {
            return System.Convert.ToSingle(System.Math.Tan(value));
        }

        public static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }
    }
}
