using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Security.Principal;
using System.Text;

namespace sysDetails
{
    public partial class ADReport : Page
    {
        private string fileDirectory = "C:\\NetworkReports"; // Replace with the actual file directory

        public ADReport()
        {
            InitializeComponent();
        }

        private void PopulateFileList()
        {
            try
            {
                List<string> fileNames = Directory.GetFiles(fileDirectory, "*.csv")
                    .Select(Path.GetFileNameWithoutExtension)
                    .ToList();

                fileListBox.ItemsSource = fileNames;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to populate file list: {ex.Message}");
            }
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedFileName = fileListBox.SelectedItem as string;
            if (!string.IsNullOrEmpty(selectedFileName))
            {
                string csvFilePath = Path.Combine(fileDirectory, $"{selectedFileName}.csv");
                if (File.Exists(csvFilePath))
                {
                    ReadCSVFile(csvFilePath);
                }
                else
                {
                    MessageBox.Show("CSV file not found!");
                }
            }
        }

        private void ReadCSVFile(string csvFilePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(csvFilePath);

                if (lines.Length > 0)
                {
                    string[] headers = lines[0].Split(',');

                    // Remove double quotes from column headers
                    for (int i = 0; i < headers.Length; i++)
                    {
                        headers[i] = headers[i].Trim('\"');
                    }

                    // Create columns dynamically
                    dataGrid.Columns.Clear();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        DataGridTextColumn column = new DataGridTextColumn();
                        column.Header = headers[i];
                        column.Binding = new System.Windows.Data.Binding($"[{i}]");
                        dataGrid.Columns.Add(column);
                    }

                    // Clear existing items in the DataGrid
                    dataGrid.Items.Clear();

                    // Populate rows
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string[] fields = lines[i].Split(',');

                        // Remove double quotes from each field
                        for (int j = 0; j < fields.Length; j++)
                        {
                            fields[j] = fields[j].Trim('\"');
                        }

                        dataGrid.Items.Add(fields);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to read CSV file: {ex.Message}");
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Prompt user to select a file to save the data
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Open the selected file for writing
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                    {
                        // Write the column headers
                        foreach (var column in dataGrid.Columns)
                        {
                            writer.Write(column.Header);
                            writer.Write(",");
                        }
                        writer.WriteLine();

                        // Write the data rows
                        foreach (var item in dataGrid.Items)
                        {
                            var row = item as string[];
                            foreach (var field in row)
                            {
                                writer.Write(field);
                                writer.Write(",");
                            }
                            writer.WriteLine();
                        }
                    }

                    MessageBox.Show("Data saved successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save data: {ex.Message}");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Close the application
            Application.Current.Shutdown();
        }

        private void ConfigurePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Folder or File",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "SelectFolder",
                Filter = "All Files|*.*",
                ValidateNames = false
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                // Assign the selected path to the fileDirectory variable or use it as needed
                fileDirectory = selectedPath;
            }
            else
            {
                // User canceled the operation
            }
        }

        private async void AutoConfigure_Click(object sender, RoutedEventArgs e)
        {
            // Run the PowerShell script
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "createSharedFolder.ps1");
            string outputFilePath = Path.GetTempFileName();
            bool success = await RunPowerShellScriptAsync(scriptPath, outputFilePath);

            if (success)
            {
                string output = File.ReadAllText(outputFilePath);
                MessageBox.Show(output, "Auto Configure Output", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to execute PowerShell script.", "Auto Configure", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            File.Delete(outputFilePath);
        }

        private async Task<bool> RunPowerShellScriptAsync(string scriptPath, string outputFilePath)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{scriptPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Verb = "runas";

                // Create a StringBuilder to store the output
                StringBuilder outputBuilder = new StringBuilder();

                // Redirect the standard output and handle the OutputDataReceived event
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                // Start the PowerShell process
                process.Start();

                // Begin asynchronous reading of the standard output
                process.BeginOutputReadLine();

                // Wait asynchronously for the process to exit
                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    // Write the captured output to the file
                    File.WriteAllText(outputFilePath, outputBuilder.ToString());
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to execute PowerShell script: {ex.Message}", "Auto Configure", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void RunScan_Click(object sender, RoutedEventArgs e)
        {
            ShowLoadingOverlay();
            PopulateFileList();
            HideLoadingOverlay();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchBox.Text.ToLower();

            if (fileListBox != null)
            {
                if (string.IsNullOrEmpty(searchText))
                {
                    // If search text is empty, display all files
                    fileListBox.ItemsSource = Directory.GetFiles(fileDirectory, "*.csv")
                        .Select(Path.GetFileNameWithoutExtension)
                        .ToList();
                }
                else
                {
                    // Filter files based on search text
                    List<string> filteredFileNames = new List<string>();

                    foreach (string fileName in fileListBox.Items)
                    {
                        if (fileName.ToLower().Contains(searchText))
                        {
                            filteredFileNames.Add(fileName);
                        }
                    }

                    fileListBox.ItemsSource = filteredFileNames;
                }
            }
        }


        private void ShowLoadingOverlay()
        {
            loadingOverlay.Visibility = Visibility.Visible;
        }

        private void HideLoadingOverlay()
        {
            loadingOverlay.Visibility = Visibility.Collapsed;
        }
    }
}
