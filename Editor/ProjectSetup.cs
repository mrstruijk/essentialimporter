using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static System.IO.Path;

public static class ProjectSetup
{
    [MenuItem("SOSXR/Setup/Create JSON Templates")]
    private static void CreateJsonFromTemplates()
    {
        const string targetFolder = "_SOSXR/Resources";

        if (!Directory.Exists(targetFolder))
        {
            Folders.Create(targetFolder);
        }

        CreateJsonFromTemplate("template-packages", targetFolder, "packages.json"); // Combined package template
        CreateJsonFromTemplate("template-editor-assets", targetFolder, "editor-assets.json");

        AssetDatabase.Refresh();
    }

    [MenuItem("SOSXR/Setup/Run Full Project Setup")]
    public static async void RunFullProjectSetup()
    {
        CreateFolders();
        Debug.Log("Folders created successfully.");

        ImportEssentialEditorTools();
        Debug.Log("Editor assets import started.");

        await Import.CompleteAssetInstallation();

        // Import essential packages (combined from Unity and Git)
        ImportEssentialPackages();
        Debug.Log("Package import started.");

        await Import.CompletePackageInstallation();

        Debug.Log("Full project setup completed successfully.");
    }

    private static void ImportEssentialEditorTools()
    {
        ImportAssetsFromJson("editor-assets");
    }

    private static void ImportEssentialPackages()
    {
        ImportPackagesFromJson("packages"); // Import from the combined packages
    }

    private static void CreateFolders()
    {
        Folders.Create("_SOSXR/Scripts", "_SOSXR/Textures & Materials", "_SOSXR/Models", "_SOSXR/Animation", "_SOSXR/Prefabs", "_SOSXR/Swatches", "_SOSXR/Rendering", "_SOSXR/XR", "_SOSXR/Input");
        Folders.Move("Scenes", "_SOSXR");
        Folders.Move("Settings", "_SOSXR");
        Folders.Delete("TutorialInfo");

        AssetDatabase.MoveAsset("Assets/InputSystem_Actions.inputactions", "Assets/_SOSXR/Input");
        AssetDatabase.DeleteAsset("Assets/Readme.asset");
        AssetDatabase.Refresh();
    }

    private static void ImportAssetsFromJson(string fileName)
    {
        var file = Resources.Load<TextAsset>(fileName);

        if (file == null)
        {
            Debug.LogError($"{fileName}.json not found in Resources folder.");
            return;
        }

        var data = JsonUtility.FromJson<AssetList>(file.text);

        if (data?.assets == null || data.assets.Length == 0)
        {
            Debug.LogError($"Nothing found in {fileName}.json.");
            return;
        }

        Import.FromAssetStore(data.assets);
    }

    private static void ImportPackagesFromJson(string fileName)
    {
        var file = Resources.Load<TextAsset>(fileName);

        if (file == null)
        {
            Debug.LogError($"{fileName}.json not found in Resources folder.");
            return;
        }

        var data = JsonUtility.FromJson<PackageList>(file.text);

        if (data?.packages == null || data.packages.Length == 0)
        {
            Debug.LogError($"Nothing found in {fileName}.json.");
            return;
        }

        // Split into Unity and Git packages based on naming convention
        var unityPackages = data.packages.Where(p => p.StartsWith("com.")).ToArray();
        var gitPackages = data.packages.Where(p => !p.StartsWith("com.")).ToArray();

        // Import both Unity and Git packages
        Import.Packages(unityPackages);
        Import.Packages(ConstructGitUrls(gitPackages));
    }

    private static string[] ConstructGitUrls(string[] repos)
    {
        return repos.Select(repo => $"https://github.com/{repo}.git").ToArray();
    }

    private static void CreateJsonFromTemplate(string templateName, string targetFolder, string newFileName)
    {
        var template = Resources.Load<TextAsset>(templateName);

        if (template == null)
        {
            Debug.LogError($"Template '{templateName}' not found in Resources folder.");
            return;
        }

        var targetPath = Combine(Application.dataPath, targetFolder);
        Folders.Create(targetPath);

        File.WriteAllText(Combine(targetPath, newFileName), template.text);
        Debug.Log($"Created JSON file '{newFileName}' at '{targetFolder}' using template '{templateName}'.");
        AssetDatabase.Refresh();
    }

