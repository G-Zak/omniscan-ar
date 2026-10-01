using System.IO;
using UnityEditor.Android;

namespace OmniScan.Editor
{
    // Unity OpenXR and Meta's OVRPlugin both ship libopenxr_loader.so; Gradle refuses to merge duplicates.
    public class OpenXRLoaderDedupe : IPostGenerateGradleAndroidProject
    {
        const string Marker = "// OmniScan: dedupe libopenxr_loader.so";
        const string Anchor = "useLegacyPackaging true";

        public int callbackOrder => 0;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var gradle = Path.Combine(path, "..", "launcher", "build.gradle");
            if (!File.Exists(gradle)) return;

            var text = File.ReadAllText(gradle);
            if (text.Contains(Marker) || !text.Contains(Anchor)) return;

            text = text.Replace(Anchor, $"{Anchor}\n        }}\n        {Marker}\n        jniLibs {{\n            pickFirsts += ['**/libopenxr_loader.so']");
            File.WriteAllText(gradle, text);
        }
    }
}
