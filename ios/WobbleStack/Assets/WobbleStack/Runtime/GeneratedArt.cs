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

    internal enum CreatureRigPart
    {
        Body,
        LeftArm,
        RightArm,
        LeftFoot,
        RightFoot,
        Accent1,
        Accent2,
        Accent3,
        Accent4
    }

    internal enum FacePart
    {
        LeftEye,
        RightEye,
        LeftPupil,
        RightPupil,
        RelaxedBrow,
        WorriedBrow,
        DeterminedBrow,
        DizzyBrow,
        LeftClosedEye,
        RightClosedEye,
        CalmMouth,
        UncertainMouth,
        EffortMouth,
        PanicMouth,
        GritMouth,
        JoyMouth,
        DazedMouth,
        LeftBlush,
        RightBlush
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

        public static Sprite Road()
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/road-tile");
            float visibleHeight = texture.height * 0.58f;
            return GetOrCreate(
                "road",
                texture,
                new Rect(0f, 0f, texture.width, visibleHeight),
                new Vector2(0.5f, 0.5f));
        }

        public static Sprite Character(CharacterKind kind)
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/characters-chroma");
            return GetCharacterSprite("character", texture, kind);
        }

        public static Sprite CalmCharacter(CharacterKind kind)
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/characters-calm-chroma");
            return GetCharacterSprite("character-calm", texture, kind);
        }

        public static Sprite ImpactCharacter(CharacterKind kind)
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/characters-impact-chroma");
            return GetCharacterSprite("character-impact", texture, kind);
        }

        public static Sprite RigPart(CharacterKind kind, CreatureRigPart part)
        {
            Texture2D texture = Resources.Load<Texture2D>($"WobbleStack/Art/rig-{GetRigTextureName(kind)}");
            GetRigPartGeometry(kind, part, out Rect rect, out Vector2 pivot);
            return GetOrCreate($"rig-{kind}-{part}", texture, rect, pivot);
        }

        public static Sprite Face(FacePart part)
        {
            Texture2D texture = Resources.Load<Texture2D>("WobbleStack/Art/rig-face-parts");
            GetFacePartGeometry(part, out Rect rect, out Vector2 pivot);
            return GetOrCreate($"face-{part}", texture, rect, pivot);
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

        private static string GetRigTextureName(CharacterKind kind)
        {
            switch (kind)
            {
                case CharacterKind.Pear:
                    return "pear";
                case CharacterKind.Cube:
                    return "cube";
                case CharacterKind.Bird:
                    return "bird";
                case CharacterKind.Rabbit:
                    return "rabbit";
                default:
                    return "jelly";
            }
        }

        private static void GetRigPartGeometry(
            CharacterKind kind,
            CreatureRigPart part,
            out Rect rect,
            out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            switch (kind)
            {
                case CharacterKind.Pear:
                    GetPearPartGeometry(part, out rect, out pivot);
                    return;
                case CharacterKind.Cube:
                    GetCubePartGeometry(part, out rect, out pivot);
                    return;
                case CharacterKind.Bird:
                    GetBirdPartGeometry(part, out rect, out pivot);
                    return;
                case CharacterKind.Rabbit:
                    GetRabbitPartGeometry(part, out rect, out pivot);
                    return;
                default:
                    GetJellyPartGeometry(part, out rect, out pivot);
                    return;
            }
        }

        private static void GetPearPartGeometry(
            CreatureRigPart part,
            out Rect rect,
            out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            switch (part)
            {
                case CreatureRigPart.Body:
                    rect = new Rect(121f, 137f, 588f, 773f);
                    break;
                case CreatureRigPart.LeftArm:
                    rect = new Rect(818f, 669f, 218f, 188f);
                    pivot = new Vector2(0.94f, 0.06f);
                    break;
                case CreatureRigPart.RightArm:
                    rect = new Rect(1168f, 668f, 213f, 190f);
                    pivot = new Vector2(0.06f, 0.06f);
                    break;
                case CreatureRigPart.LeftFoot:
                    rect = new Rect(875f, 481f, 156f, 93f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.RightFoot:
                    rect = new Rect(1178f, 478f, 145f, 90f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.Accent1:
                    rect = new Rect(823f, 187f, 91f, 177f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                case CreatureRigPart.Accent2:
                    rect = new Rect(972f, 185f, 100f, 170f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                case CreatureRigPart.Accent3:
                    rect = new Rect(1141f, 185f, 92f, 179f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                default:
                    rect = new Rect(1290f, 184f, 98f, 169f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
            }
        }

        private static void GetCubePartGeometry(
            CreatureRigPart part,
            out Rect rect,
            out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            switch (part)
            {
                case CreatureRigPart.Body:
                    rect = new Rect(133f, 226f, 599f, 605f);
                    break;
                case CreatureRigPart.LeftArm:
                    rect = new Rect(877f, 570f, 117f, 189f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.RightArm:
                    rect = new Rect(1257f, 570f, 122f, 190f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.LeftFoot:
                    rect = new Rect(911f, 223f, 130f, 82f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.RightFoot:
                    rect = new Rect(1181f, 223f, 128f, 82f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.Accent1:
                    rect = new Rect(991f, 426f, 88f, 50f);
                    break;
                default:
                    rect = new Rect(1153f, 425f, 90f, 51f);
                    break;
            }
        }

        private static void GetBirdPartGeometry(
            CreatureRigPart part,
            out Rect rect,
            out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            switch (part)
            {
                case CreatureRigPart.Body:
                    rect = new Rect(130f, 165f, 601f, 733f);
                    break;
                case CreatureRigPart.LeftArm:
                    rect = new Rect(862f, 595f, 165f, 266f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.RightArm:
                    rect = new Rect(1241f, 591f, 165f, 268f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.LeftFoot:
                    rect = new Rect(892f, 368f, 175f, 91f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.RightFoot:
                    rect = new Rect(1202f, 365f, 175f, 93f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.Accent1:
                    rect = new Rect(900f, 153f, 58f, 110f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                case CreatureRigPart.Accent2:
                    rect = new Rect(1013f, 153f, 64f, 109f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                case CreatureRigPart.Accent3:
                    rect = new Rect(1156f, 154f, 63f, 105f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                default:
                    rect = new Rect(1300f, 150f, 62f, 108f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
            }
        }

        private static void GetRabbitPartGeometry(
            CreatureRigPart part,
            out Rect rect,
            out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            switch (part)
            {
                case CreatureRigPart.Body:
                    rect = new Rect(176f, 165f, 560f, 646f);
                    break;
                case CreatureRigPart.LeftArm:
                    rect = new Rect(888f, 303f, 133f, 203f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.RightArm:
                    rect = new Rect(1265f, 301f, 125f, 205f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.LeftFoot:
                    rect = new Rect(884f, 111f, 174f, 116f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.RightFoot:
                    rect = new Rect(1198f, 111f, 171f, 116f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.Accent1:
                    rect = new Rect(938f, 574f, 164f, 390f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                default:
                    rect = new Rect(1183f, 574f, 165f, 390f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
            }
        }

        private static void GetJellyPartGeometry(
            CreatureRigPart part,
            out Rect rect,
            out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            switch (part)
            {
                case CreatureRigPart.Body:
                    rect = new Rect(136f, 252f, 734f, 516f);
                    break;
                case CreatureRigPart.LeftArm:
                    rect = new Rect(983f, 441f, 119f, 184f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.RightArm:
                    rect = new Rect(1284f, 439f, 118f, 185f);
                    pivot = new Vector2(0.5f, 0.96f);
                    break;
                case CreatureRigPart.LeftFoot:
                    rect = new Rect(1006f, 281f, 112f, 93f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.RightFoot:
                    rect = new Rect(1271f, 282f, 111f, 92f);
                    pivot = new Vector2(0.5f, 0.92f);
                    break;
                case CreatureRigPart.Accent1:
                    rect = new Rect(1080f, 694f, 262f, 185f);
                    pivot = new Vector2(0.5f, 0.02f);
                    break;
                case CreatureRigPart.Accent2:
                    rect = new Rect(1048f, 168f, 90f, 51f);
                    break;
                default:
                    rect = new Rect(1248f, 164f, 89f, 51f);
                    break;
            }
        }

        private static void GetFacePartGeometry(
            FacePart part,
            out Rect rect,
            out Vector2 pivot)
        {
            pivot = new Vector2(0.5f, 0.5f);
            switch (part)
            {
                case FacePart.LeftEye:
                    rect = new Rect(136f, 752f, 202f, 191f);
                    break;
                case FacePart.RightEye:
                    rect = new Rect(473f, 752f, 199f, 191f);
                    break;
                case FacePart.LeftPupil:
                    rect = new Rect(833f, 784f, 124f, 119f);
                    break;
                case FacePart.RightPupil:
                    rect = new Rect(1086f, 784f, 123f, 119f);
                    break;
                case FacePart.RelaxedBrow:
                    rect = new Rect(107f, 596f, 176f, 71f);
                    break;
                case FacePart.WorriedBrow:
                    rect = new Rect(389f, 594f, 147f, 81f);
                    break;
                case FacePart.DeterminedBrow:
                    rect = new Rect(672f, 587f, 151f, 81f);
                    break;
                case FacePart.DizzyBrow:
                    rect = new Rect(936f, 585f, 197f, 72f);
                    break;
                case FacePart.LeftClosedEye:
                    rect = new Rect(1229f, 586f, 78f, 28f);
                    break;
                case FacePart.RightClosedEye:
                    rect = new Rect(1362f, 587f, 78f, 27f);
                    break;
                case FacePart.CalmMouth:
                    rect = new Rect(156f, 389f, 142f, 52f);
                    break;
                case FacePart.UncertainMouth:
                    rect = new Rect(470f, 376f, 134f, 80f);
                    break;
                case FacePart.EffortMouth:
                    rect = new Rect(779f, 355f, 134f, 136f);
                    break;
                case FacePart.PanicMouth:
                    rect = new Rect(1084f, 329f, 207f, 179f);
                    break;
                case FacePart.GritMouth:
                    rect = new Rect(134f, 127f, 236f, 120f);
                    break;
                case FacePart.JoyMouth:
                    rect = new Rect(498f, 108f, 199f, 139f);
                    break;
                case FacePart.DazedMouth:
                    rect = new Rect(826f, 122f, 205f, 123f);
                    break;
                case FacePart.LeftBlush:
                    rect = new Rect(1138f, 137f, 95f, 74f);
                    break;
                default:
                    rect = new Rect(1296f, 137f, 95f, 74f);
                    break;
            }
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
