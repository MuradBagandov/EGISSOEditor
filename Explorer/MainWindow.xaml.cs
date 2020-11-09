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
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ExplorerView DirectoryView;
        ProcessWindow processWindow;
        CancellationTokenSource cancelToken = new CancellationTokenSource();
        private bool isShowProcess = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DirectoryView = new ExplorerView();
            DirectoryView.IsChangeEvent += () => { ScrollView.Content = DirectoryView.Control; };
            FileEGISSOControl.Init();

            //FileEGISSOControl.StartAProcessEvent += () => { OpenShowProcess();};
            FileEGISSOControl.EndAProcessEvent += () => {
                processWindow.CloseProcess(); isShowProcess = false; 
            };
        }


        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdAddFiles = new OpenFileDialog();
            ofdAddFiles.Multiselect = true;
            ofdAddFiles.Filter = "Excel xlsx; xls|*.xlsx; *.xls";
            if (ofdAddFiles.ShowDialog() == true)
            { 
                foreach (string file in ofdAddFiles.FileNames)
                {
                    try
                    {
                        FileEGISSOControl.AddFiles(file, out string NewFileName);
                        DirectoryView.AddFile(NewFileName);
                    }
                    catch (Exception ex) 
                    {
                        MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error); 
                    }
                }
            }
        }
 
        private void PatternCorrection_Click(object sender, RoutedEventArgs e)
        {
            ProcessFilesAsync(ActionOnFile.PatternCorrection);
        }

        private void ErrorChecking_Click(object sender, RoutedEventArgs e)
        {
            ProcessFilesAsync(ActionOnFile.ErrorChecking);
        }

        private async void ProcessFilesAsync(ActionOnFile action)
        {
            Progress<GetProcessInformation> processInfo = new Progress<GetProcessInformation>(ShowProcess);
            try
            {
                FileEGISSOControl.ClearSelectFile();
                foreach (string file in DirectoryView.GetSelectFiles())
                {
                     FileEGISSOControl.AddSelectFile(file);
                }
                cancelToken = new CancellationTokenSource();
                List<string> SelectFiles = DirectoryView.GetSelectFiles();
                await FileEGISSOControl.ProcessFilesAsync(action, processInfo, cancelToken.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CombineFiles_Click(object sender, RoutedEventArgs e)
        {
            Progress<GetProcessInformation> processInfo = new Progress<GetProcessInformation>(ShowProcess);

            if (DirectoryView.GetSelectFiles().Count > 0)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Excel xlsx; xls|*.xlsx; *.xls";
                if (sfd.ShowDialog(this.Owner) == true)
                {
                    try
                    {
                        FileEGISSOControl.ClearSelectFile();
                        foreach (string file in DirectoryView.GetSelectFiles())
                        {
                            FileEGISSOControl.AddSelectFile(file);
                        }
                        cancelToken = new CancellationTokenSource();
                        await FileEGISSOControl.CombineFileAsync(sfd.FileName, processInfo, cancelToken.Token);
                        FileEGISSOControl.AddFiles(sfd.FileName, out string NewFileName);
                        DirectoryView.AddFile(NewFileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message,Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    } 
                }
            }
        }

        private void ShowProcess(GetProcessInformation processInfo)
        {
            if (!isShowProcess)
                OpenShowProcess();

            float shareFile = 100f / processInfo.TotalFiles;
            float currentFileProgress = (float)processInfo.CurrentFileProgress / processInfo.TotalFilesProgress;
            processWindow.Value = processInfo.ProcessedFiles == 0 ? 0 : shareFile * (processInfo.ProcessedFiles -1) + currentFileProgress  * shareFile;
            processWindow.ProcessName = processInfo.ProcessName;
            processWindow.CurrentElementName = processInfo.CurrentFileName;
            processWindow.remainingItems = $"{processInfo.TotalFiles - (processInfo.ProcessedFiles-1)} из {processInfo.TotalFiles}";
            processWindow.Update();
        }

        private void OpenShowProcess()
        {
            isShowProcess = true;
            processWindow = new ProcessWindow();
            processWindow.Owner = this;
            processWindow.ClosingRequest += () => isCancelProcess();
            processWindow.ShowDialog();
        }

        private void isCancelProcess()
        {
            cancelToken.Cancel(); 
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFile(DirectoryView.GetSelectFiles());
        }

        private void btnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveAsFile(DirectoryView.GetSelectFiles());
        }

        private void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            SaveFile(DirectoryView.GetFiles());
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            CloseFile(DirectoryView.GetSelectFiles(), true, out bool isCancel);
        }

        private void btnRemoveAll_Click(object sender, RoutedEventArgs e)
        {
            CloseFile(DirectoryView.GetFiles(), true, out bool isCancel);
        }

        private void CloseFile(string file, bool saveBefore)
        {
            List<string> fileList = new List<string>(1);
            fileList.Add(file);
            CloseFile(fileList, saveBefore, out bool isCancel);
        }

        private void CloseFile(List<string> files, bool saveBefore, out bool isCancel)
        {
            bool isPressedCancel = false; isCancel = false;
            List<string> filesRemoved = new List<string>();
            foreach (string file in files)
            {
                if (isPressedCancel)
                    break;

                while (true)
                {
                    try
                    {
                        if (saveBefore)
                        {
                            if (FileEGISSOControl.GetInfоFileIsSave(file))
                            {
                                SaveFileBeforeClose(file, out MessageBoxResult result);
                                if (result == MessageBoxResult.Cancel)
                                {
                                    isPressedCancel = true;
                                    break;
                                }
                            }
                        }
                           
                        FileEGISSOControl.Close(file);
                        filesRemoved.Add(file);
                        break;
                    }
                    catch (Exception ex)
                    {
                        var result = MessageBox.Show(ex.Message + "\n Повторить?", "Ошибка закрытия файла!", MessageBoxButton.YesNoCancel);
                        if (result == MessageBoxResult.Cancel)
                        {
                            isPressedCancel = true;
                            break;
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            break;
                        } 
                    }
                }    
            }

            if (isPressedCancel)
                isCancel = true;

            DirectoryView.RemoveFiles(filesRemoved);
        }

        private void SaveFileBeforeClose(string files, out MessageBoxResult result)
        {
            result = MessageBox.Show($"Сохранить изменения {System.IO.Path.GetFileNameWithoutExtension(files)} перед закрытием?", Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SaveFile(files);
            }
        }

        private void SaveFile(string file)
        {
            List<string> fileList = new List<string>(1);
            fileList.Add(file);
            SaveFile(fileList);
        }

        private void SaveFile(List<string> files)
        {
            foreach (string file in files)
            {
                while (true)
                {
                    try
                    {
                        FileEGISSOControl.Save(file);
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Файл удален!")
                        {
                            CloseFile(file, false);
                            MessageBox.Show($"Файл {file} удален!", "Ошибка сохранения файла!", MessageBoxButton.OK);
                            break;
                        }
                        else
                        {
                            var result = MessageBox.Show(ex.Message + "\n Повторить?", "Ошибка сохранения файла!", MessageBoxButton.YesNoCancel);
                            if (result == MessageBoxResult.No)
                                break;
                            else if (result == MessageBoxResult.Cancel)
                                return;
                        } 
                    }
                }       
            }
        }

        private void SaveAsFile(List<string> files)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Excel xlsx; xls|*.xlsx; *.xls";
            foreach (string file in files)
            {
                if (sfd.ShowDialog() == true)
                {
                    while (true)
                    {
                        try
                        {
                            FileEGISSOControl.SaveAs(file, sfd.FileName);
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "Файл удален!")
                            {
                                CloseFile(file, false);
                                MessageBox.Show($"Файл {file} удален!", "Ошибка сохранения файла!", MessageBoxButton.OK);
                                break;
                            }
                            else
                            {
                                var result = MessageBox.Show(ex.Message + "\n Повторить?", "Ошибка сохранения файла!", MessageBoxButton.YesNoCancel);
                                if (result == MessageBoxResult.No)
                                    break;
                                else if (result == MessageBoxResult.Cancel)
                                    return;
                            }
                        }
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseFile(DirectoryView.GetFiles(), true, out bool isCancel);
            if (isCancel)
            {
                e.Cancel = true;
            }
            else
                EGISSO.CloseExcel();
        }

        private void btnSNILS_Correct_Click(object sender, RoutedEventArgs e)
        {
            SNILS_Corrector SNILS_Corrector_windows = new SNILS_Corrector();
            SNILS_Corrector_windows.Owner = this;
            SNILS_Corrector_windows.Show();
        }
    }
}
