using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace FileManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<FileItem> allFiles;
        public MainWindow()
        {
            InitializeComponent();
            LoadDirectoryTree();
        }
        private void LoadDirectoryTree()
        {
            foreach (var drive in Directory.GetLogicalDrives())
            {
                var item = new TreeViewItem
                {
                    Header = drive,
                    Tag = drive
                };
                item.Items.Add(null);
                item.Expanded += Folder_Expanded;
                DirectoryTree.Items.Add(item);
            }
        }
        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                try
                {
                    foreach (var directory in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        var subitem = new TreeViewItem
                        {
                            Header = System.IO.Path.GetFileName(directory),
                            Tag = directory
                        };
                        subitem.Items.Add(null);
                        subitem.Expanded += Folder_Expanded;
                        item.Items.Add(subitem);
                    }
                }
                catch (UnauthorizedAccessException) { }
            }
        }

        private void DirectoryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
    
            var selectedItem = DirectoryTree.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                var path = selectedItem.Tag.ToString();
                PathBox.Text = path; // Update the path box
                LoadFiles(path);
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
            // Add logic here to return the correct icon based on the file extension
            // For example:
            switch (extension.ToLower())
            {
                case ".zip":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/zip-icon.png")); // Add your zip icon path here
                case ".txt":
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/txt-icon.png")); // Add your text file icon path here
                // Add more cases as needed for different file types
                default:
                    return new BitmapImage(new Uri("pack://application:,,,/Resources/file-icon.png")); // Default file icon path
            }
        }

        public class FileItem
        {
            public string Name { get; set; }
            public string Size { get; set; }
            public string Type { get; set; }
            public string DateModified { get; set; }
            public BitmapImage Icon { get; internal set; }
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
    }
    }

