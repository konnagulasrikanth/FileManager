using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WpfAnimatedGif;
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
using System.Diagnostics.Tracing;
using Microsoft.VisualBasic.ApplicationServices;
using System.Windows.Controls.Primitives;







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
        private DragAdorner dragAdorner;
        private AdornerLayer adornerLayer;
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
      

        private bool isCopyEnabled;
        public bool IsCopyEnabled
        {
            get { return isCopyEnabled; }
            set { isCopyEnabled = value; OnPropertyChanged(nameof(IsCopyEnabled)); }
        }

        private bool isCutEnabled;
        public bool IsCutEnabled
        {
            get { return isCutEnabled; }
            set { isCutEnabled = value; OnPropertyChanged(nameof(IsCutEnabled)); }
        }

        private bool isPasteEnabled;
        public bool IsPasteEnabled
        {
            get { return isPasteEnabled; }
            set { isPasteEnabled = value; OnPropertyChanged(nameof(IsPasteEnabled)); }
        }


        private bool isUndoCopyEnabled;
        public bool IsUndoCopyEnabled
        {
            get { return isUndoCopyEnabled; }
            set { isUndoCopyEnabled = value; OnPropertyChanged(nameof(IsUndoCopyEnabled)); }
        }

        private bool isRedoCopyEnabled;
        public bool IsRedoCopyEnabled
        {
            get { return isRedoCopyEnabled; }
            set { isRedoCopyEnabled = value; OnPropertyChanged(nameof(IsRedoCopyEnabled)); }
        }
        // New properties
        private bool isDeleteEnabled;
        public bool IsDeleteEnabled
        {
            get { return isDeleteEnabled; }
            set { isDeleteEnabled = value; OnPropertyChanged(nameof(IsDeleteEnabled)); }
        }

        private bool isExtractEnabled;
        public bool IsExtractEnabled
        {
            get { return isExtractEnabled; }
            set { isExtractEnabled = value; OnPropertyChanged(nameof(IsExtractEnabled)); }
        }

        private bool isNewEnabled;
        public bool IsNewEnabled
        {
            get { return isNewEnabled; }
            set { isNewEnabled = value; OnPropertyChanged(nameof(IsNewEnabled)); }
        }

        private bool isRenameEnabled;
        public bool IsRenameEnabled
        {
            get { return isRenameEnabled; }
            set { isRenameEnabled = value; OnPropertyChanged(nameof(IsRenameEnabled)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private FileItem previousSelectedItem;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // Ensure DataContext is set for bindings
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

            DataContext = this;


        }
       
        private void FileContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            // Disable the menu items
            IsCopyEnabled = true;
            IsPasteEnabled = false;
            IsUndoCopyEnabled = false;
            IsRedoCopyEnabled = false;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Your initialization code here
            // For example, load initial data, set up the UI, etc.
            // Set initial states of buttons
            IsCopyEnabled = false;
            IsCutEnabled = false;
            IsPasteEnabled = false;
            IsUndoCopyEnabled = false;
            IsRedoCopyEnabled = false;

            // Initialize new properties
            IsDeleteEnabled = false;
            IsExtractEnabled = false;
            IsNewEnabled = true;
            IsRenameEnabled = false;
            LoadFiles("D:\\Folder Structure Creator"); // You can change this to your desired initial directory

            SetImageSource("pack://application:,,,/Resources/sun.png");

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
            if (!Directory.Exists(path))
            {
               // MessageBox.Show($"The directory '{path}' does not exist.");
                return;
            }
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
                                 // Update the item count label
            int itemCount = allFiles.Count;
            ItemCountLabel.Text = itemCount == 1 ? "1 Item" : $"{itemCount} Items";
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
                //MessageBox.Show($"Error renaming folder: {ex.Message}");
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


            // Clear the previous selection
            selectedFiles.Clear();

            // Enable or disable buttons based on whether any items are selected
            bool anyItemsSelected = FileListView.SelectedItems.Count > 0;
            CopyButton.IsEnabled = anyItemsSelected;
            MoveButton.IsEnabled = anyItemsSelected;
            CopyToButton.IsEnabled = anyItemsSelected;
            DeleteButton.IsEnabled = anyItemsSelected;
            RenameButton.IsEnabled = anyItemsSelected;
            MoveButton.IsEnabled = anyItemsSelected;
            IsNewEnabled   = true;


            foreach (FileItem item in FileListView.SelectedItems)
            {
                selectedFiles.Add(item);
                string fileName = item.Name;
                string itemPath = item.Path;

                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // Disable options except Extract
                    IsCopyEnabled = false;
                    IsPasteEnabled = false;
                    IsUndoCopyEnabled = false;
                    IsRedoCopyEnabled = false;
                    IsCutEnabled = false;
                    IsDeleteEnabled = false;
                    IsRenameEnabled = false;
                    IsNewEnabled = false;
                    CopyToButton.IsEnabled = false;
                    PasteToButton.IsEnabled = false;
                    DeleteButton.IsEnabled = false;
                    RenameButton.IsEnabled = false;
                    CopyButton.IsEnabled = false;
                    MoveButton.IsEnabled = false;
                    CreateButton.IsEnabled = false;
                    IsExtractEnabled = true;
                    Extract.IsEnabled = true;

                    return;
                }

                // Check if the path contains "Archive"
                if (itemPath.IndexOf("Archive", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Disable options including Extract and New
                    IsCopyEnabled = false;
                    IsPasteEnabled = false;
                    IsUndoCopyEnabled = false;
                    IsRedoCopyEnabled = false;
                    IsCutEnabled = false;
                    IsDeleteEnabled = false;
                    IsRenameEnabled = false;
                    IsExtractEnabled = false;
                    IsNewEnabled = false;
                    CopyToButton.IsEnabled = false;
                    PasteToButton.IsEnabled = false;
                    DeleteButton.IsEnabled = false;
                    RenameButton.IsEnabled = false;
                    CopyButton.IsEnabled = false;
                    MoveButton.IsEnabled = false;
                    CreateButton.IsEnabled = false;
                    Extract.IsEnabled = false;

                    return;
                }
            }

            // Print the currently selected items' directory paths
            foreach (FileItem item in selectedFiles)
            {
                Console.WriteLine($"Selected item path: {item.Path}");
            }

            // Check if the selected item is the same as the previous selection
            FileItem selectedItem = FileListView.SelectedItem as FileItem;
            if (selectedItem != null && previousSelectedItem != null)
            {
                if (selectedItem == previousSelectedItem)
                {
                    IsCopyEnabled = false;
                    IsPasteEnabled = true;
                    IsUndoCopyEnabled = true;
                    IsRedoCopyEnabled = true;
                }
                else
                {
                    Console.WriteLine($"Previous selected item path: {previousSelectedItem.Path}");
                    selectedFiles.Add(selectedItem);
                    previousSelectedItem = selectedItem;
                    IsCopyEnabled = true;
                    IsPasteEnabled = true;
                    IsUndoCopyEnabled = false;
                    IsRedoCopyEnabled = false;
                }
            }
            //else if (selectedItem != null)
            //{
            //    selectedFiles.Add(selectedItem);
            //    previousSelectedItem = selectedItem;
            //    IsCopyEnabled = true;
            //    IsRenameEnabled = true;
            //    IsDeleteEnabled = true;
            //    IsCutEnabled = true;
            //}
            selectedFiles.Clear();
            UpdateEnabledStateForSelectedItems(selectedItem);

        }


        public void UpdateEnabledStateForSelectedItems(FileItem selectedItem)
        {
            if (selectedItem != null)
            {
                selectedFiles.Add(selectedItem);
                previousSelectedItem = selectedItem;
                IsCopyEnabled = true;
                IsRenameEnabled = true;
                IsDeleteEnabled = true;
                IsCutEnabled = true;
                IsPasteEnabled = true;
            }
        }



        private bool isProcessingDrop = false;

        private async void FileListView_Drop(object sender, DragEventArgs e)
        {
            if (isProcessingDrop) return;  // Prevent re-entry
            isProcessingDrop = true;

            try
            {
                string archiveFolderPath = "D:\\Folder Structure Creator\\Archive"; // Update this to the actual archive folder path

                if (e.Data.GetDataPresent("FileItem"))
                {
                    FileItem fileItem = e.Data.GetData("FileItem") as FileItem;
                    ObservableCollection<FileItem> sourceCollection = e.Data.GetData("SourceCollection") as ObservableCollection<FileItem>;

                    if (fileItem != null && sourceCollection != null)
                    {
                        ListView listView = sender as ListView;
                        ListViewItem targetItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                        FileItem targetFileItem = targetItem?.Content as FileItem;

                        if (targetFileItem != null)
                        {
                            // If dropping onto a specific target item, move the fileItem to its path
                            string targetPath = targetFileItem.Path.Trim();
                            string destinationFullPath = Path.Combine(targetPath, fileItem.Name.Trim());
                            if (targetPath.Equals(archiveFolderPath, StringComparison.OrdinalIgnoreCase))
                            {
                                MessageBox.Show("You cannot drop items into the Archive folder.");
                                return;
                            }

                            if (Directory.Exists(destinationFullPath) || File.Exists(destinationFullPath))
                            {
                                // Prompt user for action: Replace, Skip, or Compare
                                var result = await ShowReplaceSkipCompareDialogAsync(fileItem.Name);
                                if (result == "Replace")
                                {
                                    // Delete the existing file or directory at the destination
                                    if (Directory.Exists(destinationFullPath))
                                    {
                                        Directory.Delete(destinationFullPath, true);
                                    }
                                    else if (File.Exists(destinationFullPath))
                                    {
                                        File.Delete(destinationFullPath);
                                    }
                                    // await HandleReplaceAction(fileItem.Path, destinationFullPath);
                                }
                                else if (result == "Skip")
                                {
                                    return;
                                }
                                else if (result == "Compare")
                                {
                                    CompareFiles(fileItem.Path, destinationFullPath);
                                   
                                }
                            }

                            try
                            {
                                await Task.Run(() =>
                                {
                                    if (Directory.Exists(fileItem.Path))
                                    {
                                        Directory.Move(fileItem.Path, destinationFullPath);
                                    }
                                    else if (File.Exists(fileItem.Path))
                                    {
                                        // File.Move(fileItem.Path, destinationFullPath);
                                        MoveAndRenameFile(fileItem.Path, destinationFullPath);
                                    }
                                });

                                // Remove the item from its original location
                                sourceCollection.Remove(fileItem);

                                // Refresh the UI to show changes
                                LoadFiles(targetPath); // Load items in the target folder
                                LoadFiles(CurrentDirectory); // Load items in the current directory to reflect removal
                            }
                            catch (Exception ex)
                            {
                                await HandleExceptionAsync(ex, fileItem.Name);
                            }
                        }
                        else
                        {
                            // If dropping into the root of the ListView, add the fileItem to the root collection
                            ObservableCollection<FileItem> itemsSource = listView.ItemsSource as ObservableCollection<FileItem>;
                            if (itemsSource != null)
                            {
                                itemsSource.Add(fileItem);
                            }

                            // Remove the item from its original location
                            sourceCollection.Remove(fileItem);

                            // Refresh the UI to show changes
                            LoadFiles(CurrentDirectory);
                        }
                      //  Mouse.OverrideCursor = null;
                    }
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, "general");
            }
            finally
            {
                isProcessingDrop = false;
            }
        }
        private void MoveAndRenameFile(string sourcePath, string destinationPath)
        {
            string destinationDirectory = Path.GetDirectoryName(destinationPath);
            string destinationFileName = Path.GetFileNameWithoutExtension(destinationPath);
            string destinationExtension = Path.GetExtension(destinationPath);

            string newDestinationPath = destinationPath;
            int counter = 1;

            while (File.Exists(newDestinationPath))
            {
                string newFileName = $"{destinationFileName}({counter}){destinationExtension}";
                newDestinationPath = Path.Combine(destinationDirectory, newFileName);
                counter++;
            }

            File.Move(sourcePath, newDestinationPath);
        }

        private Task<string> ShowReplaceSkipCompareDialogAsync(string itemName)
        {
            var tcs = new TaskCompletionSource<string>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var customMessageBox = new customMessageBox(
            $"An item with the name '{itemName}' already exists at the destination. Do you want to replace it, skip it, or compare the items?");
                if (customMessageBox.ShowDialog() == true)
                {
                    switch (customMessageBox.Result)
                    {
                        case customMessageBox.CustomMessageBoxResult.Replace:
                            tcs.SetResult("Replace");
                            break;
                        case customMessageBox.CustomMessageBoxResult.Skip:
                            tcs.SetResult("Skip");
                            break;
                        case customMessageBox.CustomMessageBoxResult.Compare:
                            tcs.SetResult("Compare");
                            break;
                        default:
                            tcs.SetResult("Skip");
                            break;
                    }
                }
                else
                {
                    tcs.SetResult("Skip");
                }
            });

            return tcs.Task;
        }


        private async Task HandleExceptionAsync(Exception ex, string itemName)
        {
            if (ex is UnauthorizedAccessException)
            {
                await ShowMessageBoxAsync($"Access denied for {itemName}: {ex.Message}");
            }
            else if (ex is IOException)
            {
                await ShowMessageBoxAsync($"File I/O error for {itemName}: {ex.Message}");
            }
            else
            {
                await ShowMessageBoxAsync($"Error with {itemName}: {ex.Message}");
            }
        }

        private Task ShowMessageBoxAsync(string message)
        {
            return Task.Run(() => MessageBox.Show(message));
        }


        private async Task MoveFileOrDirectoryAsync(FileItem fileItem, string destinationFullPath, string destinationPath)
        {
            const int maxRetries = 3;
            const int delayBetweenRetries = 500; // in milliseconds

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        if (Directory.Exists(fileItem.Path))
                        {
                            Directory.Move(fileItem.Path, destinationFullPath);
                        }
                        else if (File.Exists(fileItem.Path))
                        {
                            File.Move(fileItem.Path, destinationFullPath);
                        }
                    });

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        LoadFiles(destinationPath);
                        LoadFiles(CurrentDirectory);
                        MessageBox.Show($"Item {fileItem.Name} moved to {destinationPath}");
                        return;
                    });

                    break;
                }
                catch (IOException ex) when (ex.HResult == -2147024864) // ERROR_SHARING_VIOLATION
                {
                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay(delayBetweenRetries);
                    }
                    else
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show($"File I/O error for {fileItem.Name}: {ex.Message}");
                        });
                        throw;
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Access denied for {fileItem.Name}: {ex.Message}");
                    });
                    break;
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Error moving item {fileItem.Name}: {ex.Message}");
                    });
                    break;
                }
            }
        }




        RoutedEventArgs eventArgs = new RoutedEventArgs();

        private bool IsArchiveFolder1(string path)
        {
            return path.Split(Path.DirectorySeparatorChar).Contains(archiveFolderName);
        }

        private readonly string archiveFolderName = "Archive"; // Name of the archive folder

       


  
        private void RefreshUI(FileItem targetFileItem, string destinationPath)
        {
            RefreshButton_Click(RefreshButton, new RoutedEventArgs());
            RefreshFileItem(destinationPath);
            LoadFiles(targetFileItem.Path);
        }
        private void MoveAndDeleteItem(FileItem fileItem, string destinationPath)
        {
            string originalPath = fileItem.Path;

            try
            {
                // Step 1: Move the item back to the original location
                MoveItemBack(fileItem, destinationPath);

                // Step 2: Delete the item from the TreeView at the destination path
                DeleteItemFromTreeView(fileItem, destinationPath);

                // Step 3: Refresh the UI
                RefreshTreeView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while moving and deleting the item: {ex.Message}");
            }
        }

        private void MoveItemBack(FileItem fileItem, string destinationPath)
        {
            string originalPath = fileItem.Path;

            try
            {
                if (Directory.Exists(destinationPath))
                {
                    Directory.Move(destinationPath, originalPath);
                    Console.WriteLine($"Moved directory back from {destinationPath} to {originalPath}");
                }
                else if (File.Exists(destinationPath))
                {
                    File.Move(destinationPath, originalPath);
                    Console.WriteLine($"Moved file back from {destinationPath} to {originalPath}");
                }
                else
                {
                    MessageBox.Show("Destination path does not exist, cannot revert move.");
                    return;
                }

                // Update fileItem's path to reflect the move
                fileItem.Path = originalPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error moving item back: {ex.Message}");
            }
        }

        private void DeleteItemFromTreeView(FileItem fileItem, string destinationPath)
        {
            try
            {
                // Locate the TreeView item associated with the destination path
                TreeViewItem targetItem = FindTreeViewItemByPath(destinationPath);
                if (targetItem != null)
                {
                    // Remove the item from the TreeView
                    TreeViewItem parentItem = targetItem.Parent as TreeViewItem;
                    if (parentItem != null)
                    {
                        parentItem.Items.Remove(targetItem);
                    }
                    else
                    {
                        // If there is no parent, it's a root item
                        DirectoryTree.Items.Remove(targetItem);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting item from TreeView: {ex.Message}");
            }
        }

        private void RefreshTreeView()
        {
            // Refresh your TreeView to reflect changes
            FileListView.Items.Refresh();
        }

        private TreeViewItem FindTreeViewItemByPath(string path)
        {
            // Implement a method to find a TreeViewItem by its path
            // This example assumes you have a way to iterate and match items in your TreeView
            foreach (TreeViewItem item in FileListView.Items)
            {
                if (item.Tag is FileItem fileItem && fileItem.Path == path)
                {
                    return item;
                }
            }
            return null;
        }



        private void DirectoryTree_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("FileItem"))
            {
                
                FileItem fileItem = e.Data.GetData("FileItem") as FileItem;
                if (fileItem != null)
                {   
                    TreeView treeView = sender as TreeView;
                    TreeViewItem targetItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                    if (targetItem != null)
                    {
                        FileItem targetFileItem = targetItem.Header as FileItem;
                        if (targetFileItem != null)
                        {
                            string destinationPath = Path.Combine(targetFileItem.Path, fileItem.Name);

                            if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                            {
                               MessageBox.Show("A file or directory with the same name already exists in the target location.");
                                return;
                            }

                            bool moved = false;

                            try
                            {
                                if (Directory.Exists(fileItem.Path))
                                {
                                    // Move directory
                                    Directory.Move(fileItem.Path, destinationPath);
                                    moved = true;
                                    targetFileItem.SubItems.Add(fileItem);
                                }
                                else
                                {
                                    // Move file
                                    File.Move(fileItem.Path, destinationPath);
                                    moved = true;
                                }

                                if (IsArchiveFolder1(destinationPath))
                                {
                                    // If destination is an archive folder, delete the destination item
                                    //  DeleteItemAtPath(destinationPath);
                                    MessageBox.Show("Items cannot be moved into the archive folder.");
                                    e.Handled = true;

                                    // Revert move: Move the item back to its original location
                                    if (moved)
                                    {
                                        MoveItemBack(fileItem, destinationPath);
                                        RefreshUI(targetFileItem, destinationPath);
                                    }
                                    return;
                                }

                                // Refresh UI after successful move
                                RefreshUI(targetFileItem, destinationPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error moving item: {ex.Message}");
                            }
                        }
                    }
                }
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
                        //File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);

                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = /*"notepad.exe"*/filePath,
                            //Arguments = filePath,
                            UseShellExecute = true
                        };

                        ////// Attach event handler to remove read-only attribute after closing Notepad
                        //process.Exited += (sender, e) =>
                        //{
                        //    // Remove the read-only attribute from the file
                        //    Dispatcher.Invoke(() =>
                        //    {
                        //        FileAttributes attributes = File.GetAttributes(filePath);
                        //        attributes &= ~FileAttributes.ReadOnly;
                        //        File.SetAttributes(filePath, attributes);
                        //    });
                        //};
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



        private string tempFolderPath = Path.Combine(Path.GetTempPath(), "CutPasteTemp");
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {

                if (FileListView.SelectedItem != null)
                {
                    // Assuming the FileItem has a property called FileName 
                    string fileName = ((FileItem)FileListView.SelectedItem).Name;
                    // Adjust this line to access the file name property in your FileItem class
                    // Check if the file has a .zip extension
                    if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show("Cannot copy zip files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                CopyWithoutDirectory(sender, e);
                IsCopyEnabled = false;
                IsPasteEnabled = true;

            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.V)
            {
                PasteWithoutDirectory(sender, e);
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.X)
            {

                CutWithoutDirectory(sender, e);
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Z)
            {
                UndoCopy_Click(sender, e);
                updateUndoRedo();
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Y)
            {
                RedoCopy_Click(sender, e);
                updateUndoRedo();
            }

        }
        private Stack<(string sourcePath, string destinationPath)> copyHistory = new Stack<(string sourcePath, string destinationPath)>();
        private Stack<(string sourcePath, string destinationPath)> redoHistory = new Stack<(string sourcePath, string destinationPath)>();

        private void UndoCopy_Click(object sender, RoutedEventArgs e)
        {

            if (copyHistory.Count > 0)
            {
                var lastCopy = copyHistory.Pop();
                redoHistory.Push(lastCopy);
                try
                {
                    if (Directory.Exists(lastCopy.destinationPath))
                    {
                        Directory.Delete(lastCopy.destinationPath, true);
                    }
                    else if (File.Exists(lastCopy.destinationPath))
                    {
                        File.Delete(lastCopy.destinationPath);
                    }

                    LoadFiles(PathBox.Text);
                    updateUndoRedo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error undoing copy operation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No copy operations to undo.", "Undo Copy", MessageBoxButton.OK, MessageBoxImage.Information);
            }


        }


        public void updateUndoRedo()
        {
            try
            {
                IsUndoCopyEnabled = copyHistory.Count > 0;
                IsRedoCopyEnabled = redoHistory.Count > 0;
            }
            catch (Exception ex)
            {

            }
        }
        private void RedoCopy_Click(object sender, RoutedEventArgs e)
        {
        
            if (redoHistory.Count > 0)
            {
                var lastRedo = redoHistory.Pop();
                copyHistory.Push(lastRedo);

                try
                {
                    if (Directory.Exists(lastRedo.sourcePath))
                    {
                        CopyDirectory(lastRedo.sourcePath, lastRedo.destinationPath);
                    }
                    else if (File.Exists(lastRedo.sourcePath))
                    {
                        File.Copy(lastRedo.sourcePath, lastRedo.destinationPath);
                    }

                    LoadFiles(PathBox.Text);
                    updateUndoRedo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error redoing copy operation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("No copy operations to redo.", "Redo Copy", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CopyWithoutDirectory(object sender, RoutedEventArgs e)
        {

            if (FileListView.SelectedItems.Count > 0)
            {
                selectedItemsPaths.Clear();
                foreach (var item in FileListView.SelectedItems)
                {
                    if (item is FileItem selectedFileItem)
                    {
                        string extension = Path.GetExtension(selectedFileItem.Path);
                        if (extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) || IsArchiveFolder(selectedFileItem.Path))
                        {
                            MessageBox.Show("Cannot copy zip files or archive folders.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else if (IsArchiveFolder(selectedFileItem.Path))
                        {
                            MessageBox.Show("Cannot copy archive folders.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        else
                        {
                            selectedItemsPaths.Add(selectedFileItem.Path);
                        }
                    }
                }
               
                isCutOperation = false;
                //IsPasteEnabled = true;
                //IsUndoCopyEnabled = true;  // Enable undo after copy
                //IsRedoCopyEnabled = false; // Disable redo after copy
            }
        }

        private void CutWithoutDirectory(object sender, RoutedEventArgs e)
        {
 
            if (FileListView.SelectedItems.Count > 0)
            {
                selectedItemsPaths.Clear();
                foreach (var item in FileListView.SelectedItems)
                {
                    if (item is FileItem selectedFileItem)
                    {
                        selectedItemsPaths.Add(selectedFileItem.Path);
                    }
                }

                Directory.CreateDirectory(tempFolderPath);
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
                IsPasteEnabled = true;
                IsCutEnabled = false;
                IsCopyEnabled = false;
                //IsUndoCopyEnabled = true;  // Enable undo after cut
                //IsRedoCopyEnabled = false; // Disable redo after cut
            }
        }

        private bool IsArchiveFolder(string folderPath)
        {
            // Add logic here to check if the folder contains archive files
            // For example, you can check if it contains files with extensions like .zip, .rar, etc.
            // Here's a basic example assuming all zip files are considered archive folders:
            return Directory.GetFiles(folderPath, "*.zip").Length > 0;
        }

        private void PasteWithoutDirectory(object sender, RoutedEventArgs e)
        {

            /*            string destinationFolderPath;
                        var selectedDestinationFolder = FileListView.SelectedItem as FileItem;

                        if (selectedDestinationFolder != null && Directory.Exists(selectedDestinationFolder.Path))
                        {
                            destinationFolderPath = selectedDestinationFolder.Path;
                        }
                        else
                        {
                            destinationFolderPath = PathBox.Text;
                            if (!Directory.Exists(destinationFolderPath))
                            {
                                MessageBox.Show("Please select a valid destination folder or specify a valid directory path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        bool pasteInOpenDirectory = PathBox.Text.Equals(destinationFolderPath, StringComparison.OrdinalIgnoreCase);
                        List<string> successfulMoves = new List<string>();

                        foreach (var itemPath in selectedItemsPaths)
                        {
                            string destinationPath;
                            if (pasteInOpenDirectory)
                            {
                                string fileName = Path.GetFileName(itemPath);
                                destinationPath = Path.Combine(destinationFolderPath, fileName);
                                string baseName = Path.GetFileNameWithoutExtension(fileName);
                                string extension = Path.GetExtension(fileName);
                                string newName = baseName;
                                int copyNumber = 1;

                                while (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                                {
                                    newName = $"{baseName}-Copy{(copyNumber > 1 ? $"({copyNumber})" : "")}{extension}";
                                    destinationPath = Path.Combine(destinationFolderPath, newName);
                                    copyNumber++;
                                }
                            }
                            else
                            {
                                string parentDirectory = Path.GetDirectoryName(destinationFolderPath);
                                destinationPath = Path.Combine(parentDirectory, Path.GetFileName(itemPath));
                                string baseName = Path.GetFileNameWithoutExtension(destinationPath);
                                string extension = Path.GetExtension(destinationPath);
                                string newName = baseName;
                                int copyNumber = 1;

                                while (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                                {
                                    newName = $"{baseName}-Copy{(copyNumber > 1 ? $"({copyNumber})" : "")}{extension}";
                                    destinationPath = Path.Combine(parentDirectory, newName);
                                    copyNumber++;
                                }
                            }

                            try
                            {
                                if (File.Exists(itemPath))
                                {
                                    File.Copy(itemPath, destinationPath);
                                    copyHistory.Push((itemPath, destinationPath));  // Track the copy operation
                                    if (isCutOperation)
                                    {
                                        successfulMoves.Add(itemPath);
                                    }
                                }
                                else if (Directory.Exists(itemPath))
                                {
                                    CopyDirectory(itemPath, destinationPath);
                                    copyHistory.Push((itemPath, destinationPath));  // Track the copy operation
                                    if (isCutOperation)
                                    {
                                        successfulMoves.Add(itemPath);
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

                        selectedItemsPaths.Clear();
                        LoadFiles(PathBox.Text);*/
            string destinationFolderPath;
            var selectedDestinationFolder = FileListView.SelectedItem as FileItem;

            if (selectedDestinationFolder != null && Directory.Exists(selectedDestinationFolder.Path))
            {
                // Prevent pasting into a folder named 'archive'
                if (Path.GetFileName(selectedDestinationFolder.Path).Equals("archive", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Pasting into a folder named 'archive' is not allowed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // If the selected folder is opened, paste inside it
                if (PathBox.Text.Equals(selectedDestinationFolder.Path, StringComparison.OrdinalIgnoreCase))
                {
                    destinationFolderPath = selectedDestinationFolder.Path;
                }
                else
                {
                    // Otherwise, paste in the currently open directory
                    destinationFolderPath = PathBox.Text;
                }
            }
            else
            {
                destinationFolderPath = PathBox.Text;
            }

            if (!Directory.Exists(destinationFolderPath))
            {
                MessageBox.Show("Please select a valid destination folder or specify a valid directory path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Prevent pasting into a folder named 'archive'
            if (Path.GetFileName(destinationFolderPath).Equals("archive", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Pasting into a folder named 'archive' is not allowed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<string> successfulMoves = new List<string>();

            foreach (var itemPath in selectedItemsPaths)
            {
                string destinationPath = Path.Combine(destinationFolderPath, Path.GetFileName(itemPath));

                // Extract the base name and extension
                string baseName = Path.GetFileNameWithoutExtension(destinationPath);
                string extension = Path.GetExtension(destinationPath);

                int copyNumber = 0;
                string newName = $"{baseName}{extension}";

                while (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                {
                    copyNumber++;
                    newName = copyNumber == 1
                        ? $"{baseName}{extension}-Copy"
                        : $"{baseName}{extension}-Copy({copyNumber})";
                    destinationPath = Path.Combine(destinationFolderPath, newName);
                }

                try
                {
                    if (File.Exists(itemPath))
                    {
                        File.Copy(itemPath, destinationPath);
                        copyHistory.Push((itemPath, destinationPath));  // Track the copy operation
                        if (isCutOperation)
                        {
                            successfulMoves.Add(itemPath);
                            File.Delete(itemPath);
                        }
                    }
                    else if (Directory.Exists(itemPath))
                    {
                        CopyDirectory(itemPath, destinationPath);
                        copyHistory.Push((itemPath, destinationPath));  // Track the copy operation
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

            // Refresh the file list view to show changes in the current open directory
            LoadFiles(PathBox.Text);

        }




        private void CopyButton_Click(object sender, RoutedEventArgs e)
        //{
        //    FileItem selectedItem = FileListView.SelectedItem as FileItem;
        //    if (selectedItem != null && selectedItem == previousSelectedItem)
            {

                IsCopyEnabled = false;
                IsUndoCopyEnabled = true;
                IsRedoCopyEnabled = true;
                IsPasteEnabled = true;

                CopyWithoutDirectory(sender, e);

            }
            //if (selectedItem != null && selectedItem == previousSelectedItem)
            //{
            //    IsCopyEnabled = true;
            //    IsUndoCopyEnabled = true;
            //    IsRedoCopyEnabled = true;
            //    IsPasteEnabled = true;
            //    CopyWithoutDirectory(sender, e);

            //}
       // }

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
                                        var customMessageBox = new customMessageBox(
                                            $"The destination already has a file named \"{selectedItem.Name}\".\n\nWould you like to replace the existing file, skip this file, or compare the files?");
                                        if (customMessageBox.ShowDialog() == true)
                                        {
                                            if (customMessageBox.Result == customMessageBox.CustomMessageBoxResult.Replace)
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
                                            else if (customMessageBox.Result == customMessageBox.CustomMessageBoxResult.Compare)
                                            {
                                                // Call the compare files method
                                                CompareFiles(selectedItem.Path, destinationFullPath);

                                                // After comparison, decide whether to move the file based on your criteria
                                                // For example, if user decides to keep source file, you can set itemsMoved to true
                                            }
                                            // else if (result == CustomMessageBoxResult.Skip) // No action needed for skipping
                                        }
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


                            if (itemsMoved || !filesAreDifferent)
                            {
                                // Show a success message only if items are moved
                                // MessageBox.Show($"Items moved to {destinationPath}");

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
        private bool filesAreDifferent = false; // Flag to track if files are different
        private void CompareFiles(string sourcePath, string destinationPath)
        {
            try
            {
                if (File.Exists(sourcePath) && File.Exists(destinationPath))
                {
                    // Both source and destination paths point to files
                    string sourceContent = File.ReadAllText(sourcePath);
                    string destinationContent = File.ReadAllText(destinationPath);
                    CompareAndShowResult(sourceContent, destinationContent);
                }
                else if (Directory.Exists(sourcePath) && Directory.Exists(destinationPath))
                {
                    // Both source and destination paths point to folders
                    CompareFolderContents(sourcePath, destinationPath);
                }
                else
                {
                    // Handle other cases, such as one path pointing to a file and the other to a folder
                    MessageBox.Show("Invalid paths specified. Please make sure both paths point to either files or folders.", "Invalid Paths", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (filesAreDifferent == false)
                {
                    // Prompt user to decide which file to keep
                    var compareDialog = new System.Windows.Forms.Form
                    {
                        Text = "File Compare",
                        Width = 500,
                        Height = 500
                    };

                    var label = new System.Windows.Forms.Label
                    {
                        Text = "Which files do you want to keep?",
                        Dock = System.Windows.Forms.DockStyle.Top,
                        TextAlign = (System.Drawing.ContentAlignment)System.Windows.Forms.HorizontalAlignment.Center // Fixed alignment
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

                    var continueButton = new System.Windows.Forms.Button
                    {
                        Text = "Continue",
                        Dock = System.Windows.Forms.DockStyle.Bottom
                    };
                    continueButton.Click += (sender, e) =>
                    {
                        if (sourceFileCheckBox.Checked && destinationFileCheckBox.Checked)
                        {
                            // Check if source and destination paths are directories
                            if (Directory.Exists(sourcePath) && Directory.Exists(destinationPath))
                            {
                                string[] sourceFiles = Directory.GetFiles(sourcePath);

                                foreach (string sourceFile in sourceFiles)
                                {
                                    string destinationFile = Path.Combine(destinationPath, Path.GetFileName(sourceFile));

                                    // Check if file with the same name exists in the destination directory
                                    if (File.Exists(destinationFile))
                                    {
                                        string destinationDir = Path.GetDirectoryName(destinationFile);
                                        string sourceFileName = Path.GetFileNameWithoutExtension(sourceFile);
                                        string sourceExtension = Path.GetExtension(sourceFile);

                                        string newSourceFileName = sourceFileName;
                                        string newSourcePath = destinationFile;

                                        for (int i = 1; ; i++)
                                        {
                                            string newName = $"{sourceFileName}({i}){sourceExtension}";
                                            newSourcePath = Path.Combine(destinationDir, newName);
                                            if (!File.Exists(newSourcePath))
                                            {
                                                File.Move(sourceFile, newSourcePath);
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Move the source file to the destination directory if no conflict
                                        File.Move(sourceFile, destinationFile);
                                    }
                                }

                                // Delete the source folder and its contents
                                Directory.Delete(sourcePath, true);
                            }
                            else
                            {
                             //   MessageBox.Show("Source or destination path is not a directory.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        /*
                                            continueButton.Click += (sender, e) =>
                                            {
                                                if (sourceFileCheckBox.Checked && destinationFileCheckBox.Checked)
                                                {
                                                    // Check if source path is a file and destination path is a directory
                                                    if (File.Exists(sourcePath) && Directory.Exists(destinationPath))
                                                    {
                                                        string sourceFileName = Path.GetFileName(sourcePath);
                                                        string destinationFile = Path.Combine(destinationPath, sourceFileName);

                                                        // Check if file with the same name exists in the destination directory
                                                        if (File.Exists(destinationFile))
                                                        {
                                                            string destinationDir = Path.GetDirectoryName(destinationFile);
                                                            string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFileName);
                                                            string sourceExtension = Path.GetExtension(sourceFileName);

                                                            string newSourcePath;

                                                            for (int i = 1; ; i++)
                                                            {
                                                                string newName = $"{sourceFileNameWithoutExtension}({i}){sourceExtension}";
                                                                newSourcePath = Path.Combine(destinationDir, newName);
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
                                                            File.Move(sourcePath, destinationFile);
                                                        }

                                                        // Delete the source file's parent directory if it's empty
                                                        string sourceDir = Path.GetDirectoryName(sourcePath);
                                                        if (Directory.GetFiles(sourceDir).Length == 0 && Directory.GetDirectories(sourceDir).Length == 0)
                                                        {
                                                            Directory.Delete(sourceDir);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show("Source path is not a file or destination path is not a directory.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                                                    }
                                                }*/

                        //oriiginal
                        /*                    continueButton.Click += (sender, e) =>
                                            {
                                                if (sourceFileCheckBox.Checked && destinationFileCheckBox.Checked)
                                                {
                                                    // Keep both files by renaming the source file if the destination file already exists
                                                    if (File.Exists(destinationPath))
                                                    {
                                                        string destinationDir = Path.GetDirectoryName(destinationPath);
                                                        string destinationFileName = Path.GetFileNameWithoutExtension(destinationPath);
                                                        string destinationExtension = Path.GetExtension(destinationPath);

                                                        string sourceFileName = Path.GetFileNameWithoutExtension(sourcePath);
                                                        string sourceExtension = Path.GetExtension(sourcePath);

                                                        string newSourceFileName = sourceFileName;
                                                        string newSourcePath = sourcePath;

                                                        for (int i = 1; ; i++)
                                                        {
                                                            string newName = $"{sourceFileName}({i}){sourceExtension}";
                                                            newSourcePath = Path.Combine(destinationDir, newName);
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
                                                        if (File.Exists(sourcePath))
                                                        {
                                                            File.Move(sourcePath, newSourcePath);
                                                        }
                                                        else
                                                        {
                                                            MessageBox.Show($"Source file '{sourcePath}' not found.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                                                        }
                                                    }
                                                }*/

                        else if (sourceFileCheckBox.Checked)
                        {
                            if (Directory.Exists(destinationPath) || File.Exists(destinationPath))
                            {
                                // Handle existing file or directory, e.g., delete or rename
                                if (Directory.Exists(destinationPath))
                                {
                                    Directory.Delete(destinationPath, true); // Careful: This deletes the directory and its contents
                                }
                            
                                else
                                {
                                    File.Delete(destinationPath); // Delete the existing file
                                }
                            }

                            // Now perform the move operation
                            Directory.Move(sourcePath, destinationPath);

                        }

                        else if (destinationFileCheckBox.Checked)
                        {
                            // Keep only the destination file or directory
                            if (Directory.Exists(destinationPath))
                            {
                                // Delete source directory
                                Directory.Delete(sourcePath, true); // Delete recursively
                            }
                            else
                            {
                                // Delete source file
                                File.Delete(sourcePath);
                            }
                        }


                        compareDialog.Close();

                    };

                    compareDialog.Controls.Add(label);
                    compareDialog.Controls.Add(sourceFileCheckBox);
                    compareDialog.Controls.Add(destinationFileCheckBox);
                    compareDialog.Controls.Add(continueButton);
                    compareDialog.ShowDialog();
                }
            }

            catch (Exception ex)
            {
                // MessageBox.Show($"Error comparing files: {ex.Message}");
            }

        }
        private void CompareFolderContents(string sourceFolderPath, string destinationFolderPath)
        {
            // Get the list of files in both folders
            string[] sourceFiles = Directory.GetFiles(sourceFolderPath);
            string[] destinationFiles = Directory.GetFiles(destinationFolderPath);

            // Compare the contents of corresponding files in both folders
            foreach (string sourceFile in sourceFiles)
            {
                string fileName = Path.GetFileName(sourceFile);
                string correspondingDestinationFile = Path.Combine(destinationFolderPath, fileName);

                if (Array.IndexOf(destinationFiles, correspondingDestinationFile) >= 0)
                {
                    // Corresponding file exists in the destination folder, so compare their contents
                    string sourceContent = File.ReadAllText(sourceFile);
                    string destinationContent = File.ReadAllText(correspondingDestinationFile);
                    CompareAndShowResult(sourceContent, destinationContent);
                }
            }
        }

        private void CompareAndShowResult(string sourceContent, string destinationContent)
        {
            if (sourceContent == destinationContent)
            {
                MessageBox.Show("The files are identical.", "File Comparison", MessageBoxButton.OK, MessageBoxImage.Information);
                filesAreDifferent = false;
            }
            else
            {
                MessageBox.Show("The files are different.", "File Comparison", MessageBoxButton.OK, MessageBoxImage.Information);
                filesAreDifferent = true; // Set the flag to true if files are different
            }
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

                    // Check if the destination path contains "Archive"
                    if (destinationPath.IndexOf("Archive", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        MessageBox.Show("Items cannot be moved to an Archive folder. Please select a different location.",
                                        "Invalid Destination",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Error);
                        return;
                    }

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

            IsCopyEnabled = false;
            IsUndoCopyEnabled = true;
            IsRedoCopyEnabled = true;
            IsCutEnabled = true;
            PasteWithoutDirectory(sender, e);


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
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string destDirectory = Path.Combine(destinationDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDirectory);
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
                                FinalizeRename(selectedItem.Path, newFileName, selectedItem, textBox, textBlock, selectedItem.Name);
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
                            FinalizeRename(selectedItem.Path, newFileName, selectedItem, textBox, textBlock, selectedItem.Name);
                        };
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a file or folder to rename.");
            }
        }

        private void FinalizeRename(string oldPath, string newName, FileItem selectedItem, TextBox textBox, TextBlock textBlock, string oldName)
        {
            string directory = Path.GetDirectoryName(oldPath);
            string newPath = Path.Combine(directory, newName);

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("File name cannot be empty.");
                RetryRename(textBox, textBlock, oldName);
                return;
            }

            try
            {
                // Check if another file or directory with the same name already exists in the directory
                bool nameExists = Directory.EnumerateFileSystemEntries(directory)
                    .Any(entry => string.Equals(Path.GetFileName(entry), newName, StringComparison.OrdinalIgnoreCase) && !string.Equals(entry, oldPath, StringComparison.OrdinalIgnoreCase));

                if (nameExists)
                {
                    string newFileName = GetUniqueFileName(directory, newName);
                    var result = MessageBox.Show($"A file or directory named '{newName}' already exists. Do you want to rename it to '{newFileName}'?", "Rename File", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        newPath = Path.Combine(directory, newFileName);
                        newName = newFileName;
                    }
                    else
                    {
                        // Reset to original name if the user selects "No"
                        RetryRename(textBox, textBlock, oldName);
                        return;
                    }
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
                    RetryRename(textBox, textBlock, oldName);
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
                MessageBox.Show($"Rename failed: {ex.Message}");
                RetryRename(textBox, textBlock, oldName);
            }
        }

        private string GetUniqueFileName(string directory, string baseFileName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
            string fileExtension = Path.GetExtension(baseFileName);

            int count = 1;
            string newFileName;
            string newFilePath;

            do
            {
                newFileName = $"{fileNameWithoutExtension}({count}){fileExtension}";
                newFilePath = Path.Combine(directory, newFileName);
                count++;
            } while (File.Exists(newFilePath) || Directory.Exists(newFilePath));

            return newFileName;
        }

        private void RetryRename(TextBox textBox, TextBlock textBlock, string originalName)
        {
            textBox.Visibility = Visibility.Collapsed;
            textBlock.Visibility = Visibility.Visible;
            textBlock.Text = originalName;
            FileListView.Items.Refresh();

            // Explicitly reset the ItemsSource to refresh the ListView
            //var itemsSource = FileListView.ItemsSource;
            //FileListView.ItemsSource = null;
            //FileListView.ItemsSource = itemsSource;
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
            // Explicitly reset the ItemsSource to refresh the ListView
            //var itemsSource = FileListView.ItemsSource;
            //FileListView.ItemsSource = null;
            //FileListView.ItemsSource = itemsSource;
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
                    FinalizeRename(fileItem.Path, newFileName, fileItem, textBox, textBlock, fileItem.Name);

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
            ClearButton.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Collapsed : Visibility.Visible;
             SearchIcon.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;

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
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            ClearButton.Visibility = Visibility.Collapsed;
            SearchPlaceholder.Visibility = Visibility.Visible;
            NoResultsTextBlock.Visibility = Visibility.Collapsed;
            FileListView.ItemsSource = allFiles; // Reset to show all files
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
      

       /* private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            ClearButton.Visibility = Visibility.Collapsed;
        }
*/
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
        private void DirectoryTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

      
        private void FileListView_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                ListView listView = sender as ListView;
                ListViewItem item = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

                if (item != null)
                {
                    FileItem fileItem = item.Content as FileItem;
                    if (fileItem != null)
                    {
                        ObservableCollection<FileItem> itemsSource = listView.ItemsSource as ObservableCollection<FileItem>;
                        DataObject dragData = new DataObject("FileItem", fileItem);
                        dragData.SetData("SourceCollection", itemsSource);

                        string folderName = Path.GetFileName(fileItem.Path.TrimEnd(Path.DirectorySeparatorChar));

                        // Paths to your icons
                        string folderIconPath = "C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\folder (2).png"; // Adjust path to your folder icon
                        string fileIconPath = "C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\txts.png"; // Adjust path to your file icon

                        // Determine if the item is a folder or a file
                        bool isFolder = Directory.Exists(fileItem.Path);

                        dragAdorner = new DragAdorner(listView, $"Move to {folderName}", folderIconPath, fileIconPath, isFolder);

                        if (adornerLayer == null)
                        {
                            adornerLayer = AdornerLayer.GetAdornerLayer(listView);
                        }

                        adornerLayer.Add(dragAdorner);
                        DragDrop.DoDragDrop(item, dragData, DragDropEffects.Move);

                        // Remove the adorner after the drag operation
                        adornerLayer.Remove(dragAdorner);
                        dragAdorner = null;
                    }
                }
            }
        }
      
        private void DirectoryTree_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                TreeView treeView = sender as TreeView;
                TreeViewItem item = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (item != null)
                {
                    FileItem fileItem = item.Header as FileItem;
                    if (fileItem != null)
                    {
                        DataObject dragData = new DataObject("FileItem", fileItem);

                        string folderName = Path.GetFileName(fileItem.Path.TrimEnd(Path.DirectorySeparatorChar));

                        // Paths to your icons
                        string folderIconPath = "C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\folder (2).png"; // Adjust path to your folder icon
                        string fileIconPath = "C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\txts.png"; // Adjust path to your file icon

                        // Determine if the item is a folder or a file
                        bool isFolder = Directory.Exists(fileItem.Path);

                        dragAdorner = new DragAdorner(treeView, $"Move to {folderName}", folderIconPath, fileIconPath, isFolder);

                        if (adornerLayer == null)
                        {
                            adornerLayer = AdornerLayer.GetAdornerLayer(treeView);
                        }

                        adornerLayer.Add(dragAdorner);
                        DragDrop.DoDragDrop(item, dragData, DragDropEffects.Move);

                        // Remove the adorner after the drag operation
                        adornerLayer.Remove(dragAdorner);
                        dragAdorner = null;
                    }
                }
            }
        }
        private void FileListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent("FileItem"))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
           /* Uri cursorUri = new Uri("pack://application:,,,/Resources/file.cur");
            Cursor customCursor = new Cursor(Application.GetResourceStream(cursorUri).Stream);
            this.Cursor = customCursor;*/

        }
        private Dictionary<string, Cursor> customCursors = new Dictionary<string, Cursor>(); // Dictionary to store custom cursors
        private Label cursorLabel = new Label(); // Label to display item name as cursor label
        private Image cursorImage = new Image(); // Image to display as custom cursor


        private void LoadCustomCursors()
        {
            // Load custom cursors from resources or other sources
            foreach (var item in FileListView.Items)
            {
                string itemName = (string)item; // Replace with actual property to get item name
                Uri cursorUri = new Uri($"pack://application:,,,/Resources/{itemName}.cur"); // Adjust URI format as per your resource structure
                Cursor customCursor = new Cursor(Application.GetResourceStream(cursorUri).Stream);
                customCursors[itemName] = customCursor;
            }
        }

    
        private void FileListView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent("FileItem"))
            {
                ListView listView = sender as ListView;
                ListViewItem targetItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                FileItem targetFileItem = targetItem?.Content as FileItem;

                FileItem draggedFileItem = e.Data.GetData("FileItem") as FileItem;

                if (targetFileItem != null && draggedFileItem != null && dragAdorner != null)
                {
                    if (targetFileItem.Path == draggedFileItem.Path)
                    {
                        e.Effects = DragDropEffects.None; // Prevent drop on the same item
                    }
                    else
                    {
                        e.Effects = DragDropEffects.Move;
                        string folderName = Path.GetFileName(targetFileItem.Path.TrimEnd(Path.DirectorySeparatorChar));
                        dragAdorner.UpdateText($"Move to {folderName}");
                        Point cursorPosition = e.GetPosition(listView);
                        dragAdorner.UpdatePosition(cursorPosition);
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void DirectoryTree_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent("FileItem"))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }


       private void DirectoryTree_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else if (e.Data.GetDataPresent("FileItem"))
            {
                TreeView treeView = sender as TreeView;
                TreeViewItem targetItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                FileItem targetFileItem = targetItem?.Header as FileItem;

                FileItem draggedFileItem = e.Data.GetData("FileItem") as FileItem;

                if (targetFileItem != null && draggedFileItem != null && dragAdorner != null)
                {
                    if (targetFileItem.Path == draggedFileItem.Path)
                    {
                        e.Effects = DragDropEffects.None; // Prevent drop on the same item
                    }
                    else
                    {
                        e.Effects = DragDropEffects.Move;
                        string folderName = Path.GetFileName(targetFileItem.Path.TrimEnd(Path.DirectorySeparatorChar));
                        dragAdorner.UpdateText($"Move to {folderName}");
                        Point cursorPosition = e.GetPosition(treeView);
                        dragAdorner.UpdatePosition(cursorPosition);
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void LargeIconsButton_Click(object sender, RoutedEventArgs e)
        {
            //FileListView.View = (ViewBase)FindResource("LargeIconsView");
            //FileListView.ItemContainerStyle = (Style)FindResource("LargeIconStyle");
            // Set the ListView to display in LargeIconsView mode
            FileListView.View = (ViewBase)FindResource("LargeIconsView");
            FileListView.ItemContainerStyle = (Style)FindResource("LargeIconStyle");
            FileListView.ItemsPanel = (ItemsPanelTemplate)FindResource("ItemsPanelTemplateLargeIcons");

            // Optionally, adjust other ListView properties as needed
            FileListView.SelectionMode = SelectionMode.Single; // Example

            // Refresh the ListView to apply changes
            FileListView.Items.Refresh();
        }

        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            // Switch back to the detailed view (default GridView)
            FileListView.View = (ViewBase)FindResource("DetailedView");
            FileListView.ClearValue(ListView.ItemContainerStyleProperty);
        }

        private void RootFolder_Click(object sender, RoutedEventArgs e)
        {
            LoadFiles("D:\\Folder Structure Creator");
        }
      private void  ThemeToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            //isDarkMode = !isDarkMode;
            //ApplyTheme();

        }
        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;
            bool isDarkTheme = (this.Background == Brushes.Black);

            if (isDarkTheme)
            {
                // Switch to light theme
                SetThemeColors(this, Brushes.White, Brushes.Black);
                ApplyGridViewColumnHeaderStyle("LightGridViewColumnHeaderStyle");

                SetImageSource("pack://application:,,,/Resources/sun.png");
            }
            else
            {
                // Switch to dark theme
                SetThemeColors(this, Brushes.Black, Brushes.White);
                ApplyGridViewColumnHeaderStyle("DarkGridViewColumnHeaderStyle");

                SetImageSource("pack://application:,,,/Resources/moon (2).png");
            }
        }
        private void SetThemeColors(DependencyObject parent, Brush background, Brush foreground)
        {

            // Iterate through all children of the parent control
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is Control control)
                {
                    control.Background = background;
                    control.Foreground = foreground;
                }
                else if (child is Panel panel)
                {
                    panel.Background = background;
                    // Iterate through all children of the panel recursively
                    SetThemeColors(panel, background, foreground);
                }

                // Recursively set theme colors for all child elements
                SetThemeColors(child, background, foreground);
            }

            // Set the background and foreground of the window itself
            if (parent is Window window)
            {
                window.Background = background;
                window.Foreground = foreground;
            }

        }
        private void ApplyGridViewColumnHeaderStyle(string styleKey)
        {
            Style style = (Style)FindResource(styleKey);
            if (style != null)
            {
                GridView gv = FileListView.View as GridView;
                if (gv != null)
                {
                    foreach (GridViewColumn column in gv.Columns)
                    {
                        if (column.Header is string headerText)
                        {
                            // Create a new GridViewColumnHeader with the correct style
                            GridViewColumnHeader header = new GridViewColumnHeader
                            {
                                Content = headerText,
                                Style = style
                            };
                            column.Header = header;
                        }
                        else if (column.Header is GridViewColumnHeader existingHeader)
                        {
                            // Update the style of the existing header
                            existingHeader.Style = style;
                        }
                        else if (column.Header != null)
                        {
                            // Handle cases where the header might be another type of content
                            GridViewColumnHeader newHeader = new GridViewColumnHeader
                            {
                                Content = column.Header,
                                Style = style
                            };
                            column.Header = newHeader;
                        }
                    }
                }
            }
        }

        private void SetImageSource(string uri)
        {
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(uri);
                image.CacheOption = BitmapCacheOption.OnLoad; // Ensure the image is fully loaded into memory
                image.EndInit();
                GifImage.Source = image;
                GifImage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image: {ex.Message}");
            }
        }
        private bool isDarkMode = false;

        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // Apply dark mode
                Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1E1E1E"));
                Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.White);

                // Apply dark mode column header style
                foreach (var column in ((GridView)FileListView.View).Columns)
                {
                    column.HeaderContainerStyle = (Style)FindResource("DarkGridViewColumnHeaderStyle");
                }
            }
            else
            {
                // Apply light mode
                Application.Current.Resources["BackgroundBrush"] = new SolidColorBrush(Colors.White);
                Application.Current.Resources["ForegroundBrush"] = new SolidColorBrush(Colors.Black);

                // Apply light mode column header style
                foreach (var column in ((GridView)FileListView.View).Columns)
                {
                    column.HeaderContainerStyle = (Style)FindResource("GridViewColumnHeaderStyle");
                }
            }
        }



        private void FileListView_DragLeave(object sender, DragEventArgs e)
        {
            
        }
    }
}







