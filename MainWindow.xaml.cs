using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.IO.Compression;
using SevenZipExtractor;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;

namespace _7DaysToDieModManager
{
    /// <summary>
    /// Main window class for the 7 Days To Die Mod Manager application.
    /// Provides functionality to load, update, delete, and manage mods.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Path to the main Mods directory
        private const string ModsFolderPath = @"C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Mods";
        // Name of the folder where inactive mods are stored
        private const string InactiveFolderName = "Inactive";
        // Path to the game executable
        private const string GameExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\7DaysToDie.exe"; // Update to your actual path

        // Collection to hold the list of mods
        private ObservableCollection<Mod> _mods;

        public MainWindow()
        {
            InitializeComponent();
            LoadMods(); // Load the list of mods when the window initializes
        }

        /// <summary>
        /// Loads the list of mods from the directory and binds them to the DataGrid.
        /// </summary>
        private void LoadMods()
        {
            _mods = new ObservableCollection<Mod>();

            // Load active mods from the main Mods folder
            LoadModsFromDirectory(ModsFolderPath, _mods);

            // Load inactive mods from the Inactive folder
            var inactiveFolderPath = GetInactiveFolderPath();
            if (Directory.Exists(inactiveFolderPath))
            {
                LoadModsFromDirectory(inactiveFolderPath, _mods, isActive: false);
            }

            // Sort the mods list by Active state and then by Name
            SortMods();

            // Bind the sorted mods list to the DataGrid
            ModTable.ItemsSource = _mods;
        }

        /// <summary>
        /// Loads mods from a specified directory and adds them to the provided collection.
        /// </summary>
        /// <param name="directoryPath">The path of the directory to load mods from.</param>
        /// <param name="mods">The collection to add the loaded mods to.</param>
        /// <param name="isActive">Indicates whether the mods are active or not.</param>
        private void LoadModsFromDirectory(string directoryPath, ObservableCollection<Mod> mods, bool isActive = true)
        {
            if (Directory.Exists(directoryPath))
            {
                foreach (var modDir in Directory.GetDirectories(directoryPath))
                {
                    var modInfoPath = Path.Combine(modDir, "ModInfo.xml");
                    if (File.Exists(modInfoPath))
                    {
                        var mod = LoadModInfo(modInfoPath, modDir, isActive);
                        mods.Add(mod);
                    }
                }
            }
        }

        /// <summary>
        /// Loads mod information from the given XML file and directory path.
        /// </summary>
        /// <param name="modInfoPath">Path to the XML file containing mod information.</param>
        /// <param name="modDir">Path to the directory containing the mod.</param>
        /// <param name="isActive">Indicates whether the mod is active or not.</param>
        /// <returns>A <see cref="Mod"/> object populated with information from the XML file.</returns>
        private Mod LoadModInfo(string modInfoPath, string modDir, bool isActive)
        {
            var doc = new System.Xml.XmlDocument();
            doc.Load(modInfoPath);

            // Try to get DisplayName first
            var displayNameNode = doc.SelectSingleNode("//DisplayName");
            string displayName = displayNameNode?.Attributes["value"]?.Value;

            // If DisplayName is not available, try to get Name
            if (string.IsNullOrEmpty(displayName))
            {
                var nameNode = doc.SelectSingleNode("//Name");
                displayName = nameNode?.Attributes["value"]?.Value ?? "Unknown";
            }

            var version = doc.SelectSingleNode("//Version")?.Attributes["value"]?.Value ?? "Unknown";
            var description = doc.SelectSingleNode("//Description")?.Attributes["value"]?.Value ?? "No description available.";
            var fileInfo = new FileInfo(modInfoPath);
            var lastUpdated = fileInfo.LastWriteTime;

            return new Mod
            {
                Name = displayName,
                Version = version,
                LastUpdated = lastUpdated,
                Active = isActive,
                DirectoryPath = modDir,
                Description = description
            };
        }

