using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RatosFileSearch
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isSearching = false;
        private Stopwatch stopwatch = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            LoadSet();
        }

        //функция для загрузки критериев поиска пользователя
        private void LoadSet()
        {
            txtStartDirectory.Text = Properties.Settings.Default.StartDirectory;
            txtFileNameFormat.Text = Properties.Settings.Default.FileNameFormat;
        }
        private void SaveSettings()
        {
            // Сохраняем текущие настройки стартовой директории и шаблона имени файла
            Properties.Settings.Default.StartDirectory = txtStartDirectory.Text;
            Properties.Settings.Default.FileNameFormat = txtFileNameFormat.Text;
            Properties.Settings.Default.Save(); // Сохраняем изменения
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            // Если поиск уже выполняется, останавливаем его
            if (isSearching)
            {
                isSearching = false;
                return;
            }

            string startDirectory = txtStartDirectory.Text;
            string fileNameFormat = txtFileNameFormat.Text;

            SaveSettings();

            // Очистка древа файлов
            treeViewResults.Items.Clear();

            stopwatch.Reset();
            stopwatch.Start();

            currentDirectory.Text = "Поиск в: " + startDirectory;

            // Проверка полей ввода
            if (string.IsNullOrWhiteSpace(startDirectory) || string.IsNullOrEmpty(fileNameFormat))
            {
                MessageBox.Show("Пожалуйста, введите начальный каталог и имя файла.");
                return;
            }

            await Task.Run(() =>
            {
                isSearching = true;
                // Создаем узел для главной папки и начинаем поиск файлов
                Dispatcher.Invoke(() =>
                {
                    TreeViewItem mainNode = new TreeViewItem { Header = startDirectory };
                    treeViewResults.Items.Add(mainNode);
                    SearchFiles(startDirectory, fileNameFormat, mainNode);
                });
            });
        }

        private void SearchFiles(string startDirectory, string fileNameFormat, TreeViewItem parentNode)
        {
            try
            {
                // Поиск файлов по критерию fileNameFormat в текущей директории
                string[] files = Directory.GetFiles(startDirectory, $"*{fileNameFormat}", SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    if (!isSearching)
                        return;

                    Dispatcher.Invoke(() =>
                    {
                        // Добавляем файл как дочерний элемент текущего узла
                        parentNode.Items.Add(new TreeViewItem { Header = Path.GetFileName(file) });
                    });
                }

                // Поиск поддиректорий в текущей директории
                string[] directories = Directory.GetDirectories(startDirectory);
                foreach (string subDirectory in directories)
                {
                    if (!isSearching)
                        return;

                    // Создаем узел для поддиректории и добавляем его как дочерний элемент родительского узла
                    Dispatcher.Invoke(() =>
                    {
                        TreeViewItem subDirectoryNode = new TreeViewItem { Header = Path.GetFileName(subDirectory) };
                        parentNode.Items.Add(subDirectoryNode);
                        // Рекурсивный вызов для поиска файлов в поддиректории
                        SearchFiles(subDirectory, fileNameFormat, subDirectoryNode);
                    });
                }

                // Обновление информации о найденных файлах
                Dispatcher.Invoke(() =>
                {
                    // Количество файлов, найденных по критерию fileNameFormat
                    int foundFileCount = CountFilesByFormat(startDirectory, fileNameFormat);
                    // Количество файлов, найденных по критерию startDirectory
                    int totalFileCount = CountFilesInDirectory(startDirectory);
                    foundFiles.Text = "Найдено файлов: " + foundFileCount;
                    totalFiles.Text = "Всего файлов: " + totalFileCount;
                    elapsedTime.Text = "Затрачено времени на поиск: " + stopwatch.Elapsed.TotalSeconds.ToString();
                });
            }
            catch (UnauthorizedAccessException) { }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка: {ex.Message}");
                    txtStatus.Text = "Обнаружена ошибка во время поиска";
                });
            }
        }

        private int CountFilesByFormat(string startDirectory, string fileNameFormat)
        {
            try
            {
                int totalCount = Directory.GetFiles(startDirectory, $"*{fileNameFormat}", SearchOption.AllDirectories).Length;
                return totalCount;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return 0; // В случае других ошибок также возвращаем 0
            }
        }

        private int CountFilesInDirectory(string directory)
        {
            try
            {
                int totalCount = Directory.GetFiles(directory).Length;

                // Рекурсивно подсчитываем количество файлов во всех поддиректориях
                foreach (string subDirectory in Directory.GetDirectories(directory))
                {
                    totalCount += CountFilesInDirectory(subDirectory);
                }

                return totalCount;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
                return 0; // В случае других ошибок также возвращаем 0
            }
        }
    }
}
