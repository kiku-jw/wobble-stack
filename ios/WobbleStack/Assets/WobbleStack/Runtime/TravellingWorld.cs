using System.Collections.Generic;
using UnityEngine;

namespace WobbleStack.Runtime
{
    internal sealed class TravellingWorld : MonoBehaviour
    {
        private const float GroundSurfaceY = -7.15f;
        private const float FarParallax = 0.24f;
        private const float MidParallax = 0.52f;
        private const float ForegroundParallax = 1.08f;
        private readonly List<CloudMotion> _clouds = new List<CloudMotion>();
        private readonly List<RoutePickup> _pickups = new List<RoutePickup>();
        private Transform _sky;
        private Transform _farLayer;
        private Transform _midLayer;
        private Transform _routeLayer;
        private Transform _foregroundLayer;
        private Transform _windmillRotor;
        private float _cameraX;
        private float _routeProgress;

        public int CloudCount => _clouds.Count;

        public int BadgeCount => _pickups.Count;

        public float WindmillRotation => _windmillRotor == null
            ? 0f
            : _windmillRotor.localEulerAngles.z;

        public void Build()
        {
            _sky = CreateLayer("Route Sky");
            SpriteRenderer skyRenderer = CreateSprite(
                "Empty Sunset",
                _sky,
                GeneratedArt.RouteSky(),
                Vector2.zero,
                20.4f,
                -100,
                Color.white);
            skyRenderer.material = GeneratedArt.OpaqueSpriteMaterial;
            skyRenderer.transform.localPosition = new Vector3(0f, 0f, 2f);

            Transform cloudLayer = CreateLayer("Drifting Clouds");
            CreateCloud(cloudLayer, WorldProp.CloudLeft, -7.2f, 5.3f, 1.75f, 0.19f);
            CreateCloud(cloudLayer, WorldProp.CloudMiddle, -1.8f, 3.6f, 2.05f, 0.14f);
            CreateCloud(cloudLayer, WorldProp.CloudRight, 4.7f, 6.1f, 1.55f, 0.23f);
            CreateCloud(cloudLayer, WorldProp.CloudLeft, 9.2f, 1.8f, 1.35f, 0.17f);
        }

        public void ConfigureRoute(RouteDefinition route, WobbleStackGame game)
        {
            ClearRouteLayers();
            _farLayer = CreateLayer($"Route {route.Index + 1} Far");
            _midLayer = CreateLayer($"Route {route.Index + 1} Mid");
            _routeLayer = CreateLayer($"Route {route.Index + 1} Gameplay");
            _foregroundLayer = CreateLayer($"Route {route.Index + 1} Foreground");
            _pickups.Clear();
            _windmillRotor = null;

            BuildFarScenery(route);
            BuildMidScenery(route);
            BuildRouteLandmarks(route);
            BuildForeground(route);
            BuildBadges(route, game);
            SetRouteView(_cameraX, _routeProgress);
        }

        public void SetCameraX(float cameraX)
        {
            SetRouteView(cameraX, cameraX);
        }

        public void SetRouteView(float cameraX, float routeProgress)
        {
            _cameraX = cameraX;
            _routeProgress = routeProgress;
            if (_sky != null)
            {
                _sky.position = new Vector3(cameraX, 0f, 0f);
            }

            if (_farLayer != null)
            {
                _farLayer.position = new Vector3(
                    cameraX - (routeProgress * FarParallax),
                    0f,
                    0f);
            }

            if (_midLayer != null)
            {
                _midLayer.position = new Vector3(
                    cameraX - (routeProgress * MidParallax),
                    0f,
                    0f);
            }

            if (_routeLayer != null)
            {
                _routeLayer.position = new Vector3(
                    cameraX - routeProgress,
                    0f,
                    0f);
            }

            if (_foregroundLayer != null)
            {
                _foregroundLayer.position = new Vector3(
                    cameraX - (routeProgress * ForegroundParallax),
                    0f,
                    0f);
            }
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            for (int index = 0; index < _clouds.Count; index += 1)
            {
                CloudMotion cloud = _clouds[index];
                float offset = Mathf.Repeat(
                    cloud.StartOffset + (time * cloud.Speed) + 10f,
                    20f) - 10f;
                cloud.Transform.position = new Vector3(
                    _cameraX + offset,
                    cloud.Height + (Mathf.Sin((time * 0.28f) + cloud.Phase) * 0.08f),
                    0f);
            }

            if (_windmillRotor != null)
            {
                float direction = Mathf.Sin(time * 0.21f) >= -0.82f ? -1f : 1f;
                _windmillRotor.Rotate(0f, 0f, direction * 22f * Time.unscaledDeltaTime);
            }
        }

