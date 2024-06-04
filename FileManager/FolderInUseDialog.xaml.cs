using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace FileManager
{
    /// <summary>
    /// Interaction logic for FolderInUseDialog.xaml
    /// </summary>
    public partial class FolderInUseDialog : Window
    {
        public bool TryAgain { get; private set; }

        public FolderInUseDialog(string fileName, DateTime fileDate)
        {
            InitializeComponent();
            FileNameTextBlock.Text = fileName;
            FileDateTextBlock.Text = $"Date created: {fileDate}";
        }
 
        private void TryAgainButton_Click(object sender, RoutedEventArgs e)
        {
            TryAgain = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            TryAgain = false;
            this.Close();
        }
    }
}
