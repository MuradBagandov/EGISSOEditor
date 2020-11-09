using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace EGISSOEditor
{
    /// <summary>
    /// Логика взаимодействия для ProcessWindow.xaml
    /// </summary>
    public partial class ProcessWindow : Window
    {
        public float Value { get; set; }
        public string ProcessName { get; set; }
        public string CurrentElementName { get; set; }
        public string remainingItems { get; set; }
        public event Action ClosingRequest;
        private bool closePermission = false;

        public ProcessWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public async void Update()
        {
            setValue(Value);
            LblProcessName.Content = ProcessName;
            LblCurrentElementName.Content = CurrentElementName;
            LblremainingItems.Content = remainingItems;
            LblProgress.Content = (int)Value + "%";
        }

        private async void setValue(float Value)
        {
            for (double i = Progress.Value; i<= Value; i++)
            {
                Progress.Value++;
                await Task.Delay(1);
            } 
        }

        public void CloseProcess()
        {
            closePermission = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!closePermission)
            {
                ClosingRequest?.Invoke();
                Progress.IsIndeterminate = true;
                e.Cancel = true;
            }
            
        }
    }
}
