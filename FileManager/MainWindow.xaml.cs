using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static FileManager.MainWindow;
using Path = System.IO.Path;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.ComponentModel;
using Microsoft.VisualBasic;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.ComponentModel.Design;
using System.Collections;
using System.Globalization;
using System.Security.Policy;







namespace FileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<FileItem> allFiles;
        private int clickCount = 0;
        private DateTime lastClickTime;
        private FileItem lastClickedItem;
        private string selectedItemPath;
        private bool isDirectory;
        private Point startPoint;
        private GridViewColumnHeader lastHeaderClicked = null;
        private ListSortDirection lastDirection = ListSortDirection.Ascending;
        public string FileType { get; set; } // Ensure this property exists

        public string DisplayName
        {
            get
            {
                if (FileType == "Text Document" && Name.EndsWith(".txt"))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(Name);
                }
                return Name;
            }
        }

        private List<string> navigationHistory = new List<string>();
        private int currentHistoryIndex = -1;
        private bool isNavigating = false;



        // Store selected items for copy/cut operations
        private List<FileItem> selectedItems = new List<FileItem>();
        private List<String> selectedItemsPaths = new List<String>();
        private bool isCutOperation = false;
        // Declare and initialize CurrentDirectory variable
        private string CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public MainWindow()
        {
            InitializeComponent();
            LoadDirectoryTree();
            // Enable drag and drop functionality for ListView
            FileListView.AllowDrop = true;
            FileListView.Drop += FileListView_Drop;

            // Enable drag and drop functionality for TreeView
            DirectoryTree.AllowDrop = true;
            DirectoryTree.Drop += DirectoryTree_Drop;
            FileListView.DragEnter += FileListView_DragEnter;
            FileListView.DragOver += FileListView_DragOver;
            DirectoryTree.DragEnter += DirectoryTree_DragEnter;
            DirectoryTree.DragOver += DirectoryTree_DragOver;
            //FileListView.MouseMove += FileListView_MouseMove;
            FileListView.MouseDoubleClick += FileListView_MouseDoubleClick; // Add this line
            DirectoryTree.SelectedItemChanged += DirectoryTree_SelectedItemChanged;
            FileListView.PreviewMouseLeftButtonDown += FileListView_PreviewMouseLeftButtonDown;

        }


        //private void LoadDirectoryTree()
        //{
        //    //foreach (var drive in Directory.GetLogicalDrives())
        //    //{
        //    //    var item = new TreeViewItem
        //    //    {
        //    //        Header = drive,
        //    //        Tag = drive
        //    //    };
        //    //    item.Items.Add(null);
        //    //    item.Expanded += Folder_Expanded;
        //    //    DirectoryTree.Items.Add(item);
        //    //}


        private void LoadDirectoryTree()
        {
            var rootPath = "D:\\Folder Structure Creator";
            if (Directory.Exists(rootPath))
            {
                var rootItem = CreateFileItem(rootPath, true);
                DirectoryTree.Items.Add(rootItem);
            }

        }


        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                var fileItem = (FileItem)item.DataContext;

                if (fileItem.SubItems.Count == 1 && fileItem.SubItems[0] == null)
                {
                    fileItem.SubItems.Clear();

                    try
                    {
                        foreach (var directory in Directory.GetDirectories(fileItem.Path))
                        {
                            var subItem = CreateFileItem(directory, true);
                            fileItem.SubItems.Add(subItem);
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }



        private FileItem CreateFileItem(string path, bool isDirectory)
        {
            var fileItem = new FileItem
            {
                Name = System.IO.Path.GetFileName(path),
                Path = path,
                Icon = isDirectory ? new BitmapImage(new Uri("C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\folder (2).png")) : GetIconForFile(System.IO.Path.GetExtension(path))
            };

            if (isDirectory)
            {
                fileItem.SubItems.Add(null); // Placeholder for lazy loading
            }

            return fileItem;
        }

        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DirectoryTree.SelectedItem is FileItem fileItem)
            {
                if (fileItem.Path != PathBox.Text) // Prevent recursive updates
                {
                    PathBox.Text = fileItem.Path; // Update the path box
                    LoadFiles(fileItem.Path);
                }
            }

        }


        private string GetReadableFileSize(long size)    //srikanth konnagula
        {
            if (size < 1024)
                return $"{size} B";
            int unit = 1024;
            if (size < unit * unit)
                return $"{size / unit} KB";
            if (size < unit * unit * unit)
                return $"{size / (unit * unit):F2} MB";
            return $"{size / (unit * unit * unit):F2} GB";
        }

        private void LoadFiles(string path)       //srikanth konnagula
        {
            allFiles = new ObservableCollection<FileItem>();
            try
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    allFiles.Add(new FileItem
                    {
                        Name = System.IO.Path.GetFileName(directory),
                        Path = directory,
                        Size = "", // Directories don't have a size
                        Type = "File folder",
                        DateModified = Directory.GetLastWriteTime(directory).ToString(),
                        Icon = new BitmapImage(new Uri("C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\folder (2).png"))
                    });
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    allFiles.Add(new FileItem
                    {
                        Name = fileInfo.Name,
                        Path = file,
                        Size = GetReadableFileSize(fileInfo.Length),
                        Type = fileInfo.Extension,
                        DateModified = fileInfo.LastWriteTime.ToString(),
                        Icon = GetIconForFile(fileInfo.Extension)
                    });
                }

                if (!isNavigating)
                {
                    if (currentHistoryIndex < navigationHistory.Count - 1)
                    {
                        navigationHistory.RemoveRange(currentHistoryIndex + 1, navigationHistory.Count - currentHistoryIndex - 1);
                    }
                    navigationHistory.Add(path);
                    currentHistoryIndex++;
                }
                isNavigating = false;
            }
            catch (UnauthorizedAccessException) { }
            FileListView.ItemsSource = allFiles;
            CurrentDirectory = path;
            PathBox.Text = path; // Update the path box
        }



        private BitmapImage GetIconForFile(string extension)   //srikanth konnagula
        {
            switch (extension.ToLower())
            {
                case ".zip":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/zip.png"));
                case ".txt":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/txts.png"));
                case ".pdf":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/pdf.png"));
                case ".png":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/png.png"));
                case ".jpg":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/jpg.png"));

                default:
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/folder (1).png"));
            }
        }



        public class FileItem : INotifyPropertyChanged    //srikanth konnagula
        {
            private string name;
            private string path;
            private string size;
            private string type;
            private string dateModified;
            private BitmapImage icon;
            public bool IsSpecialMessage { get; set; }
            private ObservableCollection<FileItem> subItems = new ObservableCollection<FileItem>();
            public string Name
            {
                get => name;
                set
                {
                    if (name != value)
                    {
                        name = value;
                        OnPropertyChanged(nameof(Name));
                    }
                }
            }

            public string Path
            {
                get => path;
                set
                {
                    if (path != value)
                    {
                        path = value;
                        OnPropertyChanged(nameof(Path));
                    }
                }
            }

            public string Size
            {
                get => size;
                set
                {
                    if (size != value)
                    {
                        size = value;
                        OnPropertyChanged(nameof(Size));
                    }
                }
            }

            public string Type
            {
                get => type;
                set
                {
                    if (type != value)
                    {
                        type = value;
                        OnPropertyChanged(nameof(Type));
                    }
                }
            }
            public string DateModified
            {
                get => dateModified;
                set
                {
                    if (dateModified != value)
                    {
                        dateModified = value;
                        OnPropertyChanged(nameof(DateModified));
                    }
                }
            }
            public BitmapImage Icon
            {
                get => icon;
                set
                {
                    if (icon != value)
                    {
                        icon = value;
                        OnPropertyChanged(nameof(Icon));
                    }
                }
            }


            public ObservableCollection<FileItem> SubItems
            {
                get => subItems;
                set
                {
                    if (subItems != value)
                    {
                        subItems = value;
                        OnPropertyChanged(nameof(SubItems));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private void SearchBox_KeyUp(object sender, KeyEventArgs e)  //bhanu
        {
            if (e.Key == Key.Enter)
            {
                var searchQuery = SearchBox.Text.ToLower();
                var currentPath = PathBox.Text;
                var files = new ObservableCollection<FileItem>();
                try
                {
                    foreach (var file in Directory.GetFiles(currentPath))
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Name.ToLower().Contains(searchQuery))
                        {
                            files.Add(new FileItem
                            {
                                Name = fileInfo.Name,
                                Size = fileInfo.Length.ToString(),
                                Type = fileInfo.Extension,
                                DateModified = fileInfo.LastWriteTime.ToString()
                            });
                        }
                    }
                }
                catch (UnauthorizedAccessException) { }
                FileListView.ItemsSource = files;
            }
        }


        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject  //srikanth konnagula
        {
            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }





        private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }





        private void CreateFolderStructure_Click(object sender, RoutedEventArgs e) //Srikanth konnagula
        {



            string rootPath = PathBox.Text;
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                MessageBox.Show("Invalid path. Please select a valid directory.");
                return;
            }

            // Automatically generate a new folder name with incrementing numbers
            string baseFolderName = "New folder";
            string newFolderName = baseFolderName;
            int folderNumber = 1;

            // Scan existing folders to find the highest incremented number
            foreach (var directory in Directory.GetDirectories(rootPath))
            {
                string folderName = Path.GetFileName(directory);
                if (folderName.StartsWith(baseFolderName))
                {
                    if (folderName.Equals(baseFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        folderNumber = Math.Max(folderNumber, 1);
                    }
                    else if (folderName.StartsWith($"{baseFolderName} (") && folderName.EndsWith(")"))
                    {
                        string numberPart = folderName.Substring(baseFolderName.Length + 1, folderName.Length - baseFolderName.Length - 3);
                        if (int.TryParse(numberPart, out int number))
                        {
                            folderNumber = Math.Max(folderNumber, number + 1);
                        }
                    }
                }
            }

            // Set the final folder name with the correct number
            if (folderNumber > 1)
            {
                newFolderName = $"{baseFolderName} ({folderNumber})";
            }

            string newFolderPath = Path.Combine(rootPath, newFolderName);

            try
            {
                Directory.CreateDirectory(newFolderPath);

                // Create a new FileItem for the new folder
                var newFolderItem = new FileItem
                {
                    Name = newFolderName,
                    Path = newFolderPath,
                    Size = "",
                    Type = "Folder",
                    DateModified = DateTime.Now.ToString(),
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/folder (2).png"))
                };

                // Add the new item to the ObservableCollection
                allFiles.Add(newFolderItem);

                // Set focus to the new item and make the name editable
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var newItem = FileListView.ItemContainerGenerator.ContainerFromItem(newFolderItem) as ListViewItem;
                    if (newItem != null)
                    {
                        var textBlock = FindVisualChild<TextBlock>(newItem);
                        if (textBlock != null)
                        {
                            textBlock.Visibility = Visibility.Collapsed;
                        }
                        newItem.Focus();
                        var textBox = FindVisualChild<TextBox>(newItem);
                        if (textBox != null)
                        {
                            textBox.Text = newFolderName; // Set the text box to the name without the extension
                            textBox.Visibility = Visibility.Visible;
                            textBox.Focus();
                            textBox.SelectAll();
                            textBox.LostFocus += (s, ev) =>
                            {
                                FinalizeNewFolder(rootPath, textBox.Text, newFolderPath);
                            };
                        }
                    }
                }), DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating folder: {ex.Message}");
            }

        }

        private void FinalizeNewFolder(string directory, string newName, string oldPath)
        {


            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Folder name cannot be empty.");
                return;
            }

            string newFolderPath = Path.Combine(directory, newName);

            // Check if the folder already exists and find a unique name if it does
            string baseName = newName;
            int folderNumber = 1; // Start from 1 to ensure the first folder is named "New folder"

            // Extract base name and number if the name contains an existing number
            if (baseName.EndsWith(")") && baseName.Contains("("))
            {
                int lastOpenParen = baseName.LastIndexOf("(");
                string numberPart = baseName.Substring(lastOpenParen + 1, baseName.Length - lastOpenParen - 2);
                if (int.TryParse(numberPart, out int existingNumber))
                {
                    baseName = baseName.Substring(0, lastOpenParen).TrimEnd();
                    folderNumber = existingNumber + 1;
                }
            }

            while (Directory.Exists(newFolderPath))
            {
                // If the folder already exists, append the folder number
                newFolderPath = Path.Combine(directory, $"{baseName} ({folderNumber++})");
            }

            try
            {
                Directory.Move(oldPath, newFolderPath);
                var folderItem = allFiles.FirstOrDefault(f => f.Path == oldPath);
                if (folderItem != null)
                {
                    folderItem.Name = Path.GetFileName(newFolderPath);
                    folderItem.Path = newFolderPath;
                    folderItem.DateModified = DateTime.Now.ToString();
                }
                RefreshView(directory);

                // Ask the user if they want to create SDLC subfolders
                MessageBoxResult result = MessageBox.Show("Do you want to include SDLC subfolders?", "Create Subfolders", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    CreateSDLCSubfolders(newFolderPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming folder: {ex.Message}");
            }


        }

        private void CreateSDLCSubfolders(string rootPath)
        {
            var subfolders = new Dictionary<string, string[]>
    {
        { "1.Requirements", new string[] { "1.Documents", "2.Specifications" } },
        { "2.Design", new string[] { "1.Diagrams", "2.Prototypes" } },
        { "3.Implementation", new string[] { "1.SourceCode", "2.Binaries" } },
        { "4.Testing", new string[] { "1.TestCases", "2.Reports" } },
        { "5.Deployment", new string[] { "1.Scripts", "2.Documentation" } }
    };

            try
            {
                foreach (var subfolder in subfolders)
                {
                    string subfolderPath = System.IO.Path.Combine(rootPath, subfolder.Key);
                    if (!Directory.Exists(subfolderPath))
                    {
                        Directory.CreateDirectory(subfolderPath);
                    }

                    foreach (var nestedSubfolder in subfolder.Value)
                    {
                        string nestedSubfolderPath = System.IO.Path.Combine(subfolderPath, nestedSubfolder);
                        if (!Directory.Exists(nestedSubfolderPath))
                        {
                            Directory.CreateDirectory(nestedSubfolderPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating SDLC subfolders: {ex.Message}");
            }
        }


        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedFiles.Clear();
            foreach (FileItem item in FileListView.SelectedItems)
            {
                selectedFiles.Add(item);
            }
        }

        // Event handler for completing the drop operation (unchanged)
        private async void FileListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FileItem)))
            {
                FileItem droppedData = e.Data.GetData(typeof(FileItem)) as FileItem;
                string destinationPath = PathBox.Text;

                if (droppedData != null && !string.IsNullOrWhiteSpace(destinationPath))
                {
                    try
                    {
                        string destinationFullPath = Path.Combine(destinationPath, droppedData.Name);

                        if (Directory.Exists(destinationFullPath) || File.Exists(destinationFullPath))
                        {
                            MessageBox.Show("An item with the same name already exists at the destination.");
                            return;
                        }

                        await Task.Run(() =>
                        {
                            if (Directory.Exists(droppedData.Path))
                            {
                                Directory.Move(droppedData.Path, destinationFullPath);
                            }
                            else if (File.Exists(droppedData.Path))
                            {
                                File.Move(droppedData.Path, destinationFullPath);
                            }
                        });

                        MessageBox.Show($"Item moved to {destinationPath}");

                        LoadFiles(destinationPath);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        MessageBox.Show($"Access denied: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        MessageBox.Show($"File I/O error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error moving item: {ex.Message}");
                    }
                }
            }
        }


        private void DirectoryTree_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && DirectoryTree.SelectedItem is FileItem targetItem)
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string targetPath = targetItem.Path;

                foreach (string file in files)
                {
                    try
                    {
                        string destinationPath = System.IO.Path.Combine(targetPath, System.IO.Path.GetFileName(file));
                        if (File.Exists(file))
                        {
                            File.Copy(file, destinationPath);
                        }
                        else if (Directory.Exists(file))
                        {
                            CopyDirectory(file, destinationPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error copying file: {ex.Message}");
                    }
                }
                LoadFiles(targetPath);
            }
        }
        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)          //srikanth konnagula
        {
            if (FileListView.SelectedItem is FileItem selectedItem && Directory.Exists(selectedItem.Path))
            {
                // If it's a directory, navigate into it
                LoadFiles(selectedItem.Path);
            }
        }

        private void OpenFile(string filePath)         //srikanth konnagula
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                Process process = new Process();
                process.EnableRaisingEvents = true;
                process.Exited += (sender, e) => Dispatcher.Invoke(() => RefreshFileItem(filePath));

                switch (extension)
                {
                    case ".pdf":
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true // Opens with the default associated application
                        };
                        break;

                    case ".txt":
                        // First, set the file attribute to read-only
                        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);

                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "notepad.exe",
                            Arguments = filePath,
                            UseShellExecute = true
                        };

                        // Attach event handler to remove read-only attribute after closing Notepad
                        process.Exited += (sender, e) =>
                        {
                            // Remove the read-only attribute from the file
                            Dispatcher.Invoke(() =>
                            {
                                FileAttributes attributes = File.GetAttributes(filePath);
                                attributes &= ~FileAttributes.ReadOnly;
                                File.SetAttributes(filePath, attributes);
                            });
                        };
                        break;

                    default:
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true // Opens with the default associated application
                        };
                        break;
                }

                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening file: " + ex.Message);
            }
        }



        private void RefreshFileItem(string filePath)          //srikanth konnagula
        {
            var fileItem = allFiles.FirstOrDefault(f => f.Path == filePath);
            if (fileItem != null)
            {
                var fileInfo = new FileInfo(filePath);
                fileItem.Size = GetReadableFileSize(fileInfo.Length);
                fileItem.DateModified = fileInfo.LastWriteTime.ToString();

                // Refresh the ListView to reflect the changes
                FileListView.Items.Refresh();
            }
        }

        private bool isRenameInitiated = false;
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)        //srikanth konnagula
        {
            var textBlock = (TextBlock)sender;
            var listViewItem = FindAncestor<ListViewItem>(textBlock);

            if (listViewItem != null)
            {
                var fileItem = (FileItem)listViewItem.DataContext;

                if (e.ClickCount == 2)
                {
                    // Double-click to open the file
                    if (File.Exists(fileItem.Path))
                    {
                        OpenFile(fileItem.Path);
                    }
                }
                else if (e.ClickCount == 1)
                {
                    // Single-click to enable text editing if the item is a folder
                    if (Directory.Exists(fileItem.Path))
                    {
                        var textBox = FindVisualChild<TextBox>(listViewItem);
                        if (textBox != null)
                        {
                            textBox.Visibility = Visibility.Visible;
                            textBox.Focus();
                            textBox.SelectAll();
                        }
                        textBlock.Visibility = Visibility.Collapsed;

                        // Set the flag indicating rename was initiated by single click
                        isRenameInitiated = true;
                    }
                }
            }
        }


        private void FileListView_KeyDown(object sender, KeyEventArgs e)        //srikanth konnagula
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelectedItems();
            }
        }

        private List<FileItem> selectedFiles = new List<FileItem>();

        private void DeleteSelectedItems()           //srikanth konnagula
        {
            if (selectedFiles.Count > 0)
            {
                var result = MessageBox.Show("Are you sure you want to delete the selected items?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    DeleteItemsWithRetry();
                }
            }
            else
            {
                MessageBox.Show("No items selected for deletion.");
            }
        }
        private void DeleteItemsWithRetry()           //srikanth konnagula
        {
            try
            {
                string archivePath = "D:\\Folder Structure Creator\\Archive"; // Define your archive location here
                if (!Directory.Exists(archivePath))
                {
                    Directory.CreateDirectory(archivePath);
                }

                foreach (var item in selectedFiles)
                {
                    if (IsArchiveFolder(item.Path, archivePath))
                    {
                        MessageBox.Show("Cannot delete the Archive folder.");
                        continue;
                    }

                    if (Directory.Exists(item.Path))
                    {
                        DeleteFolderWithRetry(item.Path, archivePath);
                    }
                    else if (File.Exists(item.Path))
                    {
                        DeleteFile(item.Path, archivePath);
                    }
                }

                // Refresh the file list view after processing all items
                LoadFiles(CurrentDirectory);
                //MessageBox.Show("Selected items deleted and archived successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting items: {ex.Message}");
            }
        }

        private void DeleteFolderWithRetry(string folderPath, string archivePath)
        {
            bool retry;
            do
            {
                retry = false;
                if (!IsFolderInUse(folderPath))
                {
                    // Zip and archive the folder
                    string zipFileName = $"{Path.GetFileName(folderPath)}.zip";
                    string zipFilePath = Path.Combine(archivePath, zipFileName);
                    ZipFolder(folderPath, zipFilePath);

                    // Delete the original folder
                    Directory.Delete(folderPath, true);
                }
                else
                {
                    var fileInfo = new DirectoryInfo(folderPath);
                    var dialog = new FolderInUseDialog(fileInfo.Name, fileInfo.CreationTime);
                    dialog.Owner = Application.Current.MainWindow;
                    dialog.ShowDialog();
                    retry = dialog.TryAgain;
                }
            } while (retry);
        }

        private void DeleteFile(string filePath, string archivePath)
        {
            bool retry;
            do
            {
                retry = false;
                if (!IsFileInUse(filePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    string destinationPath = Path.Combine(archivePath, fileName);
                    if (File.Exists(destinationPath))
                    {
                        //MessageBox.Show($"A file with the name {fileName} already exists in the archive.");
                        return;
                    }
                    File.Move(filePath, destinationPath);
                }
                else
                {
                    var fileInfo = new FileInfo(filePath);
                    var dialog = new FolderInUseDialog(fileInfo.Name, fileInfo.CreationTime);
                    dialog.Owner = Application.Current.MainWindow;
                    dialog.ShowDialog();
                    retry = dialog.TryAgain;
                }
            } while (retry);
        }



        private bool IsArchiveFolder(string itemPath, string archivePath)
        {
            return itemPath.StartsWith(archivePath, StringComparison.OrdinalIgnoreCase);
        }


        private bool IsFileInUse(string filePath)        //srikanth konnagula
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    // Attempt to acquire an exclusive lock on the file
                    if (stream != null && stream.CanRead)
                    {
                        return false; // File is not in use
                    }
                    else
                    {
                        return true; // File is in use
                    }
                }
            }
            catch (IOException)
            {
                return true; // File is in use
            }
        }

        private bool IsFolderInUse(string folderPath)   //srikanth konnagula
        {
            try
            {
                var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (IsFileInUse(file))
                    {
                        return true; // Return true as soon as any file is found to be in use
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return true; // Consider folder in use if an exception occurs
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete the selected items?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                DeleteItemsWithRetry();
            }
        }

        private void ZipFolder(string sourceFolder, string zipFilePath)   //srikanth konnagula
        {
            try
            {
                ZipFile.CreateFromDirectory(sourceFolder, zipFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error zipping folder: {ex.Message}");
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)     //srikanth konnagula
        {
            // Refresh the directory tree
            DirectoryTree.Items.Clear();
            LoadDirectoryTree();

            // Refresh the file list view
            if (!string.IsNullOrEmpty(CurrentDirectory))
            {
                LoadFiles(CurrentDirectory);
            }
        }

        private void FileListView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem item)
            {
                var fileItem = (FileItem)item.DataContext;
                if (fileItem != null)
                {
                    DragDrop.DoDragDrop(FileListView, new DataObject(DataFormats.FileDrop, new string[] { fileItem.Path }), DragDropEffects.Copy);
                }
            }
        }
        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the View button
            Button viewButton = sender as Button;
            if (viewButton != null)
            {
                // Find the ViewContextMenu resource
                ContextMenu viewMenu = this.FindResource("ViewContextMenu") as ContextMenu;
                if (viewMenu != null)
                {
                    // Open the context menu at the position of the View button
                    viewMenu.PlacementTarget = viewButton;
                    viewMenu.IsOpen = true;

                }
            }
        }


        private void FileListView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            if (!e.Data.GetDataPresent(typeof(File)) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void FileListView_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            if (!e.Data.GetDataPresent(typeof(File)) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void DirectoryTree_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void DirectoryTree_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

      
        private string tempFolderPath = Path.Combine(Path.GetTempPath(), "CutPasteTemp");       //tejaswini
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                CopyWithoutDirectory(sender, e);
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.V)
            {
                PasteWithoutDirectory(sender, e);
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.X)
            {
                CutWithoutDirectory(sender, e);
            }
          
        }
     
        private void CopyWithoutDirectory(object sender, RoutedEventArgs e)    //tejaswini
        {
            if (FileListView.SelectedItems.Count > 0)
            {
                // Clear previous selected items
                selectedItemsPaths.Clear();

                // Store the paths of selected items to be copied
                foreach (var item in FileListView.SelectedItems)
                {
                    if (item is FileItem selectedFileItem)
                    {
                        selectedItemsPaths.Add(selectedFileItem.Path);
                    }
                }

                isCutOperation = false;
            }
        }

        private void CutWithoutDirectory(object sender, RoutedEventArgs e)    //tejaswini
        {
            if (FileListView.SelectedItems.Count > 0)
            {
                // Clear previous selected items
                selectedItemsPaths.Clear();

                // Store the paths of selected items to be cut
                foreach (var item in FileListView.SelectedItems)
                {
                    if (item is FileItem selectedFileItem)
                    {
                        selectedItemsPaths.Add(selectedFileItem.Path);
                    }
                }

                // Ensure temporary folder exists
                Directory.CreateDirectory(tempFolderPath);

                // Copy selected files to the temporary folder
                foreach (var itemPath in selectedItemsPaths)
                {
                    string tempPath = Path.Combine(tempFolderPath, Path.GetFileName(itemPath));
                    if (File.Exists(itemPath))
                    {
                        File.Copy(itemPath, tempPath, true);
                    }
                    else if (Directory.Exists(itemPath))
                    {
                        CopyDirectory(itemPath, tempPath);
                    }
                }

                isCutOperation = true;
            }
        }

        private void PasteWithoutDirectory(object sender, RoutedEventArgs e)   
        {
            List<string> successfulMoves = new List<string>();

            foreach (var itemPath in selectedItemsPaths)
            {
                // Determine the destination path for the item
                string fileName = Path.GetFileName(itemPath);
                string destinationPath = Path.Combine(PathBox.Text, fileName);

                // If the destination file already exists, rename it with an incrementing suffix
                string baseName = Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                string newName = baseName;

                int copyNumber = 1;

                // Check if the base name already exists
                while (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                {
                    if (copyNumber == 1)
                    {
                        // For the first copy, add the suffix directly without any numbering
                        newName = $"{baseName}-Copy{extension}";
                    }
                    else
                    {
                        // For subsequent copies, append the copy number within parentheses
                        newName = $"{baseName}-Copy({copyNumber}){extension}";
                    }

                    destinationPath = Path.Combine(PathBox.Text, newName);
                    copyNumber++;
                }

                try
                {
                    if (File.Exists(itemPath))
                    {
                        File.Copy(itemPath, destinationPath);
                        if (isCutOperation)
                        {
                            successfulMoves.Add(itemPath);
                            File.Delete(itemPath);
                        }
                    }
                    else if (Directory.Exists(itemPath))
                    {
                        CopyDirectory(itemPath, destinationPath);
                        if (isCutOperation)
                        {
                            successfulMoves.Add(itemPath);
                            Directory.Delete(itemPath, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error pasting {itemPath}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (isCutOperation)
            {
                // Delete original files only if move was successful
                foreach (var itemPath in successfulMoves)
                {
                    try
                    {
                        if (File.Exists(itemPath))
                        {
                            File.Delete(itemPath);
                        }
                        else if (Directory.Exists(itemPath))
                        {
                            Directory.Delete(itemPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting original {itemPath}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }



            // Refresh the file list view
            LoadFiles(PathBox.Text); // Refresh to show changes in the destination directory
        }


        private void CopyButton_Click(object sender, RoutedEventArgs e)        //tejaswini
        {
            if (FileListView.SelectedItems.Count > 0)
            {
                selectedItemsPaths.Clear();
                foreach (var item in FileListView.SelectedItems)
                {
                    if (item is FileItem selectedFileItem)
                    {
                        string selectedItemPath = Path.Combine(PathBox.Text, selectedFileItem.Name);
                        selectedItemsPaths.Add(selectedItemPath);
                    }
                }

                // No message box here
            }
            else
            {
                // No message box here
            }
        }
        private void MoveItem_Click(object sender, RoutedEventArgs e)          //bhanu
        {
            // Check if any items are selected
            if (FileListView.SelectedItems.Count > 0)
            {
                // Use FolderBrowserDialog to select the destination folder
                using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "Select the destination location";
                    folderBrowserDialog.ShowNewFolderButton = true;

                    // Set the root folder for the dialog
                    folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer; // Set to the root of "My Computer"
                    folderBrowserDialog.SelectedPath = @"D:\Folder Structure Creator";
                    // Show the dialog and check if the user selected a folder
                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string destinationPath = folderBrowserDialog.SelectedPath;

                        // Check if the destination path contains "Archive"
                        if (destinationPath.IndexOf("Archive", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            MessageBox.Show("Items cannot be moved to an Archive folder. Please select a different location.",
                                            "Invalid Destination",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Error);
                            return;
                        }

                        try
                        {
                            bool itemsMoved = false; // Flag to track if any items are moved

                            foreach (var item in FileListView.SelectedItems)
                            {
                                if (item is FileItem selectedItem)
                                {
                                    string destinationFullPath = Path.Combine(destinationPath, selectedItem.Name);

                                    // Check if the destination already contains the file or directory
                                    if (Directory.Exists(destinationFullPath) || File.Exists(destinationFullPath))
                                    {
                                        // Prompt user for action: Replace, Skip, or Compare
                                        var result = MessageBox.Show($"The destination already has a file named \"{selectedItem.Name}\".\n\nWould you like to replace the existing file, skip this file, or compare the files?",
                                                                     "Replace or Skip Files",
                                                                     MessageBoxButton.YesNoCancel,
                                                                     MessageBoxImage.Question);
                                        if (result == MessageBoxResult.Yes)
                                        {
                                            // Replace the file or directory in the destination
                                            if (Directory.Exists(destinationFullPath))
                                            {
                                                Directory.Delete(destinationFullPath, true);
                                                Directory.Move(selectedItem.Path, destinationFullPath);
                                            }
                                            else if (File.Exists(destinationFullPath))
                                            {
                                                File.Delete(destinationFullPath);
                                                File.Move(selectedItem.Path, destinationFullPath);
                                            }

                                            itemsMoved = true; // Set the flag to true since an item is moved
                                        }
                                        else if (result == MessageBoxResult.Cancel)
                                        {
                                            // Compare info and prompt user to keep one or both files
                                            CompareFiles(selectedItem.Path, destinationFullPath);
                                        }
                                        // else if (result == MessageBoxResult.No) // No action needed for skipping
                                    }
                                    else
                                    {
                                        // Move the file or directory if no conflict
                                        if (Directory.Exists(selectedItem.Path))
                                        {
                                            Directory.Move(selectedItem.Path, destinationFullPath);
                                        }
                                        else if (File.Exists(selectedItem.Path))
                                        {
                                            File.Move(selectedItem.Path, destinationFullPath);
                                        }

                                        itemsMoved = true; // Set the flag to true since an item is moved
                                    }
                                }
                            }

                            if (itemsMoved )
                            {
                                // Show a success message only if items are moved
                                MessageBox.Show($"Items moved to {destinationPath}");

                                // Refresh the file list view to reflect changes
                                LoadFiles(PathBox.Text); // Ensure PathBox.Text contains the current directory path
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            MessageBox.Show($"Access denied: {ex.Message}");
                        }
                        catch (IOException ex)
                        {
                            MessageBox.Show($"File I/O error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error moving items: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select items to move.");
            }
        }

        public bool comparefiles = false;
        private void CompareFiles(string sourcePath, string destinationPath)
        {
            try
            {
                // Read the content of both files
                string sourceContent = File.ReadAllText(sourcePath);
                string destinationContent = File.ReadAllText(destinationPath);

                // Compare the content of the files
                if (sourceContent == destinationContent)
                {
                    MessageBox.Show("The files are identical.", "File Comparison", MessageBoxButton.OK, MessageBoxImage.Information);
                    ShowCompareForm(sourcePath, destinationPath);
                    comparefiles = true;
                    // return false; // Files are identical
                }
                else
                {
                    // Files are different
                    //  MessageBox.Show("The files are different.", "File Comparison", MessageBoxButton.OK, MessageBoxImage.Information);
                    comparefiles = false;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error comparing files: {ex.Message}");
                // return false; // Comparison failed
            }
        }

       
        private void ShowCompareForm(string sourcePath, string destinationPath)
        {
            var compareDialog = new System.Windows.Forms.Form
            {
                Text = "File Compare",
                Width = 500,
                Height = 300
            };

            var label = new System.Windows.Forms.Label
            {
                Text = "Which files do you want to keep?",
                Dock = System.Windows.Forms.DockStyle.Top,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            var sourceFileCheckBox = new System.Windows.Forms.CheckBox
            {
                Text = $"Source File: {sourcePath}",
                Dock = System.Windows.Forms.DockStyle.Top
            };

            var destinationFileCheckBox = new System.Windows.Forms.CheckBox
            {
                Text = $"Destination File: {destinationPath}",
                Dock = System.Windows.Forms.DockStyle.Top
            };

            var compareButton = new System.Windows.Forms.Button
            {
                Text = "Compare",
                Dock = System.Windows.Forms.DockStyle.Bottom
            };
           
            compareButton.Click += (sender, e) =>
            {
                if (sourceFileCheckBox.Checked && destinationFileCheckBox.Checked)
                {
                    // Keep both files by renaming the source file if the destination file already exists
                    if (File.Exists(destinationPath))
                    {
                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        string sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
                        string sourceExtension = Path.GetExtension(sourcePath);

                        for (int i = 1; ; i++)
                        {
                            string newName = $"{sourceFileName}_Copy({i}){sourceExtension}";
                            string newSourcePath = Path.Combine(destinationDir, newName);
                            if (!File.Exists(newSourcePath))
                            {
                                File.Move(sourcePath, newSourcePath);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Move the source file to the destination directory if no conflict
                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        string newSourcePath = Path.Combine(destinationDir, Path.GetFileName(sourcePath));
                        File.Move(sourcePath, newSourcePath);
                    }
                }
                else if (sourceFileCheckBox.Checked)
                {
                    // Move the source file to the destination
                    File.Delete(destinationPath); // Delete destination file first
                    File.Move(sourcePath, destinationPath); // Move source to destination
                }
                else if (destinationFileCheckBox.Checked)
                {
                    // Keep only the destination file
                    //  File.Delete(sourcePath);

                }

                compareDialog.Close();
            };

            compareDialog.Controls.Add(label);
            compareDialog.Controls.Add(sourceFileCheckBox);
            compareDialog.Controls.Add(destinationFileCheckBox);
            compareDialog.Controls.Add(compareButton);
            compareDialog.ShowDialog();
        }
    


        private void CopyToButton_Click(object sender, RoutedEventArgs e)
        {
            // Use FolderBrowserDialog to select the destination folder
            using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select the destination location";
                folderBrowserDialog.ShowNewFolderButton = true;

                // Set the root folder for the dialog
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer; // Set to the root of "My Computer"
                folderBrowserDialog.SelectedPath = @"D:\Folder Structure Creator";


                // Show the dialog and check if the user selected a folder
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string destinationPath = folderBrowserDialog.SelectedPath;

                    // Perform the copying action here
                    if (FileListView.SelectedItems.Count > 0)
                    {
                        try
                        {
                            foreach (var item in FileListView.SelectedItems)
                            {
                                if (item is FileItem selectedItem)
                                {
                                    string destinationFullPath = Path.Combine(destinationPath, selectedItem.Name);

                                    // Check if the selected item is a directory or file and copy accordingly
                                    if (Directory.Exists(selectedItem.Path))
                                    {
                                        CopyDirectory(selectedItem.Path, destinationFullPath);
                                    }
                                    else if (File.Exists(selectedItem.Path))
                                    {
                                        File.Copy(selectedItem.Path, destinationFullPath);
                                    }
                                }
                            }

                            // Show a success message
                            MessageBox.Show($"{FileListView.SelectedItems.Count} item(s) copied to {destinationPath}");
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            MessageBox.Show($"Access denied: {ex.Message}");
                        }
                        catch (IOException ex)
                        {
                            MessageBox.Show($"File I/O error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error copying items: {ex.Message}");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Please select items to copy.");
                    }
                }
            }
        }


        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItemsPaths.Count > 0)
            {
                string destinationPath = PathBox.Text;
                try
                {
                    foreach (string selectedItemPath in selectedItemsPaths)
                    {
                        string destinationFileName = Path.GetFileName(selectedItemPath);
                        string destinationFullPath = Path.Combine(destinationPath, destinationFileName);

                        // If the destination file already exists, rename it with an incrementing suffix
                        string baseName = Path.GetFileNameWithoutExtension(destinationFullPath);
                        string extension = Path.GetExtension(destinationFullPath);
                        string newName = baseName;

                        int copyNumber = 1;

                        // Check if the base name already exists
                        while (File.Exists(destinationFullPath) || Directory.Exists(destinationFullPath))
                        {
                            if (copyNumber == 1)
                            {
                                // For the first copy, add the suffix directly without any numbering
                                newName = $"{baseName}-Copy";
                            }
                            else
                            {
                                // For subsequent copies, append the copy number within parentheses
                                newName = $"{baseName}-Copy({copyNumber})";
                            }

                            destinationFullPath = Path.Combine(destinationPath, newName + extension);
                            copyNumber++;
                        }

                        if (Directory.Exists(selectedItemPath))
                        {
                            CopyDirectory(selectedItemPath, destinationFullPath);
                        }
                        else if (File.Exists(selectedItemPath))
                        {
                            File.Copy(selectedItemPath, destinationFullPath, overwrite: true);
                        }
                    }
                    //  MessageBox.Show("Paste operation completed successfully.");
                    LoadFiles(destinationPath); // Refresh the view
                }
                catch (UnauthorizedAccessException ex)
                {
                    // MessageBox.Show($"Access denied: {ex.Message}");
                }
                catch (IOException ex)
                {
                    // MessageBox.Show($"File I/O error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                // MessageBox.Show("No items to paste. Please copy files or folders first.");
            }
        }




        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it
            Directory.CreateDirectory(destinationDir);

            // Get the files in the directory and copy them to the new location
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(tempPath, true);
            }

            // Copy subdirectories and their contents to the new location
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destinationDir, subdir.Name);
                CopyDirectory(subdir.FullName, tempPath);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)   //srikanth konnagula
        {
            if (currentHistoryIndex > 0)
            {
                currentHistoryIndex--;
                isNavigating = true;
                LoadFiles(navigationHistory[currentHistoryIndex]);
                PathBox.Text = navigationHistory[currentHistoryIndex];
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentHistoryIndex < navigationHistory.Count - 1)
            {
                currentHistoryIndex++;
                isNavigating = true;
                LoadFiles(navigationHistory[currentHistoryIndex]);
                PathBox.Text = navigationHistory[currentHistoryIndex];
            }
        }

      
        private void ExtractHere_Click(object sender, RoutedEventArgs e)     //srikanth konnagula
        {
            var selectedItem = FileListView.SelectedItem as FileItem;
            if (selectedItem != null && selectedItem.Type.Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "Select the extraction location";
                    folderBrowserDialog.ShowNewFolderButton = true;

                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string extractPath = folderBrowserDialog.SelectedPath;
                        try
                        {
                            // Get the file name without extension
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(selectedItem.Path);
                            // Create a folder with the same name as the ZIP file
                            string destinationFolderPath = Path.Combine(extractPath, fileNameWithoutExtension);
                            Directory.CreateDirectory(destinationFolderPath);

                            // Extract the contents of the ZIP file to the created folder
                            ZipFile.ExtractToDirectory(selectedItem.Path, destinationFolderPath);
                            MessageBox.Show($"Files extracted to {destinationFolderPath}");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error extracting files: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a zip file to extract.");
            }
        }

        private void Restore_Click(object sender, RoutedEventArgs e)
        {

        }


        private void RenameButton_Click(object sender, RoutedEventArgs e)   //srikanth konnagula
        {
            if (FileListView.SelectedItem is FileItem selectedItem)
            {
                string oldPath = selectedItem.Path;
                string oldName = selectedItem.Name;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(oldName);
                string fileExtension = Path.GetExtension(oldName);

                // Check if the directory contains "Archive" in its path
                if (oldPath.Contains("Archive"))
                {
                    MessageBox.Show("The 'Archive' folder is not renamable.");
                    return;
                }

                var listViewItem = FileListView.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem;
                if (listViewItem != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(listViewItem);
                    var textBox = FindVisualChild<TextBox>(listViewItem);

                    if (textBlock != null && textBox != null)
                    {
                        textBlock.Visibility = Visibility.Collapsed;
                        textBox.Text = fileNameWithoutExtension;
                        textBox.Visibility = Visibility.Visible;
                        textBox.Focus();
                        textBox.SelectAll();

                        // Directly handle rename on Enter key press
                        textBox.KeyDown += (s, args) =>
                        {
                            if (args.Key == Key.Enter)
                            {
                                string newFileName = textBox.Text;
                                if (!newFileName.EndsWith(fileExtension))
                                {
                                    newFileName += fileExtension;
                                }
                                FinalizeRename(selectedItem.Path, newFileName, selectedItem, textBox, textBlock);
                            }
                        };

                        // Handle rename on lost focus
                        textBox.LostFocus += (s, args) =>
                        {
                            string newFileName = textBox.Text;
                            if (!newFileName.EndsWith(fileExtension))
                            {
                                newFileName += fileExtension;
                            }
                            FinalizeRename(selectedItem.Path, newFileName, selectedItem, textBox, textBlock);
                        };
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file or folder to rename.");
            }
        }


        private void FinalizeRename(string oldPath, string newName, FileItem selectedItem, TextBox textBox, TextBlock textBlock)
        {
            string directory = Path.GetDirectoryName(oldPath);
            string newPath = Path.Combine(directory, newName);

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("File name cannot be empty.");
                RetryRename(textBox, textBlock);
                return;
            }

            try
            {
                // Check if another file or directory with the same name already exists in the directory
                bool nameExists = Directory.EnumerateFileSystemEntries(directory)
                    .Any(entry => string.Equals(Path.GetFileName(entry), newName, StringComparison.OrdinalIgnoreCase) && !string.Equals(entry, oldPath, StringComparison.OrdinalIgnoreCase));

                if (nameExists)
                {
                    MessageBox.Show("A file or directory with the same name already exists.");
                    RetryRename(textBox, textBlock);
                    return;
                }

                if (Directory.Exists(oldPath))
                {
                    Directory.Move(oldPath, newPath);
                }
                else if (File.Exists(oldPath))
                {
                    File.Move(oldPath, newPath);
                }
                else
                {
                    MessageBox.Show("Selected item no longer exists.");
                    RetryRename(textBox, textBlock);
                    return;
                }

                // Update the item's properties only if the rename operation was successful
                selectedItem.Name = newName;
                selectedItem.Path = newPath;
                selectedItem.DateModified = DateTime.Now.ToString(); // Update modification date

                // Update the UI elements
                UpdateUIAfterRename(selectedItem, newName);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Rename failed: {ex.Message}");
                RetryRename(textBox, textBlock);
            }
        }

        private void RetryRename(TextBox textBox, TextBlock textBlock)
        {
            textBox.Visibility = Visibility.Visible;
            textBox.Focus();
            textBox.SelectAll();
            textBlock.Visibility = Visibility.Collapsed;
        }

        private void UpdateUIAfterRename(FileItem selectedItem, string newName)
        {
            var listViewItem = FileListView.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem;
            if (listViewItem != null)
            {
                var textBlock = FindVisualChild<TextBlock>(listViewItem);
                var textBox = FindVisualChild<TextBox>(listViewItem);

                if (textBlock != null)
                {
                    textBlock.Text = newName;
                    textBlock.Visibility = Visibility.Visible;
                }

                if (textBox != null)
                {
                    textBox.Visibility = Visibility.Collapsed;
                }
            }

            // Refresh the FileListView to show the updated name
            FileListView.Items.Refresh();
        }



        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var selectedItem = FileListView.SelectedItem as FileItem;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)       //srikanth konnagula
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                var fileItem = (FileItem)textBox.DataContext;
                string newFileName = textBox.Text;

                // Hide the TextBox and show the TextBlock
                textBox.Visibility = Visibility.Collapsed;
                var listViewItem = FileListView.ItemContainerGenerator.ContainerFromItem(fileItem) as ListViewItem;
                var textBlock = FindVisualChild<TextBlock>(listViewItem);
                if (textBlock != null)
                {
                    textBlock.Visibility = Visibility.Visible;
                }

                // Check if the file or folder exists before finalizing the rename
                if ((File.Exists(fileItem.Path) || Directory.Exists(fileItem.Path)) && isRenameInitiated)
                {
                    FinalizeRename(fileItem.Path, newFileName, fileItem, textBox, textBlock);

                    // Reset the flag after renaming
                    isRenameInitiated = false;
                }
                else
                {
                    //MessageBox.Show("The file or folder does not exist.");
                }
            }
        }



        private string GetCurrentDirectory()
        {
            // Return the current directory from PathBox
            return PathBox.Text;
        }

        private void RefreshView(string path)
        {
            // Refresh the files in the ListView
            LoadFiles(path);
            RefreshTreeView(path);
        }

        private void RefreshTreeView(string path)
        {

            DirectoryTree.Items.Clear();
            LoadDirectoryTree();
        }
        private void NewTextDocument_Click(object sender, RoutedEventArgs e)   //srikanth konnagula
        {

            // Get the current directory from the PathBox or selected item
            string currentDirectory = GetCurrentDirectory();

            if (string.IsNullOrEmpty(currentDirectory))
            {
                MessageBox.Show("Please select a directory.");
                return;
            }

            // Generate a unique file name
            string newTextDocName = "New Text Document";
            int count = 1;
            while (File.Exists(Path.Combine(currentDirectory, newTextDocName + ".txt")))
            {
                newTextDocName = $"New Text Document ({count++})";
            }

            // Create the new text document file in the file system
            string newTextDocPath = Path.Combine(currentDirectory, newTextDocName + ".txt");
            File.WriteAllText(newTextDocPath, string.Empty); // Create an empty text file

            // Create a new FileItem for the new file
            var newFileItem = new FileItem
            {
                Name = newTextDocName,
                Path = newTextDocPath,
                Size = "",
                Type = "Text Document",
                DateModified = DateTime.Now.ToString(),
                Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/txts.png")) // Use relative path for the image
            };

            // Add the new item to the ObservableCollection
            allFiles.Add(newFileItem);

            // Set focus to the new item and make the name editable
            Dispatcher.BeginInvoke(new Action(() =>
            {

                var newItem = FileListView.ItemContainerGenerator.ContainerFromItem(newFileItem) as ListViewItem;
                if (newItem != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(newItem);
                    if (textBlock != null)
                    {
                        textBlock.Visibility = Visibility.Collapsed;
                    }
                    newItem.Focus();
                    var textBox = FindVisualChild<TextBox>(newItem);
                    if (textBox != null)
                    {
                        textBox.Text = newTextDocName; // Set the text box to the name without the extension
                        textBox.Visibility = Visibility.Visible;
                        textBox.Focus();
                        textBox.SelectAll();
                        textBox.LostFocus += (s, ev) =>
                        {
                            FinalizeNewTextDocument(currentDirectory, textBox.Text + ".txt", newTextDocPath);
                            // File.Visibility = Visibility.Collapsed;
                        };
                    }

                }
            }), DispatcherPriority.Loaded);
        }

        private void FinalizeNewTextDocument(string directory, string newName, string oldPath)
        {
            string newFilePath = Path.Combine(directory, newName);

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("File name cannot be empty.");
                return;
            }

            if (File.Exists(newFilePath))
            {
                //MessageBox.Show("A file with the same name already exists.");
                return;
            }

            try
            {
                File.Move(oldPath, newFilePath);
                var fileItem = allFiles.FirstOrDefault(f => f.Path == oldPath);
                if (fileItem != null)
                {
                    fileItem.Name = newName;
                    fileItem.Path = newFilePath;
                    fileItem.DateModified = DateTime.Now.ToString();
                }
                RefreshView(directory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error renaming file: {ex.Message}");
            }
        }

        private void PathBox_KeyDown(object sender, KeyEventArgs e)    //bhanu
        {
            if (e.Key == Key.Enter)
            {
                // Get the new path from the PathBox
                string newPath = PathBox.Text;

                // Check if the path is empty
                if (string.IsNullOrWhiteSpace(newPath))
                {
                    // Show a MessageBox informing the user to enter a path
                   // MessageBox.Show("Please enter a valid path.", "File Explorer", MessageBoxButton.OK, MessageBoxImage.Information);
                    return; // Exit the method
                }

                // Check if the path exists
                if (Directory.Exists(newPath))
                {
                    // Load files and directories from the new path
                    LoadFiles(newPath);
                }
                else
                {
                    // Show a MessageBox similar to the Windows File Explorer error
                    MessageBox.Show($"Windows can't find '{newPath}'. Check the spelling and try again.", "File Explorer", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Remove the last part of the path
                    // newPath = RemoveLastPathComponent(newPath);

                    // Update the PathBox with the corrected path
                    PathBox.Text = newPath;
                    PathBox.CaretIndex = newPath.Length; // Move the caret to the end
                }
            }
        }



        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)     //bhanu
        {
            var searchQuery = SearchBox.Text.ToLower();
            if (allFiles != null)
            {
                var filteredFiles = new ObservableCollection<FileItem>();
                foreach (var file in allFiles)
                {
                    if (file.Name.ToLower().Contains(searchQuery))
                    {
                        filteredFiles.Add(file);
                    }
                }

                if (filteredFiles.Count == 0)
                {
                    filteredFiles.Add(new FileItem { Name = "No items match your search.", IsSpecialMessage = true });
                }

                FileListView.ItemsSource = filteredFiles;

                // Highlight search results if there are any matches
                if (filteredFiles.Count > 1 || !filteredFiles[0].IsSpecialMessage)
                {
                    FileListView.Dispatcher.BeginInvoke(new Action(() => HighlightSearchResults(searchQuery)), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private void HighlightSearchResults(string searchQuery)
        {
            foreach (FileItem item in FileListView.Items)
            {
                if (item.IsSpecialMessage) continue;

                var listViewItem = (ListViewItem)FileListView.ItemContainerGenerator.ContainerFromItem(item);
                if (listViewItem != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(listViewItem);
                    if (textBlock != null)
                    {
                        var text = textBlock.Text;
                        int index = text.ToLower().IndexOf(searchQuery);
                        if (index >= 0)
                        {
                            textBlock.Inlines.Clear();
                            textBlock.Inlines.Add(new Run(text.Substring(0, index)));
                            textBlock.Inlines.Add(new Run(text.Substring(index, searchQuery.Length)) { Background = Brushes.Yellow });
                            textBlock.Inlines.Add(new Run(text.Substring(index + searchQuery.Length)));
                        }
                    }
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject       //srikanth konnagula
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }
         
        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)   //bhanu sorting
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string sortBy = headerClicked.Column.Header as string;
                    if (sortBy == "Date Modified") // Replace with the actual header name of your date column
                    {
                        SortDate(sortBy, direction);
                        UpdateSortArrow(headerClicked, direction);
                    }
                    else if (sortBy == "Size") // Replace with the actual header name of your size column
                    {
                        SortSize(sortBy, direction);
                        UpdateSortArrow(headerClicked, direction);
                    }
                    else
                    {
                        Sort(sortBy, direction);
                        UpdateSortArrow(headerClicked, direction);
                    }

                    lastHeaderClicked = headerClicked;
                    lastDirection = direction;
                }
            }
        }

        private void SortDate(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(FileListView.ItemsSource);
            ListCollectionView listCollectionView = dataView as ListCollectionView;

            if (listCollectionView != null)
            {
                listCollectionView.CustomSort = new DateComparer("DateModified");

                if (direction == ListSortDirection.Descending)
                {
                    // Invert the sorting direction if sorting from latest to oldest
                    listCollectionView.CustomSort = new InvertComparer(listCollectionView.CustomSort);
                }
            }

            dataView.Refresh();
        }

        private void SortSize(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(FileListView.ItemsSource);
            ListCollectionView listCollectionView = dataView as ListCollectionView;

            if (listCollectionView != null)
            {
                listCollectionView.CustomSort = new SizeComparer("Size");

                if (direction == ListSortDirection.Descending)
                {
                    listCollectionView.CustomSort = new InvertComparer(listCollectionView.CustomSort);
                }
            }

            dataView.Refresh();
        }

        public class DateComparer : IComparer
        {
            private readonly string _propertyName;

            public DateComparer(string propertyName)
            {
                _propertyName = propertyName;
            }

            public int Compare(object x, object y)
            {
                if (x == null || y == null)
                {
                    Console.WriteLine("One of the objects is null.");
                    return 0;
                }

                var valueX = GetPropertyValue(x, _propertyName);
                var valueY = GetPropertyValue(y, _propertyName);

                if (valueX == null || valueY == null)
                {
                    Console.WriteLine("One of the values is null.");
                    return 0;
                }

                DateTime? dateX = ConvertToDateTime(valueX);
                DateTime? dateY = ConvertToDateTime(valueY);

                if (!dateX.HasValue || !dateY.HasValue)
                {
                    Console.WriteLine("One of the values is not a valid DateTime. valueX: {0}, valueY: {1}", valueX, valueY);
                    return 0;
                }

                return DateTime.Compare(dateX.Value, dateY.Value); // Compare date and time directly
            }

            private static object GetPropertyValue(object obj, string propertyName)
            {
                foreach (var part in propertyName.Split('.'))
                {
                    if (obj == null) return null;
                    var type = obj.GetType();
                    var info = type.GetProperty(part);
                    if (info == null) return null;
                    obj = info.GetValue(obj, null);
                }
                return obj;
            }

            private static DateTime? ConvertToDateTime(object value)
            {
                if (value is DateTime dt)
                {
                    return dt;
                }
                else if (value is string s)
                {
                    if (DateTime.TryParse(s, out DateTime result))
                    {
                        return result;
                    }
                }
                return null;
            }
        }

        public class SizeComparer : IComparer
        {
            private readonly string _propertyName;

            public SizeComparer(string propertyName)
            {
                _propertyName = propertyName;
            }

            public int Compare(object x, object y)
            {
                if (x == null || y == null)
                {
                    Console.WriteLine("One of the objects is null.");
                    return 0;
                }

                var valueX = ConvertToSize(GetPropertyValue(x, _propertyName));
                var valueY = ConvertToSize(GetPropertyValue(y, _propertyName));

                if (valueX == null && valueY == null)
                {
                    return 0; // Both are null, they are considered equal
                }
                if (valueX == null)
                {
                    return 1; // valueX is null, put it after valueY
                }
                if (valueY == null)
                {
                    return -1; // valueY is null, put it after valueX
                }

                return valueX.Value.CompareTo(valueY.Value);
            }

            private static object GetPropertyValue(object obj, string propertyName)
            {
                foreach (var part in propertyName.Split('.'))
                {
                    if (obj == null) return null;
                    var type = obj.GetType();
                    var info = type.GetProperty(part);
                    if (info == null) return null;
                    obj = info.GetValue(obj, null);
                }
                return obj;
            }

            private static long? ConvertToSize(object value)
            {
                if (value is long size)
                {
                    return size;
                }
                else if (value is string s)
                {
                    s = s.Trim();
                    if (string.IsNullOrEmpty(s))
                    {
                        return 0; // Handle empty or null strings
                    }

                    var numberPart = new string(s.TakeWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
                    var unitPart = new string(s.SkipWhile(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Trim().ToUpper();

                    if (double.TryParse(numberPart, out double number))
                    {
                        switch (unitPart)
                        {
                            case "KB":
                                return (long)(number * 1024);
                            case "MB":
                                return (long)(number * 1024 * 1024);
                            case "GB":
                                return (long)(number * 1024 * 1024 * 1024);
                            case "B":
                            case "":
                                return (long)number;
                            default:
                                Console.WriteLine($"Unexpected size unit: {unitPart}");
                                return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to parse number: {numberPart}");
                        return null;
                    }
                }
                return 0;
            }
        }


        public class InvertComparer : IComparer
        {
            private readonly IComparer _baseComparer;

            public InvertComparer(IComparer baseComparer)
            {
                _baseComparer = baseComparer;
            }

            public int Compare(object x, object y)
            {
                return _baseComparer.Compare(y, x); // Reverse the order of comparison
            }
        }
        private void UpdateSortArrow(GridViewColumnHeader header, ListSortDirection direction)
        {
            if (lastHeaderClicked != null)
            {
                // Clear the previous arrow
                var lastArrow = FindVisualChild<System.Windows.Shapes.Path>(lastHeaderClicked);
                if (lastArrow != null)
                {
                    lastArrow.Visibility = Visibility.Collapsed;
                }
            }

            // Update the new arrow
            var arrow = FindVisualChild<System.Windows.Shapes.Path>(header);
            if (arrow != null)
            {
                arrow.Visibility = Visibility.Visible;
                var rotateTransform = new RotateTransform
                {
                    Angle = direction == ListSortDirection.Ascending ? 180 : 0,
                    CenterX = 5,
                    CenterY = 5
                };
                arrow.RenderTransform = rotateTransform;

                // Set the direction tag to the header
                header.Tag = direction;
            }

            // Remember the current header for the next update
            lastHeaderClicked = header;
            lastDirection = direction;
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(FileListView.ItemsSource);
            dataView.SortDescriptions.Clear();
            dataView.SortDescriptions.Add(new SortDescription(sortBy, direction));
            dataView.Refresh();
        }


        private void FileListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        

        }
      


    }
}







