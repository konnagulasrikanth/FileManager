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


        // Store selected items for copy/cut operations
        private List<FileItem> selectedItems = new List<FileItem>();
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

                        foreach (var file in Directory.GetFiles(fileItem.Path))
                        {
                            var subItem = CreateFileItem(file, false);
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
                Icon = isDirectory ? new BitmapImage(new Uri("C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\Folder1.jpg")) : GetIconForFile(System.IO.Path.GetExtension(path))
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
                PathBox.Text = fileItem.Path; // Update the path box
                LoadFiles(fileItem.Path);
            }
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
                        Path = directory,  // Set the Path property for directories
                        Size = "",
                        Type = "File folder",
                        DateModified = Directory.GetLastWriteTime(directory).ToString(),
                        Icon = new BitmapImage(new Uri("C:\\Users\\srikanthko\\Desktop\\FileManager\\FileManager\\Resources\\Folder1.jpg")) // Add your folder icon path here
                    });
                }

                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);
                    allFiles.Add(new FileItem
                    {
                        Name = fileInfo.Name,
                        Path = file,  // Set the Path property for files
                        Size = fileInfo.Length.ToString(),
                        Type = fileInfo.Extension,
                        DateModified = fileInfo.LastWriteTime.ToString(),
                        Icon = GetIconForFile(fileInfo.Extension)
                    });
                }
            }
            catch (UnauthorizedAccessException) { }
            FileListView.ItemsSource = allFiles;
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

                default:
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/folder (1).png"));
            }
        }


        public class FileItem
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Size { get; set; }
            public string Type { get; set; }
            public string DateModified { get; set; }
            public BitmapImage Icon { get; set; }
            public ObservableCollection<FileItem> SubItems { get; set; } = new ObservableCollection<FileItem>();
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
            var selectedItem = FileListView.SelectedItem as FileItem;
            if (selectedItem != null && Directory.Exists(selectedItem.Path))
            {
                try
                {
                    LoadFiles(selectedItem.Path);
                    PathBox.Text = selectedItem.Path; // Update the path box
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cannot open folder: {ex.Message}");
                }
            }
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
            }

        }

       
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var textBlock = (TextBlock)sender;
            var listViewItem = FindAncestor<ListViewItem>(textBlock);

            if (listViewItem != null)
            {
                var fileItem = (FileItem)listViewItem.DataContext;
                var existingNames = allFiles.Select(item => item.Name).ToList();

                // Enable text editing if the item is a folder or the name doesn't already exist
                if (Directory.Exists(fileItem.Path) || !existingNames.Contains(fileItem.Name))
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
                else
                {
                    MessageBox.Show("A file or folder with this name already exists. Please choose a different name.");
                }
            }
        }
        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            } while (current != null);
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

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    var childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
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


     
        private void CreateFolderStructure_Click(object sender, RoutedEventArgs e)
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
            string newFolderPath = System.IO.Path.Combine(rootPath, newFolderName);
            int folderNumber = 0;

            // Scan existing folders to find the highest incremented number
            foreach (var directory in Directory.GetDirectories(rootPath))
            {
                string folderName = System.IO.Path.GetFileName(directory);
                if (folderName.StartsWith(baseFolderName))
                {
                    if (folderName.Equals(baseFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        folderNumber = Math.Max(folderNumber, 1);
                    }
                    else if (folderName.StartsWith($"{baseFolderName} ("))
                    {
                        string numberPart = folderName.Substring(baseFolderName.Length + 2).TrimEnd(')');
                        if (int.TryParse(numberPart, out int number))
                        {
                            folderNumber = Math.Max(folderNumber, number);
                        }
                    }
                }
            }

            // Increment the number for the new folder
            if (folderNumber > 0)
            {
                folderNumber++;
                newFolderName = $"{baseFolderName} ({folderNumber})";
                newFolderPath = System.IO.Path.Combine(rootPath, newFolderName);
            }

            try
            {
                Directory.CreateDirectory(newFolderPath);
                // Refresh the ListView to show the new folder
                CreateFolderStructure(newFolderPath);
                LoadFiles(rootPath);
                MessageBox.Show($"Folder '{newFolderName}' created successfully!");

                // Add the new folder to the ObservableCollection and update the UI
                /* var newFolder = new FileItem
                 {
                     Name = newFolderName,
                     Path = newFolderPath,
                     Size = "",
                     Type = "File folder",
                     DateModified = DateTime.Now.ToString(),
                     Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/folder (1).png"))
                 };
                 allFiles.Add(newFolder);*/
                FileListView.ItemsSource = allFiles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating folder: {ex.Message}");
            }
        }


        private void CreateFolderStructure(string rootPath)
        {

            string[] subfolders = new string[]
            {
                "Requirement",
                "Build",
                "Design",
                "Deployment",
                "Testing",

            };
            try
            {
                // Create each subfolder if it doesn't exist
                foreach (var subfolder in subfolders)
                {
                    string subfolderPath = System.IO.Path.Combine(rootPath, subfolder);
                    if (!Directory.Exists(subfolderPath))
                    {
                        Directory.CreateDirectory(subfolderPath);
                    }
                }
                // MessageBox.Show("Folder structure created successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating folder structure: {ex.Message}");
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
     /*   private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(file));
                File.Copy(file, destFile);
            }
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string destDirectory = System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(directory));
                CopyDirectory(directory, destDirectory);
            }
        }*/
        private void PopulateListView()
        {
            // Assuming you want to load the current directory files into the FileListView
            LoadFiles(CurrentDirectory);
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
        // Helper method to get full path from TreeViewItem
        private string GetFullPathFromTreeViewItem(TreeViewItem item)
        {
            string path = item.Header.ToString();
            TreeViewItem parent = item.Parent as TreeViewItem;

            while (parent != null)
            {
                path = Path.Combine(parent.Header.ToString(), path);
                parent = parent.Parent as TreeViewItem;
            }

            return path;
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
                            if (Directory.Exists(item.Path))
                            {
                                // Zip and archive the folder
                                string zipFileName = $"{Path.GetFileName(item.Path)}.zip";
                                string zipFilePath = Path.Combine(archivePath, zipFileName);
                                ZipFolder(item.Path, zipFilePath);

                                // Delete the original folder
                                Directory.Delete(item.Path, true);

                                // Recreate an empty folder with the same name
                                Directory.CreateDirectory(item.Path);
                            }
                            else if (File.Exists(item.Path))
                            {
                                File.Delete(item.Path);
                            }
                        }

                        // Refresh the file list view
                        LoadFiles(CurrentDirectory);
                        MessageBox.Show("Selected items deleted and empty folders created successfully.");
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
            if (FileListView.SelectedItem is FileItem selectedFileItem)
            {
                selectedItemPath = Path.Combine(PathBox.Text, selectedFileItem.Name);
                isDirectory = Directory.Exists(selectedItemPath);
                MessageBox.Show("Item copied to clipboard.");
            }
            else
            {
                MessageBox.Show("Please select a file or folder to copy.");
            }
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedItemPath))
            {
                string destinationPath = PathBox.Text;
                string destinationFullPath = Path.Combine(destinationPath, Path.GetFileName(selectedItemPath));

                try
                {
                    if (isDirectory)
                    {
                        CopyDirectory(selectedItemPath, destinationFullPath);
                    }
                    else if (File.Exists(selectedItemPath))
                    {
                        File.Copy(selectedItemPath, destinationFullPath, overwrite: true);
                    }

                    MessageBox.Show("Paste operation completed successfully.");
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
                MessageBox.Show("No item to paste. Please copy a file or folder first.");
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



    }


}