        /// <summary>
        /// Constructs the path to the Inactive folder where disabled mods are stored.
        /// </summary>
        /// <returns>The path to the Inactive folder.</returns>
        private string GetInactiveFolderPath()
        {
            return Path.Combine(ModsFolderPath, InactiveFolderName);
        }

        /// <summary>
        /// Event handler for the Update button click event.
        /// Currently a placeholder for update logic.
        /// </summary>
        private void OnUpdateClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Update button clicked.");
        }

        /// <summary>
        /// Event handler for the Delete button click event.
        /// Prompts for confirmation and deletes the selected mod.
        /// </summary>
        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var mod = button.DataContext as Mod;
                if (mod != null)
                {
                    // Check if the mod is "Harmony Wrapper"
                    if (mod.Name.Equals("Harmony Wrapper", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("The 'Harmony Wrapper' mod is essential and cannot be deleted.", "Deletion Not Allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Prompt the user to confirm deletion
                    var result = MessageBox.Show($"Are you sure you want to delete the mod \"{mod.Name}\"?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            // Delete the mod directory and all its contents
                            Directory.Delete(mod.DirectoryPath, true);

                            MessageBox.Show("Mod deleted successfully.");

                            // Refresh the mod list
                            LoadMods();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred while deleting the mod: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for handling drag-and-drop operations.
        /// Extracts and installs a mod from a dropped archive file.
        /// </summary>
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || files.Length == 0) return;

                var filePath = files[0];

                // Check if the file is a zip or 7z archive
                if (!IsValidArchive(filePath))
                {
                    MessageBox.Show("Please drop a valid zip or 7z file.");
                    return;
                }

                // Validate and extract the archive
                try
                {
                    if (ExtractAndValidateArchive(filePath))
                    {
                        MessageBox.Show("Mod installed successfully.");
                        LoadMods(); // Refresh the mod list
                    }
                    else
                    {
                        MessageBox.Show("Invalid mod structure. The archive must contain a directory with a ModInfo.xml file.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Checks if the provided file is a valid archive format.
        /// </summary>
        /// <param name="filePath">Path to the file being checked.</param>
        /// <returns><c>true</c> if the file is a valid archive format; otherwise, <c>false</c>.</returns>
        private bool IsValidArchive(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".zip" || extension == ".7z";
        }

        /// <summary>
        /// Extracts the archive to a temporary directory and validates its structure.
        /// </summary>
        /// <param name="filePath">Path to the archive file.</param>
        /// <returns><c>true</c> if extraction and validation succeed; otherwise, <c>false</c>.</returns>
        private bool ExtractAndValidateArchive(string filePath)
        {
            // Temporary extraction path
            string tempExtractPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(filePath));

            // Clean up any existing temp directory
            if (Directory.Exists(tempExtractPath))
            {
                Directory.Delete(tempExtractPath, true);
            }

            try
            {
                // Extract the archive to the temporary directory
                if (Path.GetExtension(filePath).ToLower() == ".zip")
                {
                    ZipFile.ExtractToDirectory(filePath, tempExtractPath);
                }
                else if (Path.GetExtension(filePath).ToLower() == ".7z")
                {
                    using (var archive = new ArchiveFile(filePath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            var destinationPath = Path.Combine(tempExtractPath, entry.FileName);
                            if (entry.IsFolder)
                            {
                                Directory.CreateDirectory(destinationPath);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                                entry.Extract(destinationPath);
                            }
                        }
                    }
                }

                // Get the extracted directory
                var extractedDirectories = Directory.GetDirectories(tempExtractPath);

                // Validate that there is exactly one directory
                if (extractedDirectories.Length == 1)
                {
                    string modFolderName = Path.GetFileName(extractedDirectories[0]);
                    string modFolderPath = Path.Combine(ModsFolderPath, modFolderName);

                    // Ensure the mod directory does not already exist
                    if (Directory.Exists(modFolderPath))
                    {
                        Directory.Delete(modFolderPath, true); // Overwrite if already exists
                    }

                    // Validate that the directory contains ModInfo.xml
                    string modInfoPath = Path.Combine(extractedDirectories[0], "ModInfo.xml");
                    if (File.Exists(modInfoPath))
                    {
                        // Move the directory to the Mods folder
                        Directory.Move(extractedDirectories[0], modFolderPath);
                        Directory.Delete(tempExtractPath, true); // Clean up the temp directory
                        return true;
                    }
                }

                // Clean up if validation fails
                Directory.Delete(tempExtractPath, true);
                return false;
            }
            catch
            {
                Directory.Delete(tempExtractPath, true); // Clean up if extraction fails
                throw;
            }
        }

        /// <summary>
        /// Event handler for when a mod's Active state is changed (checked).
        /// Moves the mod to the active directory and updates its state.
        /// </summary>
        private void OnActiveChecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var mod = checkBox.DataContext as Mod;
                if (mod != null && !mod.Active)
                {
                    if (mod.Name.Equals("Harmony Wrapper", StringComparison.OrdinalIgnoreCase))
                    {
                        // Prevent changing the state of the Harmony Wrapper mod
                        checkBox.IsChecked = false;
                        MessageBox.Show("The 'Harmony Wrapper' mod cannot be deactivated.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    MoveMod(mod, GetInactiveFolderPath(), ModsFolderPath);
                    mod.Active = true;
                    // Sort the collection again after updating
                    SortMods();
                }
            }
        }

        /// <summary>
        /// Event handler for when a mod's Active state is changed (unchecked).
        /// Moves the mod to the inactive directory and updates its state.
        /// </summary>
        private void OnActiveUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var mod = checkBox.DataContext as Mod;
                if (mod != null && mod.Active)
                {
                    if (mod.Name.Equals("Harmony Wrapper", StringComparison.OrdinalIgnoreCase))
                    {
                        // Prevent changing the state of the Harmony Wrapper mod
                        checkBox.IsChecked = true;
                        MessageBox.Show("The 'Harmony Wrapper' mod cannot be deactivated.", "Action Not Allowed", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    EnsureInactiveFolderExists();
                    MoveMod(mod, ModsFolderPath, GetInactiveFolderPath());
                    mod.Active = false;
                    // Sort the collection again after updating
                    SortMods();
                }
            }
        }

        /// <summary>
        /// Sorts the mod collection first by Active state and then by Name.
        /// </summary>
        private void SortMods()
        {
            var sortedMods = _mods.OrderBy(m => !m.Active).ThenBy(m => m.Name).ToList();
            _mods.Clear();
            foreach (var mod in sortedMods)
            {
                _mods.Add(mod);
            }
        }

        /// <summary>
        /// Ensures that the Inactive folder exists, creating it if necessary.
        /// </summary>
        private void EnsureInactiveFolderExists()
        {
            var inactiveFolderPath = GetInactiveFolderPath();
            if (!Directory.Exists(inactiveFolderPath))
            {
                Directory.CreateDirectory(inactiveFolderPath);
            }
        }

        /// <summary>
        /// Moves a mod directory from one location to another.
        /// </summary>
        /// <param name="mod">The mod to be moved.</param>
        /// <param name="fromDir">Source directory path.</param>
        /// <param name="toDir">Destination directory path.</param>
        private void MoveMod(Mod mod, string fromDir, string toDir)
        {
            var fromPath = mod.DirectoryPath;
            var toPath = Path.Combine(toDir, Path.GetFileName(fromPath));

            if (Directory.Exists(fromPath))
            {
                if (Directory.Exists(toPath))
                {
                    Directory.Delete(toPath, true); // Delete existing directory if it exists
                }
                Directory.Move(fromPath, toPath);
                mod.DirectoryPath = toPath;
            }
        }

        /// <summary>
        /// Event handler for launching the game.
        /// </summary>
        private void OnLaunchGameClick(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = GameExecutablePath,
                    Arguments = "-disableeac -fullscreen",
                    WorkingDirectory = Path.GetDirectoryName(GameExecutablePath),
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch the game: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for opening the Mods folder in File Explorer.
        /// </summary>
        private void OnOpenModsFolderClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = ModsFolderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open the Mods folder: {ex.Message}");
            }
        }
    }
}
