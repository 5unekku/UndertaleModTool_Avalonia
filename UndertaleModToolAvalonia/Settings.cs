using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Underanalyzer.Decompiler;

namespace UndertaleModTool
{
    public class Settings
    {
        public static readonly string AppDataFolder = Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UndertaleModTool");

        /// <summary>Whether file associations settings should be prompted for on startup.</summary>
        public static bool ShouldPromptForAssociations { get; set; } = false;

        public string Version { get; set; } = MainWindow.Version;
        public string GameMakerStudioPath { get; set; } = "%appdata%\\GameMaker-Studio";
        public string GameMakerStudio2RuntimesPath { get; set; } = "%ProgramData%\\GameMakerStudio2\\Cache\\runtimes";
        public bool AssetOrderSwappingEnabled { get; set; } = false;
        public bool AutomaticFileAssociation { get; set; } = true;
        public bool TempRunMessageShow { get; set; } = true;

        public bool WarnOnClose { get; set; } = true;

        private double _globalGridWidth = 20;
        private double _globalGridHeight = 20;
        public double GlobalGridWidth { get => _globalGridWidth; set { if (value >= 0) _globalGridWidth = value; } }
        public bool GridWidthEnabled { get; set; } = false;
        public double GlobalGridHeight { get => _globalGridHeight; set { if (value >= 0) _globalGridHeight = value; } }
        public bool GridHeightEnabled { get; set; } = false;

        public double GlobalGridThickness { get; set; } = 1;
        public bool GridThicknessEnabled { get; set; } = false;

        public string TransparencyGridColor1 { get; set; } = "#FF666666";
        public string TransparencyGridColor2 { get; set; } = "#FF999999";

        public bool EnableDarkMode { get; set; } = false;
        public bool ShowDebuggerOption { get; set; } = false;
        public DecompilerSettings DecompilerSettings { get; set; }
        public const string DefaultInstanceIdPrefix = "inst_";
        public string InstanceIdPrefix { get; set; } = DefaultInstanceIdPrefix;

        public bool ShowNullEntriesInResourceTree { get; set; } = false;

        // note: the wpf MainWindowPlacement (Win32 WINDOWPLACEMENT) is dropped in the avalonia port.
        public bool RememberWindowPlacements { get; set; } = false;

        public bool RecompileAllCodeSourcesOnProjectSave { get; set; } = false;

        public static Settings Instance { get; private set; }

        public static JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public Settings()
        {
            Instance = this;
        }

