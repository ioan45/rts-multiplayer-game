using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class PostBuildAction : IPostprocessBuildWithReport
{
    // Required by interface, not used because there is no other OnPostprocessBuild callback.
    public int callbackOrder { get { return 0; } }

    public void OnPostprocessBuild(BuildReport report)
    {
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, new string[]{ "BYPASS_UNITY_SERVICES", "USING_LOCAL_SERVERS" });
    }
}
