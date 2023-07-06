using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Text;

namespace sysDetails
{
    public partial class SystemDetails : Page
    {
        public SystemDetails()
        {
            InitializeComponent();
        }

        private async void RunScan_Click(object sender, RoutedEventArgs e)
        {
            string powershellScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sysDetails.ps1");

            if (File.Exists(powershellScriptPath))
            {
                // Show loading overlay
                ShowLoadingOverlay();

                // Run PowerShell script asynchronously and generate CSV file
                bool scriptExecutionResult = await RunPowerShellScriptAsync();

                if (scriptExecutionResult)
                {
                    // Read the CSV file and populate the data grid
                    ReadCSVFile();
                }

                // Hide loading overlay
                HideLoadingOverlay();
            }
            else
            {
                MessageBox.Show("PowerShell script file not found!");
            }
        }

        private async Task<bool> RunPowerShellScriptAsync()
        {
            try
            {
                string powershellScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sysDetails.ps1");

                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{powershellScriptPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Verb = "runas";

                process.Start();

                // Wait asynchronously for the process to exit
                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to execute PowerShell script: {ex.Message}");
                return false;
            }
        }

        private void ReadCSVFile()
        {
            string csvFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sysDetails.csv");

            try
            {
                if (File.Exists(csvFilePath))
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
                else
                {
                    MessageBox.Show("CSV file not found!");
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
                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
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

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a StringBuilder to store the data
                StringBuilder stringBuilder = new StringBuilder();

                // Add column headers and data values to the StringBuilder
                foreach (var column in dataGrid.Columns)
                {
                    stringBuilder.Append($"{column.Header.ToString()} : ");

                    foreach (var item in dataGrid.Items)
                    {
                        var row = item as string[];
                        int columnIndex = dataGrid.Columns.IndexOf(column);
                        stringBuilder.Append($"{row[columnIndex]}, ");
                    }

                    stringBuilder.AppendLine();
                }

                // Create a PrintDialog
                PrintDialog printDialog = new PrintDialog();

                // Check if the user wants to print
                if (printDialog.ShowDialog() == true)
                {
                    // Create a FixedDocument
                    FixedDocument fixedDocument = new FixedDocument();

                    // Set the size of the FixedDocument to match the PrintDialog printable area
                    fixedDocument.DocumentPaginator.PageSize = new Size(printDialog.PrintableAreaWidth, printDialog.PrintableAreaHeight);

                    // Create a PageContent and FixedPage for the content
                    PageContent pageContent = new PageContent();
                    FixedPage fixedPage = new FixedPage();
                    fixedPage.Width = fixedDocument.DocumentPaginator.PageSize.Width;
                    fixedPage.Height = fixedDocument.DocumentPaginator.PageSize.Height;

                    // Create a TextBlock to display the data
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = stringBuilder.ToString();
                    textBlock.Margin = new Thickness(20);

                    // Add the TextBlock to the FixedPage
                    fixedPage.Children.Add(textBlock);

                    // Add the FixedPage to the PageContent
                    ((IAddChild)pageContent).AddChild(fixedPage);

                    // Add the PageContent to the FixedDocument
                    fixedDocument.Pages.Add(pageContent);

                    // Send the FixedDocument to the printer
                    printDialog.PrintDocument(fixedDocument.DocumentPaginator, "Print Data");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to print data: {ex.Message}");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Close the application
            Application.Current.Shutdown();
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