        public static void Load()
        {
            DecompilerSettings existingDecompilerSettings = Instance?.DecompilerSettings;

            try
            {
                string path = Path.Join(AppDataFolder, "settings.json");
                if (!File.Exists(path))
                {
                    _ = new Settings() { DecompilerSettings = existingDecompilerSettings ?? new() };
                    Save();
                    ShouldPromptForAssociations = true;
                    return;
                }

                byte[] bytes = File.ReadAllBytes(path);
                JsonSerializer.Deserialize<Settings>(bytes, JsonOptions);

                bool changed = false;
                if (Instance.Version != MainWindow.Version)
                    changed = true;

                if (existingDecompilerSettings is not null)
                    Instance.DecompilerSettings = existingDecompilerSettings;
                Instance.DecompilerSettings ??= new();

                if (Instance.DecompilerSettings.UnknownArgumentNamePattern == "argument{0}")
                    Instance.DecompilerSettings.UnknownArgumentNamePattern = "arg{0}";

                Instance.Version = MainWindow.Version;
                if (changed)
                    Save();
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to load settings.json! Using default values.\n{e.Message}");
                new Settings() { DecompilerSettings = existingDecompilerSettings ?? new() };
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);
                string path = Path.Join(AppDataFolder, "settings.json");
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(Instance, JsonOptions);
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to save settings.json!\n{e.Message}");
            }
        }
    }

    /// <summary>GML decompiler settings instance used by the main UndertaleModTool tool.</summary>
    public class DecompilerSettings : IDecompileSettings
    {
        [JsonConverter(typeof(JsonStringEnumConverter<IndentStyleKind>))]
        public enum IndentStyleKind
        {
            FourSpaces,
            TwoSpaces,
            Tabs
        }

        [JsonIgnore]
        private DecompileSettings _innerSettings;

        public IndentStyleKind IndentStyle { get; set; }

        [JsonIgnore]
        public string IndentString
        {
            get => IndentStyle switch
            {
                IndentStyleKind.FourSpaces => "    ",
                IndentStyleKind.TwoSpaces => "  ",
                IndentStyleKind.Tabs => "\t",
                _ => throw new Exception("Unknown indent style")
            };
        }

        public bool UseSemicolon { get => _innerSettings.UseSemicolon; set => _innerSettings.UseSemicolon = value; }
        public bool UseCSSColors { get => _innerSettings.UseCSSColors; set => _innerSettings.UseCSSColors = value; }
        public bool PrintWarnings { get => _innerSettings.PrintWarnings; set => _innerSettings.PrintWarnings = value; }
        public bool MacroDeclarationsAtTop { get => _innerSettings.MacroDeclarationsAtTop; set => _innerSettings.MacroDeclarationsAtTop = value; }
        public bool EmptyLineAfterBlockLocals { get => _innerSettings.EmptyLineAfterBlockLocals; set => _innerSettings.EmptyLineAfterBlockLocals = value; }
        public bool EmptyLineAroundEnums { get => _innerSettings.EmptyLineAroundEnums; set => _innerSettings.EmptyLineAroundEnums = value; }
        public bool EmptyLineAroundBranchStatements { get => _innerSettings.EmptyLineAroundBranchStatements; set => _innerSettings.EmptyLineAroundBranchStatements = value; }
        public bool EmptyLineBeforeSwitchCases { get => _innerSettings.EmptyLineBeforeSwitchCases; set => _innerSettings.EmptyLineBeforeSwitchCases = value; }
        public bool EmptyLineAfterSwitchCases { get => _innerSettings.EmptyLineAfterSwitchCases; set => _innerSettings.EmptyLineAfterSwitchCases = value; }
        public bool EmptyLineAroundFunctionDeclarations { get => _innerSettings.EmptyLineAroundFunctionDeclarations; set => _innerSettings.EmptyLineAroundFunctionDeclarations = value; }
        public bool EmptyLineAroundStaticInitialization { get => _innerSettings.EmptyLineAroundStaticInitialization; set => _innerSettings.EmptyLineAroundStaticInitialization = value; }
        public bool OpenBlockBraceOnSameLine { get => _innerSettings.OpenBlockBraceOnSameLine; set => _innerSettings.OpenBlockBraceOnSameLine = value; }
        public bool RemoveSingleLineBlockBraces { get => _innerSettings.RemoveSingleLineBlockBraces; set => _innerSettings.RemoveSingleLineBlockBraces = value; }
        public bool CleanupTry { get => _innerSettings.CleanupTry; set => _innerSettings.CleanupTry = value; }
        public bool CleanupElseToContinue { get => _innerSettings.CleanupElseToContinue; set => _innerSettings.CleanupElseToContinue = value; }
        public bool CleanupDefaultArgumentValues { get => _innerSettings.CleanupDefaultArgumentValues; set => _innerSettings.CleanupDefaultArgumentValues = value; }
        public bool CleanupBuiltinArrayVariables { get => _innerSettings.CleanupBuiltinArrayVariables; set => _innerSettings.CleanupBuiltinArrayVariables = value; }
        public bool CleanupLocalVarDeclarations { get => _innerSettings.CleanupLocalVarDeclarations; set => _innerSettings.CleanupLocalVarDeclarations = value; }
        public bool CreateEnumDeclarations { get => _innerSettings.CreateEnumDeclarations; set => _innerSettings.CreateEnumDeclarations = value; }
        public string UnknownEnumName { get => _innerSettings.UnknownEnumName; set => _innerSettings.UnknownEnumName = value; }
        public string UnknownEnumValuePattern { get => _innerSettings.UnknownEnumValuePattern; set => _innerSettings.UnknownEnumValuePattern = value; }
        public string UnknownArgumentNamePattern { get => _innerSettings.UnknownArgumentNamePattern; set => _innerSettings.UnknownArgumentNamePattern = value; }
        public bool AllowLeftoverDataOnStack { get => _innerSettings.AllowLeftoverDataOnStack; set => _innerSettings.AllowLeftoverDataOnStack = value; }

        public DecompilerSettings()
        {
            RestoreDefaults();
        }

        public void RestoreDefaults()
        {
            _innerSettings = new DecompileSettings()
            {
                UnknownArgumentNamePattern = "arg{0}",
                RemoveSingleLineBlockBraces = true,
                EmptyLineAroundBranchStatements = true,
                EmptyLineBeforeSwitchCases = true
            };
            IndentStyle = IndentStyleKind.FourSpaces;
        }

        public bool TryGetPredefinedDouble(double value, [MaybeNullWhen(false)] out string result, out bool isResultMultiPart)
        {
            return _innerSettings.TryGetPredefinedDouble(value, out result, out isResultMultiPart);
        }
    }
}
