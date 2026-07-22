using System.Collections.Generic;
using UnityEngine;

namespace WobbleStack.Runtime
{
    internal enum CharacterKind
    {
        Pear,
        Cube,
        Bird,
        Rabbit,
        Jelly
    }

    internal static class GeneratedArt
    {
        private const float PixelsPerUnit = 100f;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static Material _chromaMaterial;

        public static Material ChromaMaterial
        {
            get
            {
                if (_chromaMaterial != null)
                {
                    return _chromaMaterial;
                }

                Shader shader = Resources.Load<Shader>("WobbleStack/Art/ChromaKeySprite");
                if (shader == null)
                {
                    throw new System.InvalidOperationException("Missing Wobble Stack chroma shader resource.");
                }

                _chromaMaterial = new Material(shader)
                {
                    name = "Generated Art Chroma"
                };
                _chromaMaterial.SetColor("_KeyColor", Color.magenta);
                _chromaMaterial.SetFloat("_Threshold", 0.24f);
                _chromaMaterial.SetFloat("_Softness", 0.055f);
                return _chromaMaterial;
            }
        }

        public static Sprite Background()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/sunset-stage");
            return GetOrCreate("background", texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }

        public static Sprite Character(CharacterKind kind)
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/characters-chroma");
            return GetCharacterSprite("character", texture, kind);
        }

        public static Sprite ImpactCharacter(CharacterKind kind)
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/characters-impact-chroma");
            return GetCharacterSprite("character-impact", texture, kind);
        }

        private static Sprite GetCharacterSprite(string prefix, Texture2D texture, CharacterKind kind)
        {
            Rect rect;

            switch (kind)
            {
                case CharacterKind.Pear:
                    rect = new Rect(55f, 135f, 330f, 570f);
                    break;
                case CharacterKind.Cube:
                    rect = new Rect(395f, 145f, 350f, 390f);
                    break;
                case CharacterKind.Bird:
                    rect = new Rect(745f, 145f, 330f, 455f);
                    break;
                case CharacterKind.Rabbit:
                    rect = new Rect(1070f, 135f, 380f, 575f);
                    break;
                default:
                    rect = new Rect(1440f, 145f, 420f, 370f);
                    break;
            }

            return GetOrCreate($"{prefix}-{kind}", texture, rect, new Vector2(0.5f, 0.5f));
        }

        public static Sprite Beam()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/props-ui-chroma");
            return GetOrCreate("beam", texture, new Rect(70f, 795f, 1400f, 180f), new Vector2(0.5f, 0.5f));
        }

        public static Sprite Fulcrum()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/props-ui-chroma");
            return GetOrCreate("fulcrum", texture, new Rect(590f, 480f, 340f, 310f), new Vector2(0.5f, 0.5f));
        }

        public static Sprite Crown()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/props-ui-chroma");
            return GetOrCreate("crown", texture, new Rect(65f, 80f, 365f, 330f), new Vector2(0.5f, 0.35f));
        }

        public static Sprite CoralPlate()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/props-ui-chroma");
            return GetOrCreate("coral-plate", texture, new Rect(450f, 80f, 610f, 350f), new Vector2(0.5f, 0.5f));
        }

        public static Sprite CocoaPlate()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/props-ui-chroma");
            return GetOrCreate("cocoa-plate", texture, new Rect(1110f, 75f, 330f, 360f), new Vector2(0.5f, 0.5f));
        }

        public static Sprite Dust()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/props-ui-chroma");
            return GetOrCreate("dust", texture, new Rect(115f, 455f, 340f, 275f), new Vector2(0.5f, 0.5f));
        }

        public static Sprite ImpactStars()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/props-ui-chroma");
            return GetOrCreate("impact-stars", texture, new Rect(980f, 430f, 430f, 330f), new Vector2(0.5f, 0.5f));
        }

        public static void Release()
        {
            foreach (Sprite sprite in Sprites.Values)
            {
                if (sprite != null)
                {
                    Object.Destroy(sprite);
                }
            }

            Sprites.Clear();
            if (_chromaMaterial != null)
            {
                Object.Destroy(_chromaMaterial);
                _chromaMaterial = null;
            }
        }

        private static Sprite GetOrCreate(string key, Texture2D texture, Rect rect, Vector2 pivot)
        {
            if (Sprites.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            sprite = Sprite.Create(texture, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            Sprites.Add(key, sprite);
            return sprite;
        }
    }
}
