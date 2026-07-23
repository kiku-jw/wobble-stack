using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WobbleStack.Editor
{
    public static class WobbleStackProjectBootstrap
    {
        private const string CompanyName = "KikuAI";
        private const string ProductName = "Wobble Stack";
        private const string BundleIdentifier = "dev.kikuai.wobblestack";
        private const string SceneDirectoryPath = "Assets/WobbleStack/Scenes";
        private const string GameScenePath = SceneDirectoryPath + "/Game.unity";
        private const string ResourcesArtPath = "Assets/WobbleStack/Resources/WobbleStack/Art";
        private const string AppIconPath = "Assets/WobbleStack/Art/AppIcon.png";
        private const string SmokeBuildDirectory = "Builds/MacSmoke";
        private const string SmokeBuildName = "Wobble Stack.app";
        private const string IosBuildDirectory = "Builds/iOS";
        private const int LegacyInputHandling = 0;
        private const int BothInputHandling = 2;

        [MenuItem("Wobble Stack/Configure Project")]
        public static void ConfigureProject()
        {
            EnsureGameScene();
            ConfigurePlayerSettings();
            ConfigureBuildSettings();
            ConfigureResourceTextures();
            ConfigureAppIcon();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Wobble Stack project configured.");
        }

        [MenuItem("Wobble Stack/Build Mac Smoke")]
        public static void BuildMacSmoke()
        {
            ConfigureProject();

            string outputPath = Path.Combine(SmokeBuildDirectory, SmokeBuildName);
            Directory.CreateDirectory(SmokeBuildDirectory);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { GameScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("Mac smoke build failed.");
            }

            Debug.Log($"Mac smoke build created at {outputPath}");
        }

        [MenuItem("Wobble Stack/Build iOS Development")]
        public static void BuildIosDevelopment()
        {
            ConfigureProject();
            string bundledSupportPath = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "iOSSupport");
            string hubSupportPath = Path.GetFullPath(
                Path.Combine(EditorApplication.applicationContentsPath, "..", "..", "PlaybackEngines", "iOSSupport"));
            if (!Directory.Exists(bundledSupportPath) && !Directory.Exists(hubSupportPath))
            {
                throw new BuildFailedException("Unity iOS Build Support is not installed for this editor.");
            }

            Directory.CreateDirectory(IosBuildDirectory);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { GameScenePath },
                locationPathName = IosBuildDirectory,
                target = BuildTarget.iOS,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("iOS development build failed.");
            }

            Debug.Log($"iOS Xcode project created at {IosBuildDirectory}");
        }

        public static void EnsureGameSceneBatch()
        {
            EnsureGameScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureGameScene()
        {
            Directory.CreateDirectory(SceneDirectoryPath);

            if (File.Exists(GameScenePath))
            {
                Scene existingScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
                if (existingScene.rootCount == 0)
                {
                    EditorSceneManager.SaveScene(existingScene);
                    return;
                }

                throw new BuildFailedException("Assets/WobbleStack/Scenes/Game.unity already exists and is not empty.");
            }

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(emptyScene, GameScenePath))
            {
                throw new BuildFailedException("Failed to save Assets/WobbleStack/Scenes/Game.unity.");
            }
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleIdentifier);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneOnly;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = false;
            PlayerSettings.defaultScreenWidth = 430;
            PlayerSettings.defaultScreenHeight = 932;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.SetMobileMTRendering(NamedBuildTarget.iOS, true);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);

            SetActiveInputHandling(HasInputSystemPackage() ? BothInputHandling : LegacyInputHandling);
            SetProjectSettingBoolean("submitAnalytics", false);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameScenePath, true)
            };
        }

        private static void ConfigureResourceTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ResourcesArtPath });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }

                bool changed = false;
                changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite, value => importer.textureType = value);
                changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single, value => importer.spriteImportMode = value);
                changed |= SetIfDifferent(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
                changed |= SetIfDifferent(importer.alphaIsTransparency, true, value => importer.alphaIsTransparency = value);
                changed |= SetIfDifferent(importer.wrapMode, TextureWrapMode.Clamp, value => importer.wrapMode = value);

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void ConfigureAppIcon()
        {
            AssetDatabase.ImportAsset(AppIconPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(AppIconPath) is not TextureImporter importer)
            {
                throw new BuildFailedException($"Unable to import app icon at {AppIconPath}.");
            }

            bool changed = false;
            changed |= SetIfDifferent(importer.textureType, TextureImporterType.Default, value => importer.textureType = value);
            changed |= SetIfDifferent(importer.mipmapEnabled, false, value => importer.mipmapEnabled = value);
            changed |= SetIfDifferent(importer.wrapMode, TextureWrapMode.Clamp, value => importer.wrapMode = value);
            if (changed)
            {
                importer.SaveAndReimport();
            }

            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
            {
                throw new BuildFailedException($"Unable to load app icon at {AppIconPath}.");
            }

            SetApplicationIcon(NamedBuildTarget.Standalone, icon);
            SetApplicationIcon(NamedBuildTarget.iOS, icon);
        }

        private static void SetApplicationIcon(NamedBuildTarget target, Texture2D icon)
        {
            int[] sizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
            if (sizes.Length == 0)
            {
                return;
            }

            Texture2D[] icons = new Texture2D[sizes.Length];
            for (int index = 0; index < icons.Length; index += 1)
            {
                icons[index] = icon;
            }

            PlayerSettings.SetIcons(target, icons, IconKind.Application);
        }

        private static void SetActiveInputHandling(int value)
        {
            SetProjectSettingInteger("activeInputHandler", value);
        }

        private static void SetProjectSettingInteger(string propertyName, int value)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0)
            {
                throw new BuildFailedException("Unable to load ProjectSettings asset.");
            }

            SerializedObject serializedObject = new SerializedObject(assets[0]);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new BuildFailedException($"{propertyName} property not found.");
            }

            if (property.intValue == value)
            {
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetProjectSettingBoolean(string propertyName, bool value)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0)
            {
                throw new BuildFailedException("Unable to load ProjectSettings asset.");
            }

            SerializedObject serializedObject = new SerializedObject(assets[0]);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new BuildFailedException($"{propertyName} property not found.");
            }

            if (property.boolValue == value)
            {
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasInputSystemPackage()
        {
            const string manifestPath = "Packages/manifest.json";
            return File.Exists(manifestPath) &&
                   File.ReadAllText(manifestPath).Contains("\"com.unity.inputsystem\"", StringComparison.Ordinal);
        }

        private static bool SetIfDifferent<T>(T currentValue, T expectedValue, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, expectedValue))
            {
                return false;
            }

            setter(expectedValue);
            return true;
        }
    }
}
