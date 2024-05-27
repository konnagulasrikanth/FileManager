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
        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
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
           FileListView.MouseMove+= FileListView_MouseMove;
            FileListView.MouseDoubleClick += FileListView_MouseDoubleClick; // Add this line
            DirectoryTree.SelectedItemChanged += DirectoryTree_SelectedItemChanged;
           FileListView.PreviewMouseLeftButtonDown+= FileListView_PreviewMouseLeftButtonDown;

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

      
        private string GetReadableFileSize(long size)
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

        private void LoadFiles(string path)
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



        private BitmapImage GetIconForFile(string extension)
        {
            switch (extension.ToLower())
            {
                case ".zip":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/zip.png"));
                case ".txt":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/txt.png"));
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


   
        public class FileItem : INotifyPropertyChanged
        {
            private string name;
            private string path;
            private string size;
            private string type;
            private string dateModified;
            private BitmapImage icon;
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


        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
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

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selectedItem && Directory.Exists(selectedItem.Path))
            {
                // If it's a directory, navigate into it
                LoadFiles(selectedItem.Path);
            }
        }

        private void OpenFile(string filePath)
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
                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = "notepad.exe",
                            Arguments = filePath,
                            UseShellExecute = true
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

        private void RefreshFileItem(string filePath)
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

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
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
                    }
                }
            }
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
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


        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var fileItem = (FileItem)textBox.DataContext;
            textBox.Visibility = Visibility.Collapsed;

            var listViewItem = FileListView.ItemContainerGenerator.ContainerFromItem(fileItem) as ListViewItem;
            if (listViewItem != null)
            {
                var textBlock = FindVisualChild<TextBlock>(listViewItem);
                if (textBlock != null)
                {
                    textBlock.Visibility = Visibility.Visible;
                }
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = (TextBox)sender;
                var fileItem = (FileItem)textBox.DataContext;
                fileItem.Name = textBox.Text;
                textBox.Visibility = Visibility.Collapsed;

                var listViewItem = FileListView.ItemContainerGenerator.ContainerFromItem(fileItem) as ListViewItem;
                if (listViewItem != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(listViewItem);
                    if (textBlock != null)
                    {
                        textBlock.Visibility = Visibility.Visible;
                    }
                }
            }
        }


        // Helper method to find the child element of a specific type
        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }



        private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
           
        }
        private List<FileItem> selectedFiles = new List<FileItem>();
        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedFiles.Clear(); // Clear the previous selection
            foreach (FileItem item in FileListView.SelectedItems)
            {
                selectedFiles.Add(item); // Add the selected files to the list
            }
        }


     
        private void CreateFolderStructure_Click(object sender, RoutedEventArgs e) //Srikanth
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
                    Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/Folder1.jpg"))
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
                            textBox.LostFocus += (s, ev) => {
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

        //private void CreateSDLCSubfolders(string rootPath)
        //{
        //    string[] subfolders = new string[]
        //    {
        //"Requirement",
        //"Build&Design",
        //"Deployment",
        //"Testing"
        //    };

        //    try
        //    {
        //        foreach (var subfolder in subfolders)
        //        {
        //            string subfolderPath = System.IO.Path.Combine(rootPath, subfolder);
        //            if (!Directory.Exists(subfolderPath))
        //            {
        //                Directory.CreateDirectory(subfolderPath);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error creating SDLC subfolders: {ex.Message}");
        //    }
        //}
        private void CreateSDLCSubfolders(string rootPath)
        {
            var subfolders = new Dictionary<string, string[]>
    {
        { "Requirements", new string[] { "Documents", "Specifications" } },
        { "Design", new string[] { "Diagrams", "Prototypes" } },
        { "Implementation", new string[] { "SourceCode", "Binaries" } },
        { "Testing", new string[] { "TestCases", "Reports" } },
        { "Deployment", new string[] { "Scripts", "Documentation" } }
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



        private void FileListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string targetPath = PathBox.Text;

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
        private void FileListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelectedItems();
            }
        }

        private void DeleteSelectedItems()
        {
            if (selectedFiles.Any())
            {
                var result = MessageBox.Show("Are you sure you want to delete the selected items?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
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
                                // Zip and archive the folder
                                string zipFileName = $"{Path.GetFileName(item.Path)}.zip";
                                string zipFilePath = Path.Combine(archivePath, zipFileName);
                                ZipFolder(item.Path, zipFilePath);

                                // Delete the original folder
                                Directory.Delete(item.Path, true);
                            }
                            else if (File.Exists(item.Path))
                            {
                                File.Delete(item.Path);
                            }
                        }

                        // Refresh the file list view
                        LoadFiles(CurrentDirectory);
                        MessageBox.Show("Selected items deleted and archived successfully.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting items: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("No items selected for deletion.");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItems();
        }

        private void ZipFolder(string sourceFolder, string zipFilePath)
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

        private bool IsArchiveFolder(string folderPath, string archivePath)
        {
            // Check if the folderPath is the same as archivePath or a subfolder of archivePath
            return folderPath.Equals(archivePath, StringComparison.OrdinalIgnoreCase) ||
                   folderPath.StartsWith(archivePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
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

        private void FileListView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void FileListView_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
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

        private void FileListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && FileListView.SelectedItem != null)
            {
                var selectedFile = FileListView.SelectedItem as FileItem;
                if (selectedFile != null)
                {
                    DragDrop.DoDragDrop(FileListView, new DataObject(DataFormats.FileDrop, new string[] { selectedFile.Path }), DragDropEffects.Copy);
                }
            }
        }
        private void CopyButton_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show($"{selectedItemsPaths.Count} item(s) copied to clipboard.");
            }
            else
            {
                MessageBox.Show("Please select files or folders to copy.");
            }
        }
        private void MoveItem_Click(object sender, RoutedEventArgs e)    
        {
            // Check if any items are selected
            if (FileListView.SelectedItems.Count > 0)
            {
                // Use FolderBrowserDialog to select the destination folder
                using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    folderBrowserDialog.Description = "Select the destination location";
                    folderBrowserDialog.ShowNewFolderButton = true;

                    // Show the dialog and check if the user selected a folder
                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string destinationPath = folderBrowserDialog.SelectedPath;

                        try
                        {
                            foreach (var item in FileListView.SelectedItems)
                            {
                                if (item is FileItem selectedItem)
                                {
                                    string destinationFullPath = Path.Combine(destinationPath, selectedItem.Name);

                                    // Check if the selected item is a directory or file and move accordingly
                                    if (Directory.Exists(selectedItem.Path))
                                    {
                                        Directory.Move(selectedItem.Path, destinationFullPath);
                                    }
                                    else if (File.Exists(selectedItem.Path))
                                    {
                                        File.Move(selectedItem.Path, destinationFullPath);
                                    }
                                }
                            }

                            // Show a success message
                            MessageBox.Show($"Items moved to {destinationPath}");

                            // Refresh the file list view to reflect changes
                            LoadFiles(PathBox.Text); // Ensure PathBox.Text contains the current directory path
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
        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItemsPaths.Count > 0)
            {
                string destinationPath = PathBox.Text;
                try
                {
                    foreach (string selectedItemPath in selectedItemsPaths)
                    {
                        string destinationFullPath = Path.Combine(destinationPath, Path.GetFileName(selectedItemPath));
                        if (Directory.Exists(selectedItemPath))
                        {
                            CopyDirectory(selectedItemPath, destinationFullPath);
                        }
                        else if (File.Exists(selectedItemPath))
                        {
                            File.Copy(selectedItemPath, destinationFullPath, overwrite: true);
                        }
                    }
                    MessageBox.Show("Copying operation completed successfully.");
                    LoadFiles(destinationPath); // Refresh the view
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
                    MessageBox.Show($"An error occurred: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("No items to paste. Please copy files or folders first.");
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
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

        private void ExtractHere_Click(object sender, RoutedEventArgs e)
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
                            ZipFile.ExtractToDirectory(selectedItem.Path, extractPath);
                            MessageBox.Show($"Files extracted to {extractPath}");
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
        private void ExtractZipFile(string zipFilePath, string extractPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                MessageBox.Show("Extraction completed successfully.");
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
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
      
        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selectedItem)
            {
                string oldPath = selectedItem.Path;
                string oldName = selectedItem.Name;

                var listViewItem = FileListView.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem;
                if (listViewItem != null)
                {
                    var textBlock = FindVisualChild<TextBlock>(listViewItem);
                    if (textBlock != null)
                    {
                        textBlock.Visibility = Visibility.Collapsed;
                    }

                    var textBox = FindVisualChild<TextBox>(listViewItem);
                    if (textBox != null)
                    {
                        textBox.Text = oldName; // Set the text box to the current name
                        textBox.Visibility = Visibility.Visible;
                        textBox.Focus();
                        textBox.SelectAll();
                        textBox.LostFocus += (s, ev) =>
                        {
                            FinalizeRename(oldPath, textBox.Text, selectedItem);
                        };
                        textBox.KeyDown += (s, ev) =>
                        {
                            if (ev.Key == Key.Enter)
                            {
                                FinalizeRename(oldPath, textBox.Text, selectedItem);
                            }
                        };
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file or folder to rename.");
            }
        }

        private void FinalizeRename(string oldPath, string newName, FileItem selectedItem)
        {
            string directory = System.IO.Path.GetDirectoryName(oldPath);
            string newPath = System.IO.Path.Combine(directory, newName);

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("File name cannot be empty.");
                return;
            }

            if (File.Exists(newPath))
            {
                //MessageBox.Show("A file with the same name already exists.");
                return;
            }

            try
            {
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
                    //MessageBox.Show("Selected item no longer exists.");
                    return;
                }

                // Update the item's properties
                selectedItem.Name = newName;
                selectedItem.Path = newPath;
                selectedItem.DateModified = DateTime.Now.ToString(); // Update modification date
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Rename failed: {ex.Message}");
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
        private void NewTextDocument_Click(object sender, RoutedEventArgs e)   //text document creating
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
                Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/txt.png")) // Use relative path for the image
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
                        textBox.LostFocus += (s, ev) => {
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

        private void PathBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Get the new path from the PathBox
                string newPath = PathBox.Text;

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
                    newPath = RemoveLastPathComponent(newPath);

                    // Update the PathBox with the corrected path
                    PathBox.Text = newPath;
                    PathBox.CaretIndex = newPath.Length; // Move the caret to the end
                }
            }
        }
        private string RemoveLastPathComponent(string path)
        {
            // Use Path.GetDirectoryName to remove the last part of the path
            return Path.GetDirectoryName(path);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
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
                FileListView.ItemsSource = filteredFiles;
                HighlightSearchResults(searchQuery);
            }
        }
        private void HighlightSearchResults(string searchQuery)
        {
             foreach (FileItem item in FileListView.Items)
                {
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

        private void FileListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                ListSortDirection direction;
                if (headerClicked != _lastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (_lastDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
                var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

                Sort(sortBy, direction);

                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }

        }
        private void Sort(string sortBy, ListSortDirection direction)
        {
            ICollectionView dataView = CollectionViewSource.GetDefaultView(FileListView.ItemsSource);

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            dataView.Refresh();
        }
    }


}