        private void BuildFarScenery(RouteDefinition route)
        {
            float[] landmarks = route.Index == 0
                ? new[] { 4f, 23f, 44f }
                : route.Index == 1
                    ? new[] { 7f, 28f, 49f, 62f }
                    : new[] { 10f, 34f, 56f, 72f };
            for (int index = 0; index < landmarks.Length; index += 1)
            {
                float routeX = landmarks[index];
                float height = 2.2f + ((index % 2) * 0.45f);
                CreateSprite(
                    $"Far Mesa {index + 1}",
                    _farLayer,
                    GeneratedArt.World(WorldProp.Mesa),
                    new Vector2(routeX * FarParallax, -5.85f),
                    height,
                    -72,
                    new Color(0.94f, 0.78f, 0.79f, 0.62f));
            }
        }

        private void BuildMidScenery(RouteDefinition route)
        {
            float[] treePositions = route.Index == 0
                ? new[] { -3f, 10f, 25f, 42f, 56f }
                : route.Index == 1
                    ? new[] { -4f, 18f, 41f, 66f }
                    : new[] { -3f, 14f, 33f, 54f, 73f };
            for (int index = 0; index < treePositions.Length; index += 1)
            {
                float routeX = treePositions[index];
                float height = 3.05f + ((index % 3) * 0.32f);
                CreateSprite(
                    $"Parallax Orchard {index + 1}",
                    _midLayer,
                    GeneratedArt.World(WorldProp.OrchardTree),
                    new Vector2(routeX * MidParallax, GroundSurfaceY - 0.05f),
                    height,
                    -28,
                    new Color(0.92f, 0.9f, 0.76f, 0.82f));
            }

            if (route.Index < 2)
            {
                CreateWindmill(
                    _midLayer,
                    (route.FinishX + 11f) * MidParallax,
                    4.8f,
                    -24);
            }
        }

        private void BuildRouteLandmarks(RouteDefinition route)
        {
            for (int index = 0; index < route.JoinStops.Length; index += 1)
            {
                CreateSprite(
                    $"Friend Stop {index + 1}",
                    _routeLayer,
                    GeneratedArt.World(WorldProp.SafeStop),
                    new Vector2(route.JoinStops[index], GroundSurfaceY + 0.02f),
                    1.85f,
                    -10,
                    Color.white);
            }

            if (route.Index == 2)
            {
                CreateWindmill(_routeLayer, route.FinishX + 3.6f, 6.4f, -15);
            }

            CreateSprite(
                "Festival Finish",
                _routeLayer,
                GeneratedArt.World(WorldProp.FestivalArch),
                new Vector2(route.FinishX, GroundSurfaceY + 0.04f),
                5.25f,
                -12,
                Color.white);
        }

        private void BuildForeground(RouteDefinition route)
        {
            float[] positions = route.Index == 0
                ? new[] { 14f, 39f }
                : route.Index == 1
                    ? new[] { 20f, 53f }
                    : new[] { 25f, 61f };
            for (int index = 0; index < positions.Length; index += 1)
            {
                CreateSprite(
                    $"Foreground Orchard {index + 1}",
                    _foregroundLayer,
                    GeneratedArt.World(WorldProp.OrchardTree),
                    new Vector2(positions[index] * ForegroundParallax, GroundSurfaceY - 1.4f),
                    2.75f,
                    -3,
                    new Color(0.72f, 0.64f, 0.52f, 0.92f));
            }
        }

