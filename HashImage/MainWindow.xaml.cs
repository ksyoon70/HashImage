using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Windows.Interop;
using Microsoft.WindowsAPICodePack.Dialogs;

using System.ComponentModel;

namespace HashImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public ImageSource Icon { get; set; }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // DataContext를 현재 창으로 설정
            LoadDrives();                                                                                               
        }

        private string hash_SelectedFolder;
        /**
        *  이미지 넓이 설정
        * */
        public string Hash_SelectedFolder
        {
            get
            {
                return hash_SelectedFolder;
            }
            set
            {
                hash_SelectedFolder = value;
                OnPropertyChanged("Hash_SelectedFolder");
            }
        }

        private string _logContent;

        public string LogContent
        {
            get => _logContent;
            set
            {
                _logContent = value;
                OnPropertyChanged("LogContent");
                TrimLogIfNeeded();
                // 텍스트가 변경될 때 커서와 스크롤 이동
                if (LogTextBox != null)
                {
                    LogTextBox.CaretIndex = LogTextBox.Text.Length; // 커서를 마지막 위치로 이동
                    LogTextBox.ScrollToEnd(); // 스크롤을 마지막으로 이동
                }
            }
        }

        private void TrimLogIfNeeded()
        {
            // 줄 단위로 나눕니다.
            var lines = _logContent.Split('\n').ToList();

            // 1000줄이 넘는 경우 오래된 500줄 삭제
            if (lines.Count > 1000)
            {
                _logContent = string.Join("\n", lines.Skip(500));
                OnPropertyChanged(nameof(LogContent));
                
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 화면 로드 후 초기화 작업
            string hashFileName= "HashDictionary.txt";
            string currentDirectory = Directory.GetCurrentDirectory();

            // 경로에 파일 이름을 추가
            string fullPath = System.IO.Path.Combine(currentDirectory, hashFileName);

            if (ImageHashMng.InitEngine(fullPath))
            {
                LogContent += "Hash 파일 읽기 성공\r\n";
            }
            else
            {
                LogContent += "Hash 파일 읽기 오류!!!\r\n";
            }
        }

        private void ClearContent_Click(object sender, RoutedEventArgs e)
        {
            LogContent = string.Empty; // 로그 내용을 초기화
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadDrives()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    var driveItem = new TreeViewItem
                    {
                        Header = CreateHeader(drive.Name, GetFolderIcon()),
                        Tag = drive.RootDirectory.FullName
                    };

                    // 첫 번째 하위 폴더를 미리 로드
                    LoadSubDirectories(driveItem, drive.RootDirectory.FullName);

                    driveItem.Expanded += FolderExpanded;
                    DirectoryTree.Items.Add(driveItem);
                }
            }
        }

        private void LoadSubDirectories(TreeViewItem parentItem, string path)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(path))
                {
                    var subItem = new TreeViewItem
                    {
                        Header = CreateHeader(System.IO.Path.GetFileName(directory), GetFolderIcon()),
                        Tag = directory
                    };

                    // 기본적으로 하위 폴더가 있음을 표시하기 위해 null 추가
                    subItem.Items.Add(null);
                    subItem.Expanded += FolderExpanded;

                    parentItem.Items.Add(subItem);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 접근 권한이 없는 폴더는 무시
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading subdirectories: {ex.Message}");
            }
        }

        private void FolderExpanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();

                var fullPath = (string)item.Tag;
                try
                {
                    foreach (var directory in Directory.GetDirectories(fullPath))
                    {
                        var subItem = new TreeViewItem
                        {
                            Header = CreateHeader(System.IO.Path.GetFileName(directory), GetFolderIcon()),
                            Tag = directory
                        };
                        subItem.Items.Add(null);
                        subItem.Expanded += FolderExpanded;
                        item.Items.Add(subItem);
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
                // MessageBox.Show($"Selected path: {selectedItem.Tag}");
                // 파일 리스트 갱신
                var selectedPath = selectedItem.Tag as string;
                UpdateFileListView(selectedPath);
            }
        }

        

        private void OnTreeViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element)
            {
                var clickedItem = GetParentTreeViewItem(element);

                if (clickedItem != null)
                {
                    if (clickedItem.Items.Count == 1 && clickedItem.Items[0] == null)
                    {
                        FolderExpanded(clickedItem, null);
                    }
                }
            }
        }

        private TreeViewItem GetParentTreeViewItem(FrameworkElement element)
        {
            while (element != null && !(element is TreeViewItem))
            {
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return element as TreeViewItem;
        }


        private StackPanel CreateHeader(string text, ImageSource icon)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            var image = new Image
            {
                Source = icon,
                Width = 16,
                Height = 16,
                Margin = new Thickness(0, 0, 5, 0)
            };
            var textBlock = new TextBlock { Text = text };

            stack.Children.Add(image);
            stack.Children.Add(textBlock);
            return stack;
        }

        private ImageSource GetFolderIcon()
        {
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(folderPath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);

            if (hImg == IntPtr.Zero) return null;

            var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            DestroyIcon(shinfo.hIcon); // 리소스 해제
            return bitmapSource;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    Hash_SelectedFolder = dialog.FileName;
                }
            }

        }

        private void HashInit_Click(object sender, RoutedEventArgs e)
        {
            if(ImageHashMng.GenHash(hash_SelectedFolder))
            {
                LogContent += "Hash 파일 생성\r\n";
            }
            else 
            {
                LogContent += "Hash 파일 생성 시 오류 발생!!!\r\n";
            }
            
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1; // 작은 아이콘
    }
}