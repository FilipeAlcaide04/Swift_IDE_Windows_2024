using Swift_for_Windows;
using Microsoft.Win32;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Net.NetworkInformation;


namespace Swift_for_Windows
{
    public partial class MainWindow : Window
    {
        private double originalWidth;
        private double originalHeight;
        private double originalLeft;
        private double originalTop;

        private bool _isExpanded = false;
        public MainWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)  // Check if Ctrl is pressed
            {
                if (e.Key == Key.OemPlus || e.Key == Key.Add)  // Check if the "+" key is pressed
                {
                    cima_Click();  // Increase text size
                }
                else if (e.Key == Key.OemMinus || e.Key == Key.Subtract)  // Check if the "-" key is pressed
                {
                    baixo_Click();  // Decrease text size
                }
                else if (e.Key == Key.S)  // Check if the "S" key is pressed
                {
                    SaveFile_Click();  // Call your save function
                    e.Handled = true;  // Mark the event as handled
                }
                else if (e.Key == Key.Q)
                {
                    SaveFile_Click();

                    if (tabControl.SelectedItem is TabItem selectedTab &&
                selectedTab.Content is Grid grid &&
                grid.Children[0] is TextEditor textEditor)
                    {
                        string filePath = selectedTab.Tag as string;
                        if (filePath == null)
                        {
                            MessageBox.Show("Files does exist");
                        }
                        else
                            Execute_File(filePath);
                    }
                }
            }
        }

        private void Execute_File(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Por favor, forneça um caminho de ficheiro válido.");
                return;
            }

            DayOfWeek wk = DateTime.Today.DayOfWeek;
            string dia = wk.ToString();

            string fileName = Path.GetFileName(filePath);
            string fileDirectory = Path.GetDirectoryName(filePath);
            string fileExtension = Path.GetExtension(filePath).ToLower();

            if (fileExtension != ".c" && fileExtension != ".cpp" && fileExtension != ".swift")
            {
                MessageBox.Show("Por favor, selecione um ficheiro C/C++ ou Swift válido.");
                return;
            }

            string outputExe = Path.Combine(fileDirectory, $"{dia}.exe");
            string compileCommand;

            // Using if-else for compatibility with C# 7.3
            if (fileExtension == ".c")
            {
                compileCommand = $"gcc -o \"{outputExe}\" \"{filePath}\"";
            }
            else if (fileExtension == ".cpp")
            {
                compileCommand = $"g++ -o \"{outputExe}\" \"{filePath}\"";
            }
            else // fileExtension == ".swift"
            {
                compileCommand = $"swiftc -o \"{outputExe}\" \"{filePath}\"";
            }

            string executeCommand = $"cmd.exe /K \"\"{outputExe}\" && pause\"";

            ProcessStartInfo compileStartInfo = new ProcessStartInfo("cmd.exe", "/C " + compileCommand)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process compileProcess = Process.Start(compileStartInfo))
                {
                    string output = compileProcess.StandardOutput.ReadToEnd();
                    string error = compileProcess.StandardError.ReadToEnd();
                    compileProcess.WaitForExit(); // Ensure the process has finished before checking output

                    if (compileProcess.ExitCode == 0) // Compilation was successful
                    {
                        ProcessStartInfo executeStartInfo = new ProcessStartInfo("cmd.exe", "/K " + executeCommand)
                        {
                            RedirectStandardOutput = false,
                            UseShellExecute = true,
                            CreateNoWindow = false
                        };

                        using (Process executeProcess = Process.Start(executeStartInfo))
                        {
                            executeProcess.WaitForExit();
                        }

                        try
                        {
                            if (fileExtension == ".swift")
                            {
                                string lib = Path.Combine(fileDirectory, $"{dia}.lib");
                                string exp = Path.Combine(fileDirectory, $"{dia}.exp");
                                File.Delete(outputExe);
                                File.Delete(lib);
                                File.Delete(exp);
                            }
                            else File.Delete(outputExe);
                        }
                        catch (Exception)
                        {
                            // Vazi de proposito
                        }
                    }
                    else // Compilation failed
                    {
                        MessageBox.Show("Compilation Failed Error:\n" + error);
                    }
                }
            }
            catch (Exception)
            {
               // Empty on porpuse
            }

            string lib1 = Path.Combine(fileDirectory, $"{dia}.lib");
            string exp1 = Path.Combine(fileDirectory, $"{dia}.exp");
            File.Delete(outputExe);
            File.Delete(lib1);
            File.Delete(exp1);

        }





        private void cima_Click()
        {
            foreach (var item in tabControl.Items)
            {
                if (item is TabItem tabItem &&
                    tabItem.Content is Grid grid &&
                    grid.Children[0] is TextEditor textEditor)
                {
                    if (int.TryParse(tamanhot.Text, out int x) && x < 50)
                    {
                        x += 1;
                        tamanhot.Text = x.ToString();
                        textEditor.FontSize += 1;
                    }
                }
            }
        }

        private void CreateNewFile_Click(object sender, RoutedEventArgs e)
        {
            // Ensure the tabControl is visible
            tabControl.Visibility = Visibility.Visible;

            TabItem newTab = new TabItem
            {
                Header = "New File  ",
                Tag = "New File  "
            };

            Grid grid = new Grid();

            int.TryParse(tamanhot.Text, out int x);

            TextEditor textEditor = new TextEditor
            {
                Text = "",
                FontSize = x,
                ShowLineNumbers = true,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E2E2E")), /*#252526*/ /*#2E2E2E*/
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white")),
                WordWrap = true

            };

            try
            {
                using (Stream stream = typeof(MainWindow).Assembly.GetManifestResourceStream("Project_Filipe_Alcaide.CustomCpp-Mode.xshd"))
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
            catch (Exception)
            {
                // Empty on purpose
            }

            grid.Children.Add(textEditor);
            newTab.Content = grid;

            tabControl.Items.Add(newTab);

            // Select the first tab or define another logic for selecting the tab
            if (tabControl.Items.Count > 0)
            {
                tabControl.SelectedIndex = 0;
            }
        }




        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedItem is TabItem selectedTab &&
                selectedTab.Content is Grid grid &&
                grid.Children[0] is TextEditor textEditor)
            {
                string filePath = selectedTab.Tag as string;
                if (filePath == null || !File.Exists(filePath))
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        filePath = saveFileDialog.FileName;
                        selectedTab.Tag = filePath;
                        selectedTab.Header = Path.GetFileName(filePath);
                    }
                }

                if (filePath != null)
                {
                    string fileContent = textEditor.Text;
                    File.WriteAllText(filePath, fileContent);
                }
            }
        }

        private void SaveFile_Click()
        {
            if (tabControl.SelectedItem is TabItem selectedTab &&
                selectedTab.Content is Grid grid &&
                grid.Children[0] is TextEditor textEditor)
            {
                string filePath = selectedTab.Tag as string;
                if (filePath == null || !File.Exists(filePath))
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        filePath = saveFileDialog.FileName;
                        selectedTab.Tag = filePath;
                        selectedTab.Header = Path.GetFileName(filePath);
                    }
                }

                if (filePath != null)
                {
                    string fileContent = textEditor.Text;
                    File.WriteAllText(filePath, fileContent);
                }
            }
        }

        private void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedItem is TabItem selectedTab &&
                selectedTab.Content is Grid grid &&
                grid.Children[0] is TextEditor textEditor)
            {
                // Initialize SaveFileDialog and set filters
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|C files (*.c)|*.c|C++ files (*.cpp)|*.cpp|C# files (*.cs)|*.cs|Swift files (*.swift)|*.swift",
                    FilterIndex = 1, // Default to C files
                    DefaultExt = ".txt" // Default file extension
                };

                // Show SaveFileDialog and check if user clicked Save
                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;
                    selectedTab.Tag = filePath;
                    selectedTab.Header = Path.GetFileName(filePath) + "  ";

                    // Ensure that the file has one of the supported extensions
                    string fileExtension = Path.GetExtension(filePath).ToLower();
                    if (fileExtension != ".c" && fileExtension != ".cpp" && fileExtension != ".cs" && fileExtension != ".txt" && fileExtension != ".swift")
                    {
                        MessageBox.Show("Unsupported file type. Please choose a valid file extension.");
                        return;
                    }

                    // Write content to the file
                    string fileContent = textEditor.Text;
                    File.WriteAllText(filePath, fileContent);
                }
            }
        }

        private void baixo_Click()
        {
            foreach (var item in tabControl.Items)
            {
                if (item is TabItem tabItem &&
                    tabItem.Content is Grid grid &&
                    grid.Children[0] is TextEditor textEditor)
                {
                    if (int.TryParse(tamanhot.Text, out int x) && x > 10)
                    {
                        x -= 1;
                        tamanhot.Text = x.ToString();
                        textEditor.FontSize -= 1;
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se a janela já está maximizada
            if (this.Width == SystemParameters.WorkArea.Width && this.Height == SystemParameters.WorkArea.Height)
            {
                // Restaura as dimensões e posição original
                this.WindowState = WindowState.Normal;
                this.Width = originalWidth;
                this.Height = originalHeight;
                this.Left = originalLeft;
                this.Top = originalTop;
            }
            else
            {
                // Salva as dimensões e posição original
                originalWidth = this.Width;
                originalHeight = this.Height;
                originalLeft = this.Left;
                originalTop = this.Top;

                // Ajusta o tamanho e posição para ocupar a área de trabalho excluindo a barra de tarefas
                this.WindowState = WindowState.Normal;
                this.Width = SystemParameters.WorkArea.Width;
                this.Height = SystemParameters.WorkArea.Height;
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void OpenTerminal(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("cmd.exe");
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., show a message to the user)
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void Execute_File(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Ficheiros C/C++ e Swift (*.c;*.cpp;*.swift)|*.c;*.cpp;*.swift|Todos os ficheiros (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                DayOfWeek wk = DateTime.Today.DayOfWeek;
                string dia = wk.ToString();

                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                string fileDirectory = Path.GetDirectoryName(filePath);
                string fileExtension = Path.GetExtension(filePath).ToLower();

                if (fileExtension != ".c" && fileExtension != ".cpp" && fileExtension != ".swift")
                {
                    MessageBox.Show("Por favor, selecione um ficheiro C/C++ ou Swift válido.");
                    return;
                }

                string outputExe = Path.Combine(fileDirectory, $"{dia}.exe");
                string compileCommand;

                // Using if-else for compatibility with C# 7.3
                if (fileExtension == ".c")
                {
                    compileCommand = $"gcc -o \"{outputExe}\" \"{filePath}\"";
                }
                else if (fileExtension == ".cpp")
                {
                    compileCommand = $"g++ -o \"{outputExe}\" \"{filePath}\"";
                }
                else // fileExtension == ".swift"
                {
                    compileCommand = $"swiftc -o \"{outputExe}\" \"{filePath}\"";
                }

                string executeCommand = $"cmd.exe /K \"\"{outputExe}\" && pause\"";

                ProcessStartInfo compileStartInfo = new ProcessStartInfo("cmd.exe", "/C " + compileCommand)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                try
                {
                    using (Process compileProcess = Process.Start(compileStartInfo))
                    {
                        string output = compileProcess.StandardOutput.ReadToEnd();
                        string error = compileProcess.StandardError.ReadToEnd();
                        compileProcess.WaitForExit(); // Ensure the process has finished before checking output

                        if (compileProcess.ExitCode == 0) // Compilation was successful
                        {
                            ProcessStartInfo executeStartInfo = new ProcessStartInfo("cmd.exe", "/K " + executeCommand)
                            {
                                RedirectStandardOutput = false,
                                UseShellExecute = true,
                                CreateNoWindow = false
                            };

                            using (Process executeProcess = Process.Start(executeStartInfo))
                            {
                                executeProcess.WaitForExit();
                            }

                            try
                            {
                                if( fileExtension == ".swift")
                                {
                                    string lib = Path.Combine(fileDirectory, $"{dia}.lib");
                                    string exp = Path.Combine(fileDirectory, $"{dia}.exp");
                                    File.Delete(outputExe);
                                    File.Delete(lib);
                                    File.Delete(exp);
                                }
                                else File.Delete(outputExe);

                            }
                            catch (Exception)
                            {
                                // Vazio de proposito
                            }
                        }
                        else // Compilation failed
                        {
                            MessageBox.Show("Compilation Failed Error:\n" + error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred: " + ex.Message);
                }

                string lib1 = Path.Combine(fileDirectory, $"{dia}.lib");
                string exp1 = Path.Combine(fileDirectory, $"{dia}.exp");
                File.Delete(outputExe);
                File.Delete(lib1);
                File.Delete(exp1);

            }
        }



        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            // Ensure the tabControl is visible
            tabControl.Visibility = Visibility.Visible;

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true // Permite a seleção de múltiplos ficheiros
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    string fileContent = File.ReadAllText(filePath);

                    TabItem newTab = new TabItem
                    {
                        Header = System.IO.Path.GetFileName(filePath) + "  ",
                        Tag = filePath
                    };

                    Grid grid = new Grid();
                    int.TryParse(tamanhot.Text, out int x);
                    TextEditor textEditor = new TextEditor
                    {
                        Text = fileContent,
                        FontSize = x,
                        ShowLineNumbers = true,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E2E2E")),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white")),
                        WordWrap = true
                    };

                    // Set custom syntax highlighting for C/C++
                    if (Path.GetExtension(filePath).Equals(".c", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(filePath).Equals(".cpp", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(filePath).Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(filePath).Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(filePath).Equals(".swift", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using (Stream stream = typeof(MainWindow).Assembly.GetManifestResourceStream("Swift_for_Windows.CustomCpp-Mode.xshd"))
                            using (XmlReader reader = XmlReader.Create(stream))
                            {
                                textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    grid.Children.Add(textEditor);
                    newTab.Content = grid;

                    tabControl.Items.Add(newTab);
                }

                // Seleciona a primeira aba ou outra lógica para definir a aba selecionada
                if (tabControl.Items.Count > 0)
                {
                    tabControl.SelectedIndex = 0;
                }
            }
        }

        private void CloseTab(object sender, RoutedEventArgs e)
        {
            if (sender is Button closeButton)
            {
                var tabItem = closeButton.DataContext as TabItem;
                if (tabItem != null)
                {
                    tabControl.Items.Remove(tabItem);
                }
            }
        }

        private void OpenFileExplorer(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe");
        }


    }
}
