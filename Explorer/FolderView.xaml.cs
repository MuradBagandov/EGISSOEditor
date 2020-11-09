using System;
using System.Collections;
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
    public class FolderViewElement
    {
        public Button Element;
        public string Directory;
        public string Name;
        public bool isSelect;

        public FolderViewElement(Button element, string directory, string name)
        {
            Element = element;
            Directory = directory;
            Name = name;
            isSelect = false;
        }
    }

    /// <summary>
    /// Логика взаимодействия для FolderView.xaml
    /// </summary>
    public partial class FolderView : UserControl
    {
        public string DirectoryView { get; set; }
        public string Filter { get; set; }
        public Uri FileIconUri { get; set; }

        private List<FolderViewElement> files = new List<FolderViewElement>();
        private DispatcherTimer timer;
       
        public FolderView()
        {
            InitializeComponent();
        }

        private void Update()
        {
            string[] files;
            try {
                files = Directory.GetFiles(DirectoryView, Filter ?? "*");
                WrapContent.Children.Clear();
                
                foreach (string file in files)
                {
                    FileInfo filein = new FileInfo(file);
                    AddFolderViewElement(filein.FullName, filein.Name);
                }
               foreach (UIElement element in WrapContent.Children)
                {
                    if (element is Visual)
                    {
                        Console.WriteLine("true");
                    }
                }
            }
            catch
            {

            }
            Button btn = WrapContent.Children[0] as Button;
            btn.Background = new SolidColorBrush(Color.FromRgb(184, 227, 251));
            btn.BorderBrush = new SolidColorBrush(Color.FromRgb(99, 148, 173));

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0,0,2);
            timer.Tick += new EventHandler((s,a)=> { Update(); });
            timer.Start();
        }

        private void AddFolderViewElement(string dir, string name)
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

            btn.Click += FolderViewElement_Click;
            TextBlock label = new TextBlock
            {
                TextAlignment = TextAlignment.Center,
                Text = name,
                TextWrapping = TextWrapping.Wrap
            };

            Image img = new Image{ Width = 50};

            if (FileIconUri != null)
                img.Source = new BitmapImage(FileIconUri);


            DirPanel.Children.Add(img);
            DirPanel.Children.Add(label);
            WrapContent.Children.Add(btn);

        }

        private void FolderViewElement_Click(object sender, RoutedEventArgs e)
        {

        }


        //private int GetIndexFile(string path)
        //{
        //    //for (int i = 0; i < files.Count; i++)
        //    //    if (files[i].Path == path)
        //    //        return i;
        //    //return -1;
        //}

    }
}
