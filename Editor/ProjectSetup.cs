using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static System.IO.Path;

/// <summary>
/// From git-amend: https://www.youtube.com/watch?v=0_ZRHT2faQw
/// </summary>
public static class ProjectSetup
{
    [MenuItem("SOSXR/Setup/Import Essential Assets")]
    private static void ImportEssentialAssets()
    {
        ImportAssetsFrom.AssetStore("Editor Console Pro.unitypackage", "FlyingWorm/Editor ExtensionsSystem");
        // And the rest 
    }


    [MenuItem("SOSXR/Setup/Install Essential Git Packages")]
    public static void ImportEssentialGitPackages()
    {
        // Define only the user/repo part
        string[] repos =
        {
            "solo-fsw/sosxr-unity-enhancedlogger",
            "solo-fsw/sosxr-unity-swatchr"
        };
        
        var packageUrls = ConstructGitUrls(repos);


        ImportPackages.Packages(packageUrls);
    }

    
    private static string[] ConstructGitUrls(string[] repos)
    {
        var baseUrl = "git+https://github.com/";
        var urls = new string[repos.Length];

        for (var i = 0; i < repos.Length; i++)
        {
            urls[i] = baseUrl + repos[i] + ".git";
        }

        return urls;
    }


    [MenuItem("SOSXR/Setup/Install Essential Unity Packages")]
    public static void ImportEssentialUnityPackages()
    {
        string[] packages =
        {
            "com.unity.inputsystem" // Needs to be last
        };

        ImportPackages.Packages(packages);
    }


    private static class ImportAssetsFrom
    {
        public static void AssetStore(string asset, string folder)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var assetsFolder = Combine(basePath, "Unity/Asset Store-5.x");


            AssetDatabase.ImportPackage(Combine(assetsFolder, folder, asset), false);
        }
    }


    public class ImportPackages
    {
        private static AddRequest _request;
        private static readonly Queue<string> PackagesToInstall = new();


        private static async void StartNextPackageInstallation()
        {
            _request = Client.Add(PackagesToInstall.Dequeue());

            while (!_request.IsCompleted)
            {
                await Task.Delay(10);
            }

            if (_request.Status == StatusCode.Success)
            {
                Debug.Log("Installed: " + _request.Result.packageId);
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
    }


    public class Folders
    {
    }
}