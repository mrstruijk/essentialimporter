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
    [MenuItem("SOSXR/Setup/Import Essential Editor Tools")]
    private static void ImportEssentialEditorTools()
    {
        ImportAssetsFromJson("editor-assets");
    }


    [MenuItem("SOSXR/Setup/Install Essential Unity Packages")]
    public static void ImportEssentialUnityPackages()
    {
        ImportPackagesFromJson("unity-packages");
    }


    [MenuItem("SOSXR/Setup/Install Essential Git Packages")]
    public static void ImportEssentialGitPackages()
    {
        ImportPackagesFromGit("git-packages");
    }


    [MenuItem("SOSXR/Setup/CreateFolders")]
    public static void CreateFolders()
    {
        Folders.Create("_SOSXR/Scripts", "_SOSXR/Textures & Materials", "_SOSXR/Models", "_SOSXR/Animation", "_SOSXR/Prefabs", "_SOSXR/Swatches", "_SOSXR/Rendering", "_SOSXR/XR", "_SOSXR/Input");
        Folders.Move("Scenes", "_SOSXR");
        Folders.Move("Settings", "_SOSXR");
        Folders.Delete("TutorialInfo");

        AssetDatabase.MoveAsset("Assets/InputSystem_Actions.inputactions", "Assets/_SOSXR/Input");
        AssetDatabase.DeleteAsset("Assets/Readme.asset");
        AssetDatabase.Refresh();
    }


    [MenuItem("SOSXR/Setup/Create Package Lists From Template")]
    public static void CreateJsonFromTemplates()
    {
        const string targetFolder = "Assets/_SOSXR/Resources";

        if (!Directory.Exists(targetFolder))
        {
            
        }
        Folders.Create(targetFolder);
        CreateJsonFromTemplate("template-unity-packages", targetFolder, "unity-packages.json");
        CreateJsonFromTemplate("template-editor-assets", targetFolder, "editor-assets.json");
        CreateJsonFromTemplate("template-git-packages", targetFolder, "git-packages.json");
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

        Import.Packages(data.packages);
    }


    private static void ImportPackagesFromGit(string fileName)
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

        Import.Packages(ConstructGitUrls(data.packages));
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


        public static void FromAssetStore(string[] assets)
        {
            Array.ForEach(assets, FromAssetStore);
        }


        private static void FromAssetStore(string path)
        {
            var assetsFolder = Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity/Asset Store-5.x");

            if (!Directory.Exists(assetsFolder))
            {
                Debug.LogWarning($"Folder not found: {assetsFolder}. Please download asset '{path.Split("/")[^1]}' from Asset Store.");

                return;
            }

            AssetDatabase.ImportPackage(Combine(assetsFolder, path), false);
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


        private static async void StartNextPackageInstallation()
        {
            _request = Client.Add(PackagesToInstall.Dequeue());

            while (!_request.IsCompleted)
            {
                await Task.Delay(10);
            }

            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"Installed: {_request.Result.packageId}");
            }
            else if (_request.Status >= StatusCode.Failure)
            {
                Debug.LogError(_request.Error.message);
            }

            if (PackagesToInstall.Count > 0)
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
                Debug.LogWarning($"Failed to move {folderName}: {error}");
            }
        }


        public static void Delete(string folderName)
        {
            var pathToDelete = $"Assets/{folderName}";

            if (AssetDatabase.IsValidFolder(pathToDelete))
            {
                AssetDatabase.DeleteAsset(pathToDelete);
            }
        }
    }
}