using System;
using System.Collections.Generic;
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
using System.Windows.Threading;

namespace EGISSOEditor
{
    public struct FileState
    {
        public string Path;
        public bool isSelect;

        public FileState(string Path, bool isSelect)
        {
            this.Path = Path;
            this.isSelect = isSelect;
        }
    }

    public class ExplorerView
    {
        public Action IsChangeEvent;
        
        public UserControl Control;
        private WrapPanel Explorer;
        private List<FileState> ListFiles = new List<FileState>();

        private bool isCtrlPressed = false, isShiftPressed = false;
        private int SelectIndex = 0;
       
        public ExplorerView()
        {
            Explorer = new WrapPanel();
            Explorer.Margin = new Thickness(3d);
            Control = new UserControl()
            {
                Background = new SolidColorBrush(Color.FromArgb(2, 0, 0, 0)),
                Content = Explorer
            };

            Control.MouseDown += new MouseButtonEventHandler((s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    isShiftPressed = false;
                    if (!isCtrlPressed) BtnUncheked();
                } 
            });

            Control.KeyDown += new KeyEventHandler((s,e) => {
                if (e.Key == Key.LeftCtrl) isCtrlPressed = true;
                if (e.Key == Key.LeftShift) isShiftPressed = true;
            });
            Control.KeyUp += new KeyEventHandler((s, e) => {
                if (e.Key == Key.LeftCtrl) isCtrlPressed = false;
                if (e.Key == Key.LeftShift) isShiftPressed = false;
            });
        }

        public void AddFile(string path)
        {
            if (File.Exists(path))
            {
                if (GetIndexFile(path) != -1) return;
                AddFileOnExplorer(path, System.IO.Path.GetFileName(path), @"Resources\File_Excel.png");
                ListFiles.Add(new FileState(path, false));
                IsChangeEvent?.Invoke();
            }     
        }

        public List <string> GetSelectFiles()
        {
            List<string> ResultList = new List<string>();
            for (int i = 0; i < ListFiles.Count; i++)
            {
                if (ListFiles[i].isSelect == true)
                    ResultList.Add(ListFiles[i].Path);
            }
            return ResultList;
        }

        public List<string> GetFiles()
        {
            List<string> ResultList = new List<string>();
            for (int i = 0; i < ListFiles.Count; i++)
                ResultList.Add(ListFiles[i].Path);
            return ResultList;
        }

        public void RemoveSelectFiles()
        {
            for (int i = ListFiles.Count-1; i >= 0; i--)
            {
                if (ListFiles[i].isSelect == true)
                    ListFiles.RemoveAt(i);
            }
            Update();
        }

        public void RemoveFiles(List<string> fileName)
        {
            foreach (string file in fileName)
            {
                for (int i = ListFiles.Count - 1; i >= 0; i--)
                {
                    if (ListFiles[i].Path == file)
                    {
                        ListFiles.RemoveAt(i);
                        break;
                    }
                        
                }
            }
            Update();
        }

        public void RemoveAllFiles()
        {
            Explorer.Children.Clear();
            ListFiles.Clear();
        }

        private int GetIndexFile(string path)
        {
            for (int i = 0; i < ListFiles.Count; i++)
                if (ListFiles[i].Path == path)
                    return i;
            return -1;
        }

        private void Update()
        {
            Explorer.Children.Clear();
            foreach (FileState file in ListFiles)
                AddFileOnExplorer(file.Path, System.IO.Path.GetFileName(file.Path), @"Resources\File_Excel.png");
            IsChangeEvent?.Invoke();
        }

        private void AddFileOnExplorer(string dir, string name, string pathIcon)
        {
            StackPanel DirPanel = new StackPanel();
            Button btn = new Button()
            {
                ToolTip = dir,
                Content = DirPanel,
                Width = 90,
                Background = null,
                BorderBrush = null,
                VerticalAlignment = VerticalAlignment.Top
            };

            btn.Click += new RoutedEventHandler((s, a) =>
            {
                if (isCtrlPressed)
                    BtnChecked(btn);
                else if (isShiftPressed)
                    BtnRangeChecked(btn);
                else
                {
                    BtnUncheked();
                    BtnChecked(btn);
                }
            });
            btn.MouseDoubleClick += new MouseButtonEventHandler((s, e) => {
                if (e.LeftButton == MouseButtonState.Pressed)
                    System.Diagnostics.Process.Start(btn.ToolTip.ToString());
            });

            Image img = new Image
            {
                Source = new BitmapImage(new Uri(pathIcon, UriKind.RelativeOrAbsolute)),
                Width = 50
            };

            TextBlock label = new TextBlock
            {
                TextAlignment = TextAlignment.Center,
                Text = name,
                TextWrapping = TextWrapping.Wrap
            };

            DirPanel.Children.Add(img);
            DirPanel.Children.Add(label);
            Explorer.Children.Add(btn);
            
        }
        
        private void BtnChecked(Button btn)
        {
            int index = GetIndexFile(btn.ToolTip.ToString());
            if (index != -1)
            {
                if (ListFiles[index].isSelect)
                {
                    ListFiles[index] = new FileState(ListFiles[index].Path, false);
                    btn.Background = null;
                    btn.BorderBrush = null;
                }
                else
                {
                    ListFiles[index] = new FileState(ListFiles[index].Path, true);
                    btn.Background = new SolidColorBrush(Color.FromRgb(184, 227, 251));
                    btn.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 148, 173));
                }
                SelectIndex = index;
            }
        }

        private void BtnRangeChecked(Button btn)
        {
            int index = GetIndexFile(btn.ToolTip.ToString());
            int tmpSelectIndex = SelectIndex;
            BtnUncheked();
            SelectIndex = tmpSelectIndex;
            if (tmpSelectIndex > index)
                Swap(ref tmpSelectIndex, ref index);
            
            for (int i = tmpSelectIndex; i <= index; i++)
            {
                Button lBtn = (Button)Explorer.Children[i];
                ListFiles[i] = new FileState(ListFiles[i].Path, true);
                lBtn.Background = new SolidColorBrush(Color.FromRgb(184, 227, 251));
                lBtn.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 148, 173));
            }

            void Swap<T>(ref T a, ref T b) 
            {
                T temp = a;a = b; b = temp;
            }
        }

        private void BtnUncheked()
        {
            for (int i = 0; i < ListFiles.Count; i++)
                ListFiles[i] = new FileState(ListFiles[i].Path, false);
            foreach (Button btn in Explorer.Children)
            {
                btn.Background = null;
                btn.BorderBrush = null;
            }
            SelectIndex = 0;
        }
    }     
}
