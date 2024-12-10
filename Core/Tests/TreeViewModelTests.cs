using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileGuard.Core.ViewModels;
using FileGuard.Core.Models;
using FileGuard.Core.Cache;
using FileGuard.Core.Logging;

namespace FileGuard.Core.Tests
{
    public class TreeViewModelTests
    {
        private readonly ILogger _logger;
        private readonly string _testPath;
        private readonly string _settingsPath;
        private readonly string _statePath;
        private readonly string _defaultMonitorPath;

        public TreeViewModelTests()
        {
            _logger = LoggerFactory.GetDefaultLogger();
            _testPath = Path.Combine(Path.GetTempPath(), "FileGuardTests");
            _settingsPath = Path.Combine(_testPath, "settings.json");
            _statePath = Path.Combine(_testPath, "state.json");
            _defaultMonitorPath = _testPath;
            SetupTestDirectory();
        }

        private void SetupTestDirectory()
        {
            if (Directory.Exists(_testPath))
            {
                Directory.Delete(_testPath, true);
            }

            Directory.CreateDirectory(_testPath);
            
            // Struttura principale con più livelli di profondità
            var dir1 = CreateDirectory("Level1");
            var dir2 = CreateDirectory(Path.Combine("Level1", "Level2"));
            var dir3 = CreateDirectory(Path.Combine("Level1", "Level2", "Level3"));
            var dir4 = CreateDirectory(Path.Combine("Level1", "Level2", "Level3", "Level4"));
            var dir5 = CreateDirectory(Path.Combine("Level1", "Level2", "Level3", "Level4", "Level5"));

            // File in varie posizioni della gerarchia
            CreateFile(dir1, "file1.txt");
            CreateFile(dir2, "file2.txt");
            CreateFile(dir3, "file3.txt");
            CreateFile(dir4, "file4.txt");
            CreateFile(dir5, "file5.txt");
        }

        private string CreateDirectory(string relativePath)
        {
            var path = Path.Combine(_testPath, relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        private void CreateFile(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            File.WriteAllText(path, "test content");
        }

        public void TestManualFolderCollapse()
        {
            _logger.LogInfo("Test chiusura manuale cartelle - Inizio", nameof(TreeViewModelTests));

            var config = new TreeViewModelConfig(
                settingsPath: _settingsPath,
                statePath: _statePath,
                defaultMonitorPath: _defaultMonitorPath
            );
            var treeViewModel = new TreeViewModel(config);

            // Setup iniziale
            treeViewModel.AddFolder(_testPath);
            var rootNode = treeViewModel.MonitoredNodes.FirstOrDefault() 
                ?? throw new Exception("Nodo radice non trovato");

            // Espandi tutta la gerarchia
            _logger.LogInfo("Setup: Espansione gerarchia", nameof(TreeViewModelTests));
            rootNode.IsExpanded = true;
            var level1 = GetChildByName(rootNode, "Level1");
            if (level1 == null) throw new Exception("Level1 non trovato");
            level1.IsExpanded = true;

            var level2 = GetChildByName(level1, "Level2");
            if (level2 == null) throw new Exception("Level2 non trovato");
            level2.IsExpanded = true;

            var level3 = GetChildByName(level2, "Level3");
            if (level3 == null) throw new Exception("Level3 non trovato");
            level3.IsExpanded = true;

            var level4 = GetChildByName(level3, "Level4");
            if (level4 == null) throw new Exception("Level4 non trovato");
            level4.IsExpanded = true;

            // Verifica stato iniziale
            _logger.LogInfo("Verifica stato iniziale", nameof(TreeViewModelTests));
            VerifyExpansionStates(new[] {
                (rootNode, true, "root iniziale"),
                (level1, true, "Level1 iniziale"),
                (level2, true, "Level2 iniziale"),
                (level3, true, "Level3 iniziale"),
                (level4, true, "Level4 iniziale")
            });

            // Test 1: Chiusura manuale di Level3 (livello intermedio)
            _logger.LogInfo("Test 1: Chiusura manuale Level3", nameof(TreeViewModelTests));
            level3.IsExpanded = false;

            // Verifica immediatamente dopo la chiusura
            _logger.LogInfo("Verifica stati dopo chiusura Level3", nameof(TreeViewModelTests));
            if (!rootNode.IsExpanded)
                throw new Exception("Root si è chiusa dopo la chiusura di Level3");
            if (!level1.IsExpanded)
                throw new Exception("Level1 si è chiusa dopo la chiusura di Level3");
            if (!level2.IsExpanded)
                throw new Exception("Level2 si è chiusa dopo la chiusura di Level3");
            if (level3.IsExpanded)
                throw new Exception("Level3 non si è chiusa correttamente");

            // Test 2: Chiusura manuale di Level2
            _logger.LogInfo("Test 2: Chiusura manuale Level2", nameof(TreeViewModelTests));
            level2.IsExpanded = false;

            // Verifica dopo la chiusura di Level2
            if (!rootNode.IsExpanded)
                throw new Exception("Root si è chiusa dopo la chiusura di Level2");
            if (!level1.IsExpanded)
                throw new Exception("Level1 si è chiusa dopo la chiusura di Level2");
            if (level2.IsExpanded)
                throw new Exception("Level2 non si è chiusa correttamente");

            // Test 3: Riapertura di Level2
            _logger.LogInfo("Test 3: Riapertura Level2", nameof(TreeViewModelTests));
            level2.IsExpanded = true;

            // Verifica che Level3 mantenga il suo stato chiuso
            if (!level2.IsExpanded)
                throw new Exception("Level2 non si è riaperta correttamente");
            if (level3.IsExpanded)
                throw new Exception("Level3 ha perso il suo stato chiuso dopo la riapertura di Level2");

            // Test 4: Chiusura e riapertura rapida
            _logger.LogInfo("Test 4: Chiusura e riapertura rapida", nameof(TreeViewModelTests));
            level1.IsExpanded = false;
            level1.IsExpanded = true;

            // Verifica stati finali
            VerifyExpansionStates(new[] {
                (rootNode, true, "root finale"),
                (level1, true, "Level1 finale"),
                (level2, true, "Level2 finale"),
                (level3, false, "Level3 finale")
            });

            _logger.LogInfo("Test chiusura manuale cartelle - Completato", nameof(TreeViewModelTests));
        }

        private FileSystemNodeViewModel? GetChildByName(FileSystemNodeViewModel parent, string name)
        {
            return parent.Children.OfType<FileSystemNodeViewModel>()
                .FirstOrDefault(n => n.DisplayName == name);
        }

        private void VerifyExpansionStates(params (FileSystemNodeViewModel node, bool expectedState, string context)[] checks)
        {
            foreach (var (node, expectedState, context) in checks)
            {
                if (node.IsExpanded != expectedState)
                {
                    _logger.LogError($"Stato di espansione errato per {context}", 
                        new Exception($"Atteso {expectedState}, trovato {node.IsExpanded}"), 
                        nameof(TreeViewModelTests));
                    throw new Exception($"Stato di espansione errato per {context}: atteso {expectedState}, trovato {node.IsExpanded}");
                }
            }
        }

        public void RunAllTests()
        {
            _logger.LogInfo("Inizio esecuzione test", nameof(TreeViewModelTests));
            
            TestManualFolderCollapse();
            
            _logger.LogInfo("Test completati", nameof(TreeViewModelTests));

            // Pulisci dopo i test
            try
            {
                if (Directory.Exists(_testPath))
                {
                    Directory.Delete(_testPath, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Impossibile pulire la directory di test: {ex.Message}", nameof(TreeViewModelTests));
            }
        }
    }
}