    [Serializable]
    private class PackageList
    {
        public string[] packages;
    }

    [Serializable]
    private class AssetList
    {
        public string[] assets;
    }

    private static class Import
    {
        private static AddRequest _request;
        private static readonly Queue<string> PackagesToInstall = new();
        private static readonly Queue<string> AssetsToInstall = new();

        public static void FromAssetStore(string[] assets)
        {
            foreach (var asset in assets)
            {
                AssetsToInstall.Enqueue(asset);
            }

            if (AssetsToInstall.Count > 0)
            {
                StartNextAssetImport();
            }
        }

        private static async void StartNextAssetImport()
        {
            while (AssetsToInstall.Count > 0)
            {
                var assetPath = AssetsToInstall.Dequeue();
                string assetsFolder;

                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    assetsFolder = Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Unity/Asset Store-5.x");
                }
                else if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    assetsFolder = Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Asset Store-5.x");
                }
                else // Default to Linux path for completeness
                {
                    assetsFolder = Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity/Asset Store-5.x");
                }

                if (!Directory.Exists(assetsFolder))
                {
                    Debug.LogWarning($"Folder not found: {assetsFolder}. Please download asset '{assetPath.Split("/")[^1]}' from the Asset Store.");
                    continue;
                }

                var fullAssetPath = Combine(assetsFolder, assetPath);

                if (File.Exists(fullAssetPath))
                {
                    AssetDatabase.ImportPackage(fullAssetPath, false);
                    Debug.Log($"Imported asset from: {fullAssetPath}");
                }
                else
                {
                    Debug.LogWarning($"Asset '{assetPath}' not found in '{assetsFolder}'. Please download it from the Asset Store.");
                }

                await Task.Delay(10); // Slight delay between each import to avoid potential conflicts
            }
        }

        public static void Packages(string[] packages)
        {
            foreach (var package in packages)
            {
                PackagesToInstall.Enqueue(package);
            }

            if (PackagesToInstall.Count > 0)
            {
                StartNextPackageInstallation();
            }
        }

        private static void StartNextPackageInstallation()
        {
            if (PackagesToInstall.Count == 0)
            {
                return;
            }

            _request = Client.Add(PackagesToInstall.Dequeue());
            EditorApplication.update += MonitorPackageInstall;
        }

        private static void MonitorPackageInstall()
        {
            if (_request == null || !_request.IsCompleted)
            {
                return;
            }

            // Check request result
            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"Installed: {_request.Result.packageId}");
            }
            else if (_request.Status >= StatusCode.Failure)
            {
                Debug.LogError(_request.Error.message);
            }

            EditorApplication.update -= MonitorPackageInstall;
            _request = null;

            // Add a delay before starting the next installation
            EditorApplication.delayCall += () =>
            {
                if (PackagesToInstall.Count > 0)
                {
                    StartNextPackageInstallation();
                }
            };
        }

        public static async Task CompleteAssetInstallation()
        {
            while (AssetsToInstall.Count > 0)
            {
                await Task.Delay(10);
            }
        }

        public static async Task CompletePackageInstallation()
        {
            while (PackagesToInstall.Count > 0)
            {
                await Task.Delay(10);
            }
        }
    }

    private static class Folders
    {
        public static void Create(string root, params string[] folders)
        {
            var fullPath = Combine(Application.dataPath, root);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            foreach (var folder in folders)
            {
                CreateSubFolders(fullPath, folder);
            }
        }

        private static void CreateSubFolders(string rootPath, string folderHierarchy)
        {
            var currentPath = rootPath;

            foreach (var folder in folderHierarchy.Split('/'))
            {
                currentPath = Combine(currentPath, folder);

                if (!Directory.Exists(currentPath))
                {
                    Directory.CreateDirectory(currentPath);
                }
            }
        }

        public static void Move(string folderName, string destination)
        {
            var sourcePath = $"Assets/{folderName}";

            if (!AssetDatabase.IsValidFolder(sourcePath))
            {
                return;
            }

            var error = AssetDatabase.MoveAsset(sourcePath, $"Assets/{destination}/{folderName}");

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
            }
        }

        public static void Delete(string folderName)
        {
            var path = $"Assets/{folderName}";

            if (AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
