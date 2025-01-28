// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Unity Editor window for exporting and releasing Unity sample scenes for internal ISDK development.
/// This tool is designed to be used by ISDK developers to package and release sample scenes
/// directly from a checked out GitHub project.
/// </summary>
/// <remarks>
///     Prerequisites:
///     - Git must be installed and available in the system PATH
///     - GitHub CLI (gh) must be installed and authenticated
///     - User must have appropriate GitHub repository permissions
///     Usage:
///     1. Open the window from Tools > Export & Release Window
///     2. Set the version tag (e.g., v1.0.0)
///     3. Add release notes
///     4. Configure the scenes source path (defaults to Assets/ShowcaseSamples)
///     5. Click "Export Scenes & Release" to:
///     - Export all scenes in the specified path as .unitypackage files
///     - Create a git tag
///     - Create a GitHub release with the exported packages
/// </remarks>
public class UnityExportAndReleaseWindow : EditorWindow
{
    // Serialized fields to persist values between Unity session restarts
    [SerializeField] private string versionTag = "v1.0.0";
    [SerializeField] private string releaseNotes = "Release notes here...";
    [SerializeField] private bool exportWithDependencies = true;
    [SerializeField] private string customExportPath = "Exports/UnityPackages";
    [SerializeField] private string scenesSourcePath = "Assets/ShowcaseSamples";

    private Vector2 scrollPosition;
    private bool isProcessing;
    private const string PREFS_KEY_EXPORT_PATH = "UnityExportAndRelease_ExportPath";

    /// <summary>
    /// Creates or focuses the Export & Release window.
    /// </summary>
    [MenuItem("Tools/Export & Release Window")]
    private static void Init()
    {
        var window =
            (UnityExportAndReleaseWindow)GetWindow(
                typeof(UnityExportAndReleaseWindow),
                false,
                "Export & Release"
            );
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    /// <summary>
    /// Called when the window is enabled. Loads saved preferences.
    /// </summary>
    private void OnEnable()
    {
        // Load saved export path
        customExportPath = EditorPrefs.GetString(PREFS_KEY_EXPORT_PATH, customExportPath);
    }

    /// <summary>
    /// Draws the editor window GUI.
    /// </summary>
    private void OnGUI()
    {
        using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
        {
            scrollPosition = scrollView.scrollPosition;
            DrawSettings();
            DrawExportButton();
        }

        // Show processing indicator
        if (isProcessing)
            EditorGUI.ProgressBar(
                new Rect(5, position.height - 25, position.width - 10, 20),
                0.5f,
                "Processing..."
            );
    }

    /// <summary>
    /// Draws the main settings GUI section including version, notes, and paths.
    /// </summary>
    private void DrawSettings()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);

        using (new EditorGUI.IndentLevelScope())
        {
            versionTag = EditorGUILayout.TextField("Version Tag", versionTag);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Release Notes", EditorStyles.boldLabel);
                releaseNotes = EditorGUILayout.TextArea(
                    releaseNotes,
                    GUILayout.MinHeight(100)
                );
            }

            EditorGUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Scenes Source Path");
                EditorGUILayout.SelectableLabel(scenesSourcePath, EditorStyles.textField);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var newPath = EditorUtility.OpenFolderPanel(
                        "Select Scenes Source Directory",
                        scenesSourcePath,
                        ""
                    );
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        // Convert absolute path to relative Asset path if necessary
                        if (newPath.Contains(Application.dataPath))
                            newPath = "Assets" + newPath.Substring(Application.dataPath.Length);
                        scenesSourcePath = newPath;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Export Path");
                EditorGUILayout.SelectableLabel(customExportPath, EditorStyles.textField);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    var newPath = EditorUtility.OpenFolderPanel(
                        "Select Export Directory",
                        customExportPath,
                        ""
                    );
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        customExportPath = newPath;
                        EditorPrefs.SetString(PREFS_KEY_EXPORT_PATH, newPath);
                    }
                }
            }

            exportWithDependencies = EditorGUILayout.Toggle(
                "Export With Dependencies",
                exportWithDependencies
            );
        }
    }

    /// <summary>
    /// Draws the main export button and handles its enabled state.
    /// </summary>
    private void DrawExportButton()
    {
        EditorGUILayout.Space(20);

        using (new EditorGUI.DisabledGroupScope(isProcessing))
        {
            GUI.enabled = !string.IsNullOrEmpty(versionTag);
            if (GUILayout.Button("Export Scenes & Release", GUILayout.Height(30)))
                if (ValidateSettings())
                    ExportScenesAndUpload(versionTag, releaseNotes);
            GUI.enabled = true;
        }
    }

    /// <summary>
    /// Validates all settings before starting the export process.
    /// </summary>
    /// <returns>True if all settings are valid, false otherwise.</returns>
    private bool ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(versionTag))
        {
            EditorUtility.DisplayDialog(
                "Validation Error",
                "Version tag cannot be empty.",
                "OK"
            );
            return false;
        }

        if (!Directory.Exists(customExportPath))
        {
            var create = EditorUtility.DisplayDialog(
                "Directory Missing",
                $"Export directory does not exist:\n{customExportPath}\n\nCreate it?",
                "Create",
                "Cancel"
            );

            if (create)
                try
                {
                    Directory.CreateDirectory(customExportPath);
                }
                catch (Exception e)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        $"Failed to create directory:\n{e.Message}",
                        "OK"
                    );
                    return false;
                }
            else
                return false;
        }

        return true;
    }

    /// <summary>
    /// Main export and upload process. Handles the entire workflow from scene export to GitHub release.
    /// </summary>
    /// <param name="tag">The version tag for the release (e.g., v1.0.0)</param>
    /// <param name="notes">Release notes to be included in the GitHub release</param>
    private async void ExportScenesAndUpload(string tag, string notes)
    {
        isProcessing = true;

        try
        {
            // 1. Export scenes from specific path as .unitypackage
            var sceneGUIDs = AssetDatabase.FindAssets("t:Scene", new[] { scenesSourcePath });
            if (sceneGUIDs.Length == 0) throw new Exception($"No scenes found in path: {scenesSourcePath}");

            var exportedPackages = new List<string>();
            foreach (var guid in sceneGUIDs)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var packagePath = ExportScene(scenePath);
                if (!string.IsNullOrEmpty(packagePath)) exportedPackages.Add(packagePath);
            }

            // 2. Git operations
            await Task.Run(() => { RunGitCommands(tag, notes, exportedPackages); });

            EditorUtility.DisplayDialog(
                "Success",
                $"Export & Release process completed for {tag}.",
                "OK"
            );
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Export & Release failed:\n{e.Message}",
                "OK"
            );
            Debug.LogError($"Export & Release failed: {e}");
        }
        finally
        {
            isProcessing = false;
            Repaint();
        }
    }

    /// <summary>
    /// Exports a single scene to a .unitypackage file.
    /// </summary>
    /// <param name="scenePath">The asset path to the scene</param>
    /// <returns>The path to the exported package, or null if export failed</returns>
    private string ExportScene(string scenePath)
    {
        try
        {
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var packagePath = Path.Combine(
                customExportPath,
                $"{sceneName}_{versionTag}.unitypackage"
            );

            var options = exportWithDependencies ? ExportPackageOptions.Recurse : ExportPackageOptions.Default;

            AssetDatabase.ExportPackage(
                scenePath,
                packagePath,
                options
            );

            Debug.Log($"Exported {sceneName} to {packagePath}");
            return packagePath;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to export scene {scenePath}: {e}");
            return null;
        }
    }

    /// <summary>
    /// Executes the Git and GitHub CLI commands to create a tag and release.
    /// </summary>
    /// <param name="tag">The version tag</param>
    /// <param name="notes">Release notes</param>
    /// <param name="packages">List of paths to the exported .unitypackage files</param>
    private void RunGitCommands(string tag, string notes, List<string> packages)
    {
        // Verify git and gh are installed
        if (!IsCommandAvailable("git") || !IsCommandAvailable("gh"))
            throw new Exception(
                "Git and GitHub CLI must be installed and available in PATH"
            );

        // Create and push tag
        RunCommand("git", $"tag {tag}");
        RunCommand("git", $"push origin {tag}");

        // Create release with files
        if (packages.Count == 0)
            Debug.LogWarning(
                "No unitypackage files to upload. Release will be created without files."
            );

        // Build release command
        var argParts = new List<string>
        {
            "release create",
            tag,
            "--notes",
            $"\"{notes.Replace("\"", "\\\"")}\"" // Escape quotes in notes
        };

        foreach (var filePath in packages) argParts.Add($"\"{filePath.Replace("\"", "\\\"")}\"");

        RunCommand("gh", string.Join(" ", argParts));
    }

    /// <summary>
    /// Checks if a command-line tool is available in the system PATH.
    /// </summary>
    /// <param name="command">The command to check (e.g., "git" or "gh")</param>
    /// <returns>True if the command is available, false otherwise</returns>
    private bool IsCommandAvailable(string command)
    {
        try
        {
            RunCommand(command, "--version");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Executes a command-line process and handles its output.
    /// </summary>
    /// <param name="command">The command to run</param>
    /// <param name="arguments">Command arguments</param>
    /// <exception cref="System.Exception">Thrown when the process fails to start or returns a non-zero exit code</exception>
    private void RunCommand(string command, string arguments)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            if (process == null) throw new Exception($"Failed to start process: {command}");

            var output = process.StandardOutput.ReadToEnd();
            var errorOutput = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception(
                    $"Command failed with exit code {process.ExitCode}:\n{errorOutput}"
                );

            if (!string.IsNullOrEmpty(output)) Debug.Log($"[{command} output] {output}");
            if (!string.IsNullOrEmpty(errorOutput)) Debug.LogWarning($"[{command} error] {errorOutput}");
        }
    }
}
