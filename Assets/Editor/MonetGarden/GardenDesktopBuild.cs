using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace MonetGarden.Editor
{
    public static class GardenDesktopBuild
    {
        [MenuItem("Tools/Monet Garden/Build Windows Demo")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before building the demo.");
            string output="Builds/MonetGarden111/MonetGardenVR.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/MonetGarden_Prototype.unity"},
                locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
            });
            Directory.CreateDirectory("Documentation/Validation");
            string text=$"Windows build {DateTime.Now:O}\nResult: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\nOutput: {output}\n";
            foreach(var step in report.steps)foreach(var message in step.messages)
                if(message.type==LogType.Error || message.type==LogType.Exception || message.type==LogType.Warning)text+=$"{message.type}: {message.content}\n";
            File.WriteAllText("Documentation/Validation/round2_windows_build.txt",text);
            Debug.Log("MONET_WINDOWS_BUILD: "+report.summary.result);
        }
    }
}
