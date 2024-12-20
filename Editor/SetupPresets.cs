using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;


namespace SOSXR.Setup
{
    public static class SetupPresets
    {
        private static string[] _foldersToSearch
        {
            get { return new[] {"Assets/_SOSXR", "Packages"}; }
        }

        private static readonly string _defaultFilter = "t:preset SOSXR ";


        [MenuItem("SOSXR/Setup/Setup Initial Presets")]
        private static void SetInitialPresets()
        {
            SetPlayerSettingsPreset();
            SetQualitySettingsPreset();
        }


        private static void SetPlayerSettingsPreset()
        {
            var productName = PlayerSettings.productName;

            var targets = Resources.FindObjectsOfTypeAll<PlayerSettings>();

            var filter = "PlayerSettings";
            var preset = GetPreset(filter);

            foreach (var target in targets)
            {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(target)))
                {
                    continue;
                }

                if (preset.ApplyTo(target))
                {
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
                    Debug.Log($"Successfully applied preset to {target.name}");
                }
            }

            PlayerSettings.productName = productName;
        }


        private static void SetQualitySettingsPreset()
        {
            var target = Resources.FindObjectsOfTypeAll<QualitySettings>()[0];

            var filter = "Quality";
            var preset = GetPreset(filter);

            if (preset.ApplyTo(target))
            {
                Debug.Log($"Successfully applied preset to {target.name}");
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
            }
        }


        private static Preset GetPreset(string filter)
        {
            var fullFilter = _defaultFilter + filter;
            var guids = AssetDatabase.FindAssets(fullFilter, _foldersToSearch);

            if (guids.Length == 0)
            {
                Debug.LogWarning("No presets found with filter " + fullFilter);

                return null;
            }

            if (guids.Length > 1)
            {
                Debug.LogWarning($"Multiple presets found using filter {fullFilter}. Using the first one.");
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);

            if (preset == null)
            {
                Debug.LogError($"Preset not found for GUID: {guids[0]}");

                return null;
            }

            return preset;
        }


        /// <summary>
        ///     Based on Warped Imagination: https://www.youtube.com/watch?v=KFmP1Q8NySo
        /// </summary>
        [MenuItem("SOSXR/Setup/Setup Default Presets")]
        private static void SetupLibraryPresetsMenuOption()
        {
            var filter = "Default";
            var fullFilter = _defaultFilter + filter;
            var guids = AssetDatabase.FindAssets(fullFilter, _foldersToSearch);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);

                var type = preset.GetPresetType();

                var list = new List<DefaultPreset>(Preset.GetDefaultPresetsForType(type));

                if (list.Any(defaultPreset => defaultPreset.preset == preset))
                {
                    return;
                }

                list.Add(new DefaultPreset(null, preset));

                Preset.SetDefaultPresetsForType(type, list.ToArray());
            }
        }
    }
}