        private void BuildBadges(RouteDefinition route, WobbleStackGame game)
        {
            for (int index = 0; index < route.Badges.Length; index += 1)
            {
                GameObject badge = new GameObject($"Festival Badge {index + 1}");
                badge.transform.SetParent(_routeLayer, false);
                badge.transform.localPosition = route.Badges[index];
                SpriteRenderer renderer = badge.AddComponent<SpriteRenderer>();
                renderer.sprite = GeneratedArt.Fulcrum();
                renderer.material = GeneratedArt.ChromaMaterial;
                renderer.sortingOrder = 22;
                FitHeight(badge.transform, renderer.sprite, 0.76f);
                CircleCollider2D collider = badge.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = renderer.sprite.bounds.extents.x * 0.7f;
                RoutePickup pickup = badge.AddComponent<RoutePickup>();
                pickup.Initialize(game, index);
                _pickups.Add(pickup);
            }
        }

        private void CreateCloud(
            Transform parent,
            WorldProp prop,
            float startOffset,
            float height,
            float spriteHeight,
            float speed)
        {
            SpriteRenderer renderer = CreateSprite(
                $"Cloud {_clouds.Count + 1}",
                parent,
                GeneratedArt.World(prop),
                Vector2.zero,
                spriteHeight,
                -86,
                new Color(1f, 0.94f, 0.83f, 0.73f));
            _clouds.Add(new CloudMotion(
                renderer.transform,
                startOffset,
                height,
                speed,
                _clouds.Count * 1.13f));
        }

        private void CreateWindmill(
            Transform parent,
            float x,
            float height,
            int sortingOrder)
        {
            CreateSprite(
                "Windmill Tower",
                parent,
                GeneratedArt.World(WorldProp.WindmillTower),
                new Vector2(x, GroundSurfaceY + 0.04f),
                height,
                sortingOrder,
                Color.white);
            SpriteRenderer rotor = CreateSprite(
                "Windmill Rotor",
                parent,
                GeneratedArt.World(WorldProp.WindmillRotor),
                new Vector2(x, GroundSurfaceY + (height * 0.79f)),
                height * 0.48f,
                sortingOrder + 1,
                Color.white);
            _windmillRotor = rotor.transform;
        }

        private Transform CreateLayer(string name)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(transform, false);
            return layer.transform;
        }

        private static SpriteRenderer CreateSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 position,
            float height,
            int sortingOrder,
            Color color)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = position;
            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.material = GeneratedArt.WorldChromaMaterial;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            FitHeight(spriteObject.transform, sprite, height);
            return renderer;
        }

        private void ClearRouteLayers()
        {
            DisableAndDestroy(_farLayer);
            DisableAndDestroy(_midLayer);
            DisableAndDestroy(_routeLayer);
            DisableAndDestroy(_foregroundLayer);
        }

        private static void DisableAndDestroy(Transform layer)
        {
            if (layer == null)
            {
                return;
            }

            layer.gameObject.SetActive(false);
            Object.Destroy(layer.gameObject);
        }

        private static void FitHeight(Transform target, Sprite sprite, float height)
        {
            float scale = height / sprite.bounds.size.y;
            target.localScale = new Vector3(scale, scale, 1f);
        }

        private readonly struct CloudMotion
        {
            public CloudMotion(
                Transform transform,
                float startOffset,
                float height,
                float speed,
                float phase)
            {
                Transform = transform;
                StartOffset = startOffset;
                Height = height;
                Speed = speed;
                Phase = phase;
            }

            public Transform Transform { get; }

            public float StartOffset { get; }

            public float Height { get; }

            public float Speed { get; }

            public float Phase { get; }
        }
    }
}
