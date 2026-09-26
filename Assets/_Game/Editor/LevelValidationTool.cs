using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ColorSort.Core.Board;
using ColorSort.Core.Solving;
using ColorSort.Game.Data;
using UnityEditor;
using UnityEngine;

namespace ColorSort.EditorTools
{
    public static class LevelValidationTool
    {
        private const string ReportFileName = "LevelReport.md";

        [MenuItem("ColorSort/Validate Levels", priority = 0)]
        public static void ValidateAllCatalogs()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            if (guids.Length == 0)
            {
                Debug.LogWarning("No LevelCatalog asset found.");
                return;
            }

            foreach (string guid in guids)
                Validate(AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        public static bool Validate(LevelCatalog catalog)
        {
            var report = new StringBuilder();
            report.AppendLine($"# Level report: {catalog.name}");
            report.AppendLine();
            report.AppendLine($"Generated {DateTime.Now:yyyy-MM-dd HH:mm} by ColorSort > Validate Levels.");
            report.AppendLine();
            report.AppendLine("| # | Asset | Bottles | Colours | Optimal | Max moves 3★ / 2★ | Time (s) | States | Issues |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|");

            var firstLevelByFingerprint = new Dictionary<string, int>();
            ColorPalette palette = FindPalette();
            int problems = 0;

            try
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    LevelDefinition level = catalog.Levels[i];
                    int number = i + 1;

                    if (EditorUtility.DisplayCancelableProgressBar("Validating levels", $"Level {number}/{catalog.Count}", (float)i / catalog.Count))
                    {
                        Debug.LogWarning("Level validation cancelled; results are partial.");
                        break;
                    }

                    if (level == null)
                    {
                        problems++;
                        report.AppendLine($"| {number} | (missing) | | | | | | | Empty catalog slot |");
                        continue;
                    }

                    if (!ValidateLevel(level, number, palette, firstLevelByFingerprint, report))
                        problems++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            string reportPath = WriteReport(report.ToString());

            string summary = $"Validated {catalog.Count} levels in '{catalog.name}': {problems} with problems. Report: {reportPath}";
            if (problems > 0)
                Debug.LogError(summary, catalog);
            else
                Debug.Log(summary, catalog);
            return problems == 0;
        }

        private static bool ValidateLevel(LevelDefinition level, int number, ColorPalette palette, Dictionary<string, int> firstLevelByFingerprint, StringBuilder report)
        {
            var issues = new List<string>();
            BoardState board;
            try
            {
                board = level.CreateBoard();
            }
            catch (ArgumentException e)
            {
                SetOptimalMoves(level, 0);
                report.AppendLine($"| {number} | {level.name} | | | | | | | Invalid layout: {e.Message} |");
                return false;
            }

            LevelReport result = LevelValidator.Validate(board);
            issues.AddRange(result.Errors);
            issues.AddRange(result.Warnings.Select(w => "Warning: " + w));
            if (palette != null)
                issues.AddRange(FindConfusableColours(board, palette).Select(w => "Warning: " + w));

            bool unique = true;
            string fingerprint = board.Fingerprint();
            if (firstLevelByFingerprint.TryGetValue(fingerprint, out int original))
            {
                unique = false;
                issues.Add($"Duplicate of level {original}");
            }
            else
            {
                firstLevelByFingerprint.Add(fingerprint, number);
            }

            SetOptimalMoves(level, result.OptimalMoves);

            string stars = "", time = "";
            if (result.IsPlayable)
            {
                stars = $"{level.Stars.ThreeStarMaxMoves} / {level.Stars.TwoStarMaxMoves}";
                time = level.TimeLimitSeconds.ToString("0");
            }

            report.AppendLine(
                $"| {number} | {level.name} | {board.BottleCount} | {board.CountColors().Count} | " +
                $"{(result.IsPlayable ? result.OptimalMoves.ToString() : "-")} | {stars} | {time} | " +
                $"{result.Solve?.ExploredStates.ToString("N0") ?? "-"} | {string.Join("; ", issues)} |");

            if (!result.IsPlayable)
                Debug.LogError($"Level {number} ({level.name}): {string.Join("; ", result.Errors)}", level);

            return result.IsPlayable && unique;
        }

        private static IEnumerable<string> FindConfusableColours(BoardState board, ColorPalette palette)
        {
            byte[] ids = board.CountColors().Keys.OrderBy(id => id).ToArray();
            for (int a = 0; a < ids.Length; a++)
            {
                if (ids[a] >= palette.Count)
                    yield return $"Colour {ids[a]} is not in the palette";

                for (int b = a + 1; b < ids.Length; b++)
                {
                    if (PaletteChecks.AreConfusable(palette.GetColor(ids[a]), palette.GetColor(ids[b])))
                        yield return $"{palette.GetName(ids[a])} and {palette.GetName(ids[b])} look alike";
                }
            }
        }

        private static ColorPalette FindPalette()
        {
            string guid = AssetDatabase.FindAssets("t:" + nameof(ColorPalette)).FirstOrDefault();
            if (guid == null)
            {
                Debug.LogWarning("No ColorPalette asset found; skipping colour checks.");
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<ColorPalette>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void SetOptimalMoves(LevelDefinition level, int moves)
        {
            if (level.OptimalMoves == moves)
                return;
            Undo.RecordObject(level, "Validate level");
            level.EditorSetOptimalMoves(moves);
            EditorUtility.SetDirty(level);
        }

        private static string WriteReport(string contents)
        {
            string docs = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs"));
            Directory.CreateDirectory(docs);
            string path = Path.Combine(docs, ReportFileName);
            File.WriteAllText(path, contents);
            return path;
        }
    }
}
