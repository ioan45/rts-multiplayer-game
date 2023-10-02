using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class PreBuildAction : IPreprocessBuildWithReport
{
    // Required by interface, not used because there is no other OnPreprocessBuild callback.
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, "");
    }
}
