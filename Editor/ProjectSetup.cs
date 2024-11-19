using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static System.Environment;
using static System.IO.Path;
using static UnityEditor.AssetDatabase;


/// <summary>
///     From git-amend: https://www.youtube.com/watch?v=0_ZRHT2faQw&t=77s
/// </summary>
public static class ProjectSetup
{
    [MenuItem("SOSXR/Setup/Import Essential Assets")]
    public static void ImportEssentials()
    {
        Assets.ImportAsset("Editor Console Pro.unitypackage", "FlyingWorm/Editor ExtensionsSystem");
        Assets.ImportAsset("Play Mode Saver.unitypackage", "Clarky/Editor ExtensionsSystem");
        Assets.ImportAsset("Script Inspector 3.unitypackage", "Flipbook Games/Editor ExtensionsVisual Scripting");
        Assets.ImportAsset("Colourful Hierarchy Category GameObject.unitypackage", "M STUDIO HUB/Editor ExtensionsUtilities");
        Assets.ImportAsset("Smart Editor Selection.unitypackage", "Overfort Games/Editor ExtensionsDesign");
        Assets.ImportAsset("UMotion Pro - Animation Editor.unitypackage", "Soxware Interactive/Editor ExtensionsAnimation");
    }


    [MenuItem("SOSXR/Setup/Install Essential Packages")]
    public static void InstallPackages()
    {
        Packages.InstallPackages(new[]
        {
            "com.unity.2d.animation",
            "com.unity.mobile.android-logcat",
            "com.unity.cloud.gltfast",
            "com.unity.nuget.newtonsoft-json",
            "com.unity.modules.imageconversion",
            "git+https://github.com/adammyhre/Unity-Utils.git",
            "git+https://github.com/adammyhre/Unity-Improved-Timers.git",
            "git+https://github.com/KyleBanks/scene-ref-attribute.git",
            "git+https://github.com/solo-fsw/sosxr-unity-enhancedlogger.git",
            "git+https://github.com/solo-fsw/sosxr-unity-swatchr.git",
            "git+https://github.com/solo-fsw/sosxr-unity-additionalunityevents.git",
            "git+https://github.com/solo-fsw/sosxr-unity-additionalgizmos.git",
            "git+https://github.com/solo-fsw/sosxr-unity-editortools.git",
            "git+https://github.com/solo-fsw/sosxr-unity-autosave.git",
            "git+https://github.com/solo-fsw/sosxr-unity-buildhelpers.git",
            "git+https://github.com/solo-fsw/sosxr-unity-extensionmethods.git",
            "git+https://github.com/solo-fsw/sosxr-unity-scriptableobjectarchitecture.git",
            "git+https://github.com/solo-fsw/sosxr-unity-timelineextensions.git",
            "git+https://github.com/solo-fsw/sosxr-unity-readmehelpers.git",
            "git+https://github.com/solo-fsw/sosxr-unity-markdownviewer.git",
            "git+https://github.com/solo-fsw/sosxr-unity-ingamedebugconsole.git",
            "git+https://github.com/solo-fsw/sosxr-unity-assetdependency.git",
            "git+https://github.com/mrstruijk/UXF.git",
            "git+https://github.com/mrstruijk/Unity-Editor-Toolbox.git",
            "git+https://github.com/XCharts-Team/XCharts.git",
            "com.unity.inputsystem" // If necessary, import new Input System last as it requires a Unity Editor restart
        });
    }


    [MenuItem("SOSXR/Setup/Create Folders")]
    public static void CreateFolders()
    {
        Folders.Create("_SOSXR", "Scripts", "Textures & Materials", "Models", "Animation", "Prefabs", "Swatches", "Rendering", "XR", "Input", "Collected Data", "Resources");
        
        Refresh();
        Folders.Move("_SOSXR", "Scenes");
        Folders.Move("_SOSXR", "Settings");
        Folders.Delete("TutorialInfo");
        Refresh();

        MoveAsset("Assets/InputSystem_Actions.inputactions", "Assets/_SOSXR/Settings/InputSystem_Actions.inputactions");
        DeleteAsset("Assets/Readme.asset");
        Refresh();

        // Optional: Disable Domain Reload
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
    }


    private static class Assets
    {
        public static void ImportAsset(string asset, string folder)
        {
            string basePath;

            if (OSVersion.Platform is PlatformID.MacOSX or PlatformID.Unix)
            {
                var homeDirectory = GetFolderPath(SpecialFolder.Personal);
                basePath = Combine(homeDirectory, "Library/Unity/Asset Store-5.x");
            }
            else
            {
                var defaultPath = Combine(GetFolderPath(SpecialFolder.ApplicationData), "Unity");
                basePath = Combine(EditorPrefs.GetString("AssetStoreCacheRootPath", defaultPath), "Asset Store-5.x");
            }

            asset = asset.EndsWith(".unitypackage") ? asset : asset + ".unitypackage";

            var fullPath = Combine(basePath, folder, asset);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"The asset package was not found at the path: {fullPath}");
            }

            ImportPackage(fullPath, false);
        }
    }


    private static class Packages
    {
        private static AddRequest request;
        private static readonly Queue<string> packagesToInstall = new();


        public static void InstallPackages(string[] packages)
        {
            foreach (var package in packages)
            {
                packagesToInstall.Enqueue(package);
            }

            if (packagesToInstall.Count > 0)
            {
                StartNextPackageInstallation();
            }
        }


        private static async void StartNextPackageInstallation()
        {
            request = Client.Add(packagesToInstall.Dequeue());

            while (!request.IsCompleted)
            {
                await Task.Delay(10);
            }

            if (request.Status == StatusCode.Success)
            {
                Debug.Log("Installed: " + request.Result.packageId);
            }
            else if (request.Status >= StatusCode.Failure)
            {
                Debug.LogError(request.Error.message);
            }

            if (packagesToInstall.Count > 0)
            {
                await Task.Delay(1000);
                StartNextPackageInstallation();
            }
        }
    }


    private static class Folders
    {
        public static void Create(string root, params string[] folders)
        {
            var fullpath = Combine(Application.dataPath, root);

            if (!Directory.Exists(fullpath))
            {
                Directory.CreateDirectory(fullpath);
            }

            foreach (var folder in folders)
            {
                CreateSubFolders(fullpath, folder);
            }
        }


        private static void CreateSubFolders(string rootPath, string folderHierarchy)
        {
            var folders = folderHierarchy.Split('/');
            var currentPath = rootPath;

            foreach (var folder in folders)
            {
                currentPath = Combine(currentPath, folder);

                if (Directory.Exists(currentPath))
                {
                    continue;
                }

                Directory.CreateDirectory(currentPath);
            }
        }


        public static void Move(string newParent, string folderName)
        {
            var sourcePath = $"Assets/{folderName}";

            if (!IsValidFolder(sourcePath))
            {
                return;
            }

            var destinationPath = $"Assets/{newParent}/{folderName}";

            if (Directory.Exists(destinationPath))
            {
                return;
            }
            
            var error = MoveAsset(sourcePath, destinationPath);

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Failed to move {folderName}: {error}");
            }
        }


        public static void Delete(string folderName)
        {
            var pathToDelete = $"Assets/{folderName}";

            if (!IsValidFolder(pathToDelete))
            {
                return;
            }

            DeleteAsset(pathToDelete);
        }
    }
}