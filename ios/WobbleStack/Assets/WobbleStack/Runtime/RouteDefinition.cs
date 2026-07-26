using UnityEngine;

namespace WobbleStack.Runtime
{
    internal readonly struct RouteDefinition
    {
        private RouteDefinition(
            int index,
            string title,
            string subtitle,
            float finishX,
            int initialCreatureCount,
            Vector2[] badges,
            float[] joinStops,
            float[] roadBumps)
        {
            Index = index;
            Title = title;
            Subtitle = subtitle;
            FinishX = finishX;
            InitialCreatureCount = initialCreatureCount;
            Badges = badges;
            JoinStops = joinStops;
            RoadBumps = roadBumps;
        }

        public int Index { get; }

        public string Title { get; }

        public string Subtitle { get; }

        public float FinishX { get; }

        public int InitialCreatureCount { get; }

        public Vector2[] Badges { get; }

        public float[] JoinStops { get; }

        public float[] RoadBumps { get; }

        public static int Count => 3;

        public static RouteDefinition Get(int index)
        {
            switch (Mathf.Clamp(index, 0, Count - 1))
            {
                case 0:
                    return new RouteDefinition(
                        0,
                        "ORCHARD ROAD",
                        "FIND RABBIT AND JELLY",
                        38f,
                        3,
                        new[]
                        {
                            new Vector2(5f, -2.3f),
                            new Vector2(10f, -0.45f),
                            new Vector2(15f, 0.7f),
                            new Vector2(20f, -1.65f),
                            new Vector2(25f, 1.55f),
                            new Vector2(31f, -0.15f),
                            new Vector2(36f, 1.3f)
                        },
                        new[] { 12.5f, 25f },
                        new[] { 19f });
                case 1:
                    return new RouteDefinition(
                        1,
                        "CLOUD BRIDGE",
                        "KEEP ALL FIVE TOGETHER",
                        48f,
                        5,
                        new[]
                        {
                            new Vector2(6f, -0.9f),
                            new Vector2(12f, 1.65f),
                            new Vector2(18f, -2f),
                            new Vector2(24f, 0.2f),
                            new Vector2(30f, 2.05f),
                            new Vector2(36f, -1.4f),
                            new Vector2(42f, 0.75f),
                            new Vector2(46f, 1.75f)
                        },
                        new float[0],
                        new[] { 15f, 34f });
                default:
                    return new RouteDefinition(
                        2,
                        "WINDMILL HILL",
                        "MAKE THE SUNSET FESTIVAL",
                        58f,
                        5,
                        new[]
                        {
                            new Vector2(6f, -1.8f),
                            new Vector2(12f, 0.3f),
                            new Vector2(18f, 1.9f),
                            new Vector2(24f, -0.8f),
                            new Vector2(30f, 1.35f),
                            new Vector2(37f, -1.9f),
                            new Vector2(44f, 0.15f),
                            new Vector2(51f, 2.2f),
                            new Vector2(56f, 0.85f)
                        },
                        new float[0],
                        new[] { 12f, 29f, 46f });
            }
        }
    }
}
