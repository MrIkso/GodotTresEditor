using System.Text;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using GodotTresEditor.Core;
using GodotTresEditor.Core.Models;
using GodotTresEditor.Utilities.Extensions;

namespace GodotTresEditor
{
    public partial class MainForm : Form
    {
        private TresData tresData;
        private string loadedTresPath;
        private OpenedContentType openedContentType = OpenedContentType.Unknown;

        public MainForm()
        {
            InitializeComponent();
            UpdateTile();
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "TRES Files (*.tres)|*.tres|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                loadedTresPath = openFileDialog.FileName;
                await LoadTresAsync(loadedTresPath);
            }
        }

        private async Task LoadTresAsync(string tresPath)
        {
            if (string.IsNullOrWhiteSpace(tresPath))
                return;

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                UpdateTile();
                tresData = await Task.Run(() => TresParser.Parse(tresPath));
                ShowTresText();
                updateContentToolStripMenuItem.Enabled = true;
                extractDataToolStripMenuItem.Enabled = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load content.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void updateTextContentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (openedContentType)
            {
                case OpenedContentType.OptimizedTranslation:
                    UpdateTextContent();
                    ShowTresText();
                    break;
                case OpenedContentType.FontFile:
                    UpdateFontFile();
                    ShowTresText();
                    break;
                default:
                    MessageBox.Show($"Unable to update content. Unsupported TRES type: {tresData.BaseType}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void ShowTresText()
        {
            if (string.IsNullOrWhiteSpace(loadedTresPath))
                return;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                if (tresData.BaseType == "OptimizedTranslation")
                {
                    openedContentType = OpenedContentType.OptimizedTranslation;
                }
                if (tresData.BaseType == "FontFile")
                {
                    openedContentType = OpenedContentType.FontFile;
                }
                string data = File.ReadAllText(loadedTresPath);
                richTextBox.Text = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load TRES: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void extractDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var outputFilePath = string.Empty;
            switch (openedContentType)
            {
                case OpenedContentType.OptimizedTranslation:
                    var translationKeys = OptimizedTranslationParser.GetTranslatedMessages(tresData);
                    outputFilePath = Path.ChangeExtension(loadedTresPath, ".csv");
                    WriteCSV(outputFilePath, translationKeys);
                    MessageBox.Show("Translation data extracted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;

                case OpenedContentType.FontFile:
                    openedContentType = OpenedContentType.FontFile;
                    byte[] font = tresData.GetProperty<byte[]>("data");
                    outputFilePath = Path.ChangeExtension(loadedTresPath, ".ttf");
                    File.WriteAllBytes(outputFilePath, font);
                    MessageBox.Show("Font file extracted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;

                default:
                    MessageBox.Show($"Unable extract data. Unsupported TRES type: {tresData.BaseType}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;

            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void WriteCSV(string filePath, IEnumerable<string> lines)
        {
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            using var csv = new CsvWriter(writer, config);

            long index = 0;
            foreach (var line in lines)
            {
                csv.WriteField(index);
                csv.WriteField(StringExtentions.ConvertNewlinesToMarkers(line));
                csv.NextRecord();
                index++;
            }

            writer.Flush();
        }

        private void UpdateFontFile()
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Font Files (*.ttf)|*.ttf";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fontFilePath = openFileDialog.FileName;
                var fontData = File.ReadAllBytes(fontFilePath);
                TresUpdater.UpdateFontFile(loadedTresPath, fontData, tresData.Format);
                MessageBox.Show("Font file updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateTextContent()
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Csv Files (*.csv)|*.csv";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var textFilePath = openFileDialog.FileName;

                var editedStrings = new List<string>();
                using (var reader = new StreamReader(textFilePath, new UTF8Encoding(false)))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    while (csv.Read())
                    {
                        var index = csv.GetField<long>(0);
                        var text = csv.GetField<string>(1);
                        editedStrings.Add(StringExtentions.ConvertMarkersToNewlines(text));
                    }
                }

                var updatedData = TresUpdater.GenEditedStrings(tresData, editedStrings);
                TresUpdater.UpdateTranslationFile(loadedTresPath, updatedData, tresData.Format);
                MessageBox.Show("Translation file updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateTile()
        {
            if (!string.IsNullOrWhiteSpace(loadedTresPath))
            {
                this.Text = $"Godot TRES Editor - {Path.GetFileName(loadedTresPath)}";
            }
            else
            {
                this.Text = "Godot TRES Editor";
            }
        }
    }
}
