# Repository: Essential Importer for SOSXR

- **Author:** Maarten R. Struijk Wilbrink
- **For:** Leiden University, SOSXR Project
- **License:** Fully open source – contributions and modifications are welcome.

---

## Installation Guide

### Prerequisites

1. **Unity Version:** Start with the latest Unity LTS version and choose a project template (e.g., VR Template).
2. **Project Setup:**
    - Open your Unity project.
    - Go to **Window > Package Manager**.
    - Click the **+** button, and select **Add package from git URL...**.
    - Paste the following URL: `https://github.com/mrstruijk/essentialimporter.git`.
    - Press **Add** to include the package.

3. **Project Initialization:**
    - In the top bar, navigate to **SOSXR > Setup > Create JSON templates**.
    - This will generate new templates in `Assets/_SOSXR/Resources`. Adjust as necessary.
    - Return to **SOSXR > Setup** and select **Run Full Project Setup**.

### Add Package Dependencies

To ensure all dependencies are installed, add the following entries to your `manifest.json` file (located at `Packages/manifest.json`). **Note**: Avoid duplicate entries, especially if you already have GLTFast or similar packages installed. Each line should end with a comma, except for the last one.

```json
{
    "com.browar.editor-toolbox": "https://github.com/arimger/Unity-Editor-Toolbox.git#upm",
    "com.danieleverland.scriptableobjectarchitecture": "https://github.com/solo-fsw/sosxr-unity-scriptableobjectarchitecture.git",
    "com.kylewbanks.scenerefattribute": "https://github.com/KyleBanks/scene-ref-attribute.git",
    "com.mischief.markdownviewer": "https://github.com/solo-fsw/sosxr-unity-markdownviewer.git",
    "com.monitor1394.xcharts": "https://github.com/XCharts-Team/XCharts.git",
    "com.sosxr.additionalgizmos": "https://github.com/solo-fsw/sosxr-unity-additionalgizmos.git",
    "com.sosxr.additionalunityevents": "https://github.com/solo-fsw/sosxr-unity-additionalunityevents.git",
    "com.sosxr.assetdependency": "https://github.com/solo-fsw/sosxr-unity-assetdependency.git",
    "com.sosxr.buildhelpers": "https://github.com/solo-fsw/sosxr-unity-buildhelpers.git",
    "com.sosxr.editortools": "https://github.com/solo-fsw/sosxr-unity-editortools.git",
    "com.sosxr.enhancedlogger": "https://github.com/solo-fsw/sosxr-unity-enhancedlogger.git",
    "com.sosxr.extendingtimeline": "https://github.com/solo-fsw/sosxr-unity-timelineextensions.git",
    "com.sosxr.readmehelpers": "https://github.com/solo-fsw/sosxr-unity-readmehelpers.git",
    "com.sosxr.extensionmethods": "https://github.com/solo-fsw/sosxr-unity-extensionmethods.git",
    "com.sosxr.simplehelpers": "https://github.com/solo-fsw/sosxr-unity-simplehelpers.git",
    "com.jknight.swatchr": "https://github.com/solo-fsw/sosxr-unity-swatchr.git",
    "com.tarodev.autosave": "https://github.com/solo-fsw/sosxr-unity-autosave.git",
    "com.yasirkula.ingamedebugconsole": "https://github.com/solo-fsw/sosxr-unity-ingamedebugconsole.git",
    "com.unity.cloud.gltfast": "6.8.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
}
```

### Installing UXF

Download and install [UXF](https://github.com/immersivecognition/unity-experiment-framework/releases/latest) as a `.unitypackage` file.
