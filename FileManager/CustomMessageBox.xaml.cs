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
    /// Interaction logic for customMessageBox.xaml
    /// </summary>
    public partial class customMessageBox : Window
    {

        public enum CustomMessageBoxResult
        {
            Replace,
            Skip,
            Compare,
            None
        }

        public CustomMessageBoxResult Result { get; private set; } = CustomMessageBoxResult.None;

        public customMessageBox(string message)
        {
            InitializeComponent();
            MessageTextBlock.Text = message;
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Replace;
            DialogResult = true;
            Close();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Skip;
            DialogResult = true;
            Close();
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            Result = CustomMessageBoxResult.Compare;
            DialogResult = true;
            Close();
        }
    }
}