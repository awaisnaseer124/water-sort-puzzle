using System.Collections.Generic;
using System.Linq;
using ColorSort.Core.Generation;
using ColorSort.Game.Data;
using UnityEditor;
using UnityEngine;

namespace ColorSort.EditorTools
{
    public sealed class LevelGeneratorWindow : EditorWindow
    {
        private const string OutputFolder = "Assets/_Game/Data/Levels";

        private const int MaxRelaxSteps = 3;

        [SerializeField] private LevelCatalog _catalog;
        [SerializeField] private ColorPalette _palette;
        [SerializeField] private int _count = 39;
        [SerializeField] private int _seed = 20260925;
        [SerializeField] private int _capacity = 4;
        [SerializeField] private int _emptyBottles = 2;
        [SerializeField] private int _startColours = 7;
        [SerializeField] private int _endColours = 10;

        [Tooltip("Random boards average ~3.1 optimal moves per colour; higher keeps only the harder ones.")]
        [SerializeField] private float _startMovesPerColour = 3.0f;
        [SerializeField] private float _endMovesPerColour = 3.2f;

        [MenuItem("ColorSort/Level Generator...", priority = 20)]
        private static void Open() => GetWindow<LevelGeneratorWindow>("Level Generator");

        private void OnEnable()
        {
            if (_catalog == null)
                _catalog = FindFirstAsset<LevelCatalog>();
            if (_palette == null)
                _palette = FindFirstAsset<ColorPalette>();
        }

        private void OnGUI()
        {
            _catalog = (LevelCatalog)EditorGUILayout.ObjectField("Catalog", _catalog, typeof(LevelCatalog), false);
            _palette = (ColorPalette)EditorGUILayout.ObjectField("Palette", _palette, typeof(ColorPalette), false);

            EditorGUILayout.Space();
            _count = Mathf.Max(1, EditorGUILayout.IntField("Levels to add", _count));
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _capacity = Mathf.Clamp(EditorGUILayout.IntField("Bottle capacity", _capacity), 2, 8);
            _emptyBottles = Mathf.Clamp(EditorGUILayout.IntField("Empty bottles", _emptyBottles), 1, 4);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Difficulty ramp (first → last)", EditorStyles.boldLabel);
            _startColours = Mathf.Clamp(EditorGUILayout.IntField("Colours from", _startColours), 2, 16);
            _endColours = Mathf.Clamp(EditorGUILayout.IntField("Colours to", _endColours), _startColours, 16);
            _startMovesPerColour = EditorGUILayout.Slider("Min moves/colour from", _startMovesPerColour, 1f, 4f);
            _endMovesPerColour = EditorGUILayout.Slider("Min moves/colour to", _endMovesPerColour, 1f, 4f);

            EditorGUILayout.Space();
            if (_catalog != null)
            {
                Step first = StepAt(0), last = StepAt(_count - 1);
                EditorGUILayout.HelpBox(
                    $"Adds levels {_catalog.Count + 1}–{_catalog.Count + _count}.\n" +
                    $"First: {first.Colours} colours, ≥ {first.MinMoves} moves.  Last: {last.Colours} colours, ≥ {last.MinMoves} moves.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(_catalog == null || _palette == null))
            {
                if (GUILayout.Button("Generate and append", GUILayout.Height(30)))
                    Generate();
            }
        }

        private void Generate()
        {
            List<byte> colourIds = PaletteChecks.DistinctIds(_palette);
            if (colourIds.Count < _endColours)
            {
                EditorUtility.DisplayDialog("Level Generator", $"The palette has only {colourIds.Count} distinguishable colours; lower 'Colours to'.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Level Generator",
                    $"Create {_count} level assets in {OutputFolder} and append them to '{_catalog.name}'?", "Generate", "Cancel"))
                return;

            var fingerprints = new HashSet<string>(_catalog.Levels.Where(l => l != null).Select(l => l.CreateBoard().Fingerprint()));
            var created = new List<LevelDefinition>();

            try
            {
                for (int k = 0; k < _count; k++)
                {
                    int number = _catalog.Count + created.Count + 1;
                    if (EditorUtility.DisplayCancelableProgressBar("Generating levels", $"Level {number}", (float)k / _count))
                        break;

                    Step step = StepAt(k);
                    int levelSeed = _seed + k;
                    var random = new System.Random(levelSeed);
                    List<byte> colours = colourIds.OrderBy(_ => random.Next()).Take(step.Colours).ToList();

                    GeneratedLevel level = GenerateUnique(step, random, colours, fingerprints, out int minMovesUsed);
                    if (level == null)
                    {
                        Debug.LogError($"Could not generate level {number} ({step.Colours} colours, ≥ {step.MinMoves} moves). Stopped.");
                        break;
                    }

                    fingerprints.Add(level.Board.Fingerprint());
                    string source = $"Generated seed={levelSeed} colours={step.Colours} capacity={_capacity} empties={_emptyBottles} minMoves={minMovesUsed}";
                    created.Add(CreateLevelAsset(number, level, source));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (created.Count == 0)
                return;

            Undo.RecordObject(_catalog, "Append generated levels");
            _catalog.EditorSetLevels(_catalog.Levels.Concat(created));
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();

            Debug.Log($"Appended {created.Count} generated levels to '{_catalog.name}'.", _catalog);
            LevelValidationTool.Validate(_catalog);
        }

        private GeneratedLevel GenerateUnique(Step step, System.Random random, List<byte> colours, HashSet<string> fingerprints, out int minMovesUsed)
        {
            for (int relax = 0; relax <= MaxRelaxSteps; relax++)
            {
                minMovesUsed = Mathf.Max(1, step.MinMoves - relax * 2);
                var settings = new GeneratorSettings
                {
                    Colours = step.Colours,
                    Capacity = _capacity,
                    EmptyBottles = _emptyBottles,
                    MinOptimalMoves = minMovesUsed,
                };

                if (LevelGenerator.TryGenerate(settings, random, out GeneratedLevel level, colours)
                    && !fingerprints.Contains(level.Board.Fingerprint()))
                    return level;
            }

            minMovesUsed = 0;
            return null;
        }

        private static LevelDefinition CreateLevelAsset(int number, GeneratedLevel generated, string source)
        {
            var level = CreateInstance<LevelDefinition>();
            level.EditorSetBottles(generated.Board.Bottles.Select(b => new BottleLayout(b.Capacity, b.ToArray())));
            level.EditorSetOptimalMoves(generated.OptimalMoves);
            level.EditorSetSource(source);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/Level_{number:000}.asset");
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        private Step StepAt(int index)
        {
            float t = _count <= 1 ? 1f : index / (float)(_count - 1);
            int colours = Mathf.RoundToInt(Mathf.Lerp(_startColours, _endColours, t));
            float movesPerColour = Mathf.Lerp(_startMovesPerColour, _endMovesPerColour, t);
            return new Step(colours, Mathf.CeilToInt(colours * movesPerColour));
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string guid = AssetDatabase.FindAssets("t:" + typeof(T).Name).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private readonly struct Step
        {
            public Step(int colours, int minMoves)
            {
                Colours = colours;
                MinMoves = minMoves;
            }

            public int Colours { get; }
            public int MinMoves { get; }
        }
    }
}
