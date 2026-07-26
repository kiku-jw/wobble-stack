namespace WobbleStack.Domain
{
    public static class WobbleStackRules
    {
        public const float GravityScale = 0.00105f;
        public const float MaxPlatformAngle = 0.24f;
        public const float CounterWindExposure = 1.45f;
        public const float CounterTiltAuthority = 0.18f;
        public const float CounterTiltVelocityDamping = 5.5f;
        public const float FirstGustRestSeconds = 2.8f;
        public const float WindPreviewSeconds = 1.3f;
        public const float GustForceMin = 0.000055f;
        public const float GustForceMax = 0.00012f;
        public const float GustRestMin = 2.2f;
        public const float GustRestMax = 3.8f;
        public const float GustDurationMin = 3.8f;
        public const float GustDurationMax = 5.4f;
        public const int MinCreatureCount = 3;
        public const int MaxCreatureCount = 5;

        public static float SampleGustForce(float randomSample)
        {
            float bounded = Clamp(randomSample, 0f, 1f);
            return Lerp(GustForceMin, GustForceMax, bounded * bounded);
        }

        public static float GetGustIntensity(float force)
        {
            return Clamp(
                (force - GustForceMin) / (GustForceMax - GustForceMin),
                0f,
                1f);
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
            if (gravityScale <= 0f)
            {
                return 0f;
            }

            return MathUtility.Atan((MathUtility.Abs(force) * CounterWindExposure) / gravityScale);
        }

        public static float GetControlAmount(float normalizedScreenX)
        {
            float centered = (Clamp(normalizedScreenX, 0f, 1f) * 2f) - 1f;
            float magnitude = MathUtility.Abs(centered);
            const float deadZone = 0.04f;
            if (magnitude <= deadZone)
            {
                return 0f;
            }

            float remapped = (magnitude - deadZone) / (1f - deadZone);
            return centered < 0f ? -remapped : remapped;
        }

        public static float GetRelativeDriveAmount(
            float pointerOriginX,
            float currentPointerX,
            float screenWidth,
            float travelFraction)
        {
            if (screenWidth <= 0f || travelFraction <= 0f)
            {
                return 0f;
            }

            float travel = screenWidth * travelFraction;
            float normalizedDrag = Clamp((currentPointerX - pointerOriginX) / travel, -1f, 1f);
            return GetControlAmount((normalizedDrag + 1f) * 0.5f);
        }

        public static float GetWindPreviewEnvelope(float secondsUntilGust)
        {
            if (secondsUntilGust <= 0f || secondsUntilGust >= WindPreviewSeconds)
            {
                return 0f;
            }

            return SmoothStep(1f - (secondsUntilGust / WindPreviewSeconds));
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
            float envelope)
        {
            float activeEnvelope = Clamp(envelope, 0f, 1f);
            if (activeEnvelope == 0f)
            {
                return 0f;
            }

            float windAcceleration = force < 0f ? 0f : force * Sign(direction);
            return windAcceleration * activeEnvelope;
        }

        public static float GetEffectiveGustAcceleration(
            float force,
            int direction,
            float envelope,
            float platformAngleRadians,
            float gravityScale,
            float counterAuthority)
        {
            float activeEnvelope = Clamp(envelope, 0f, 1f);
            if (activeEnvelope == 0f)
            {
                return 0f;
            }

            float windAcceleration = force < 0f ? 0f : force * Sign(direction);
            float beamCounterAcceleration =
                -MathUtility.Tan(platformAngleRadians) *
                gravityScale *
                counterAuthority;
            return (windAcceleration + beamCounterAcceleration) * activeEnvelope;
        }

        public static float GetCounterTiltDampingAcceleration(
            float horizontalVelocity,
            int windDirection,
            float platformAngleRadians,
            float requiredAngleRadians,
            float envelope,
            float maximumDamping)
        {
            float direction = Sign(windDirection);
            float downwindVelocity = horizontalVelocity * direction;
            float correctTilt = platformAngleRadians * direction;
            if (downwindVelocity <= 0f ||
                correctTilt <= 0f ||
                requiredAngleRadians <= 0f ||
                maximumDamping <= 0f)
            {
                return 0f;
            }

            float activeEnvelope = Clamp(envelope, 0f, 1f);
            float tiltAuthority = Clamp(correctTilt / requiredAngleRadians, 0f, 1f);
            return -direction *
                downwindVelocity *
                maximumDamping *
                tiltAuthority *
                activeEnvelope;
        }

        public static bool CanTransition(GamePhase from, GamePhase to)
        {
            switch (from)
            {
                case GamePhase.Ready:
                    return to == GamePhase.Playing;
                case GamePhase.Playing:
                    return to == GamePhase.Paused ||
                        to == GamePhase.Failing ||
                        to == GamePhase.Finishing;
                case GamePhase.Paused:
                    return to == GamePhase.Playing || to == GamePhase.Ready;
                case GamePhase.Failing:
                    return to == GamePhase.Results;
                case GamePhase.Results:
                    return to == GamePhase.Ready || to == GamePhase.Playing;
                case GamePhase.Finishing:
                    return to == GamePhase.Results;
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

        public static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }

        public static float Tan(float value)
        {
            return System.Convert.ToSingle(System.Math.Tan(value));
        }
    }
}
