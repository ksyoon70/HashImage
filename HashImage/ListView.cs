using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace HashImage
{
    public partial class MainWindow : Window
    {
        private void UpdateFileListView(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            var fileItems = new List<FileItem>();
            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileInfo = new FileInfo(file);

                    // 아이콘 로드
                    var icon = GetFileIcon(file);

                    fileItems.Add(new FileItem
                    {
                        Name = fileInfo.Name,
                        Path = fileInfo.FullName,
                        Type = fileInfo.Extension,
                        Size = (fileInfo.Length / 1024.0).ToString("F2") + " KB",
                        Icon = icon
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show($"Access denied to {path}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading files: {ex.Message}");
            }

            FileListView.ItemsSource = fileItems;
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selectedFile)
            {
                // 이미지 확장자 확인
                var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                if (imageExtensions.Contains(selectedFile.Type.ToLower()))
                {
                    try
                    {
                        // 이미지 파일 로드
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(selectedFile.Path, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        // imageName에 이미지 표시
                        imageName.Source = bitmap;

                        //key를 얻어온다.
                        StringBuilder key = new StringBuilder(100);
                        if (ImageHashMng.GetKey(selectedFile.Path, key, key.Capacity))
                        {
                            LogContent += key.ToString() + "\r\n";

                            StringBuilder value = new StringBuilder(100);
                            if (ImageHashMng.FindValue(key.ToString(), value, value.Capacity))
                            {
                                LogContent += "인식문자:" + value.ToString() + "\r\n";
                            }
                            else
                            {
                                LogContent += "등록되지않음!!!\r\n";
                            }
                        }
                        else
                        {
                            LogContent += "Key생성오류!!!\r\n";
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error displaying image: {ex.Message}");
                    }
                }
                else
                {
                    // 이미지가 아닌 파일 선택 시 imageName 초기화
                    imageName.Source = null;
                }
            }
        }

        private ImageSource GetFileIcon(string filePath)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), SHGFI_ICON | SHGFI_SMALLICON);

            if (hImg == IntPtr.Zero) return null;

            var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            DestroyIcon(shinfo.hIcon); // 리소스 해제
            return bitmapSource;
        }
    }
}
