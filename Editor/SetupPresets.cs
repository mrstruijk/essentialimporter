using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private static bool _initialPresetsApplied // Set it as a EditorPrefs so it persists between sessions
        {
            get => EditorPrefs.GetBool("InitialPresetsApplied", false);
            set => EditorPrefs.SetBool("InitialPresetsApplied", value);
        }

        private static readonly string _defaultFilter = "t:preset SOSXR ";
        private static readonly List<string> _paths = new();


        /// <summary>
        ///     These are presets that in theory should be applied to projects only once, at the beginning.
        /// A dialog will display if the presets have already been applied, asking for confirmation.
        /// </summary>
        [MenuItem("SOSXR/Setup/Setup Initial Presets")]
        private static async void SetInitialPresets()
        {
            try
            {
                if (_initialPresetsApplied)
                {
                    var confirm = EditorUtility.DisplayDialog("Initial Presets Already Applied", "Initial presets have already been applied. Are you sure you want to apply them again?", "Yes", "No");

                    if (!confirm)
                    {
                        return;
                    }
                }
                
                await SetPlayerSettingsPreset();
                await SetQualitySettingsPreset();

                foreach (var path in _paths)
                {
                    await Import(path);
                    Debug.Log($"Successfully applied preset to {path}");
                }

                AssetDatabase.Refresh();
                _initialPresetsApplied = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }


        private static async Task SetPlayerSettingsPreset()
        {
            var productName = PlayerSettings.productName;

            var targets = Resources.FindObjectsOfTypeAll<PlayerSettings>();

            var filter = "PlayerSettings";
            var preset = GetPreset(filter);

            foreach (var target in targets) // For some reason there are multiple PlayerSettings instances, but not all are the correct ones. 
            {
                if (target == null)
                {
                    continue;
                }
                
                if (preset == null)
                {
                    continue;
                }
                
                var path = AssetDatabase.GetAssetPath(target);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (preset.ApplyTo(target))
                {
                    _paths.Add(path);
                }
                else
                {
                    Debug.LogError($"Failed to apply preset to {filter}");
                }
            }

            PlayerSettings.productName = productName; // Put back the original product name

            await Task.CompletedTask;
        }


        private static QualitySettings GetActiveQualitySettings()
        {
            return Resources.FindObjectsOfTypeAll<QualitySettings>().FirstOrDefault(qs => !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(qs)));
        }


        private static async Task SetQualitySettingsPreset()
        {
            var target = GetActiveQualitySettings();

            if (target == null)
            {
                Debug.LogError("No active QualitySettings instance found.");

                return;
            }

            var filter = "Quality";
            var preset = GetPreset(filter);

            if (preset == null)
            {
                Debug.LogError($"Preset not found for {filter}");

                return;
            }

            if (preset.ApplyTo(target))
            {
                var path = AssetDatabase.GetAssetPath(target);

                if (!string.IsNullOrEmpty(path))
                {
                    _paths.Add(path);
                }
            }
            else
            {
                Debug.LogError($"Failed to apply preset to {filter}");
            }

            await Task.CompletedTask;
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


        private static async Task Import(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Skipping import for empty or null path.");

                return;
            }

            AssetDatabase.ImportAsset(path);
            await Task.CompletedTask;
        }


        /// <summary>
        ///     Based on Warped Imagination: https://www.youtube.com/watch?v=KFmP1Q8NySo
        /// </summary>
        [MenuItem("SOSXR/Setup/Setup Default Presets")]
        private static void SetupDefaultPresetsMenuOption()
        {
            var filter = "Default";
            var fullFilter = _defaultFilter + filter;
            var guids = AssetDatabase.FindAssets(fullFilter, _foldersToSearch);

            if (guids.Length == 0)
            {
                Debug.LogWarning("No presets found with filter " + fullFilter);

                return;
            }
            
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
                
                var presetFilter = preset.name.Replace("_SOSXR_Default", "").Trim().Split('_').Last();
                
                // This means that the preset nane should end in _SOSXR_Default, and that before that should be the name of the filter I want to apply
                // This is a clunky way, but it works for now. I'll improve it later.
                // So the preset name TextureImporter_icon_SOSXR_Default will apply to TextureImporter, and the filter will be icon
                // This means that any texture with the word icon in the name will have this preset applied to it.
                // This should be improved since this is vastly error prone.

                list.Add(new DefaultPreset(presetFilter, preset));

                Preset.SetDefaultPresetsForType(type, list.ToArray());
            }
        }
    }
}