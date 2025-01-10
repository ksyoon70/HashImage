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
using System.Threading.Tasks;

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
        private void CurrentFolderTest_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = DirectoryTree.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                // 선택된 TreeViewItem의 Tag 속성을 사용하여 폴더 경로를 얻음
                var selectedPath = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    ProcessImageFilesInFolder(selectedPath);
                }
            }
        }

        private async void ProcessImageFilesInFolder(string folderPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    // 지원하는 이미지 확장자
                    string[] supportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };

                    // 현재 폴더의 이미지 파일 검색
                    var imageFiles = Directory.GetFiles(folderPath)
                                              .Where(file => supportedExtensions.Contains(System.IO.Path.GetExtension(file).ToLower()))
                                              .ToList();
                    int totalImageCount = imageFiles.Count;
                    int currentCount = 0;
                    int regCount = 0;
                    int unregCount = 0;
                    int MODCNT = totalImageCount / 20;
                    if (MODCNT == 0)
                        MODCNT = 1;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogContent += $"총 파일 갯수:{imageFiles.Count}\r\n";
                    });

                    // UI 업데이트는 Dispatcher를 사용하여 실행
                    foreach (var imageFile in imageFiles)
                    {

                        //key를 얻어온다.
                        StringBuilder key = new StringBuilder(100);
                        if (ImageHashMng.GetKey(imageFile, key, key.Capacity))
                        {
                            StringBuilder value = new StringBuilder(100);
                            if (ImageHashMng.FindValue(key.ToString(), value, value.Capacity))
                            {
                                regCount++;
                            }
                            else
                            {
                                unregCount++;
                            }
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LogContent += $"Key생성오류 파일: {imageFile}\r\n";
                            });
                            unregCount++;
                        }

                        currentCount++;

                        if(currentCount % MODCNT == 0)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LogContent += $"{currentCount * 100 / totalImageCount}% 진행\r\n";
                            });
                        }
                    }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogContent += $"100% 완료\r\n";
                        LogContent += $"등록 : {regCount}, {regCount*100/ totalImageCount} %\r\n";
                        LogContent += $"미등록 : {unregCount}, {unregCount * 100 / totalImageCount} %\r\n";
                    });

                }
                catch (UnauthorizedAccessException)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogContent += $"권한이 없어 접근할 수 없는 폴더: {folderPath}\r\n";
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogContent += $"오류 발생 - {ex.Message}: {folderPath}\r\n";
                    });
                }
            });

        }
    }
}
