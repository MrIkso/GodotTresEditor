using System.Text;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using GodotTresEditor.Core;
using GodotTresEditor.Core.Models;
using GodotTresEditor.Utilities.Extensions;
using GodotTresEditor.Utilities;

namespace GodotTresEditor
{
    public partial class MainForm : Form
    {
        private TresData tresData;
        private string loadedResourcePath;
        private OpenedContentType openedContentType = OpenedContentType.Unknown;
        private TextureParser textureParser;
        private TextureResult textureResult;

        public MainForm()
        {
            InitializeComponent();
            UpdateTile();
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "TRES Files (*.tres)|*.tres|Image Files (*.ctex;*.stex)|*.ctex;*.stex|All Files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadFile(openFileDialog.FileName);
            }
        }

        private void LoadFile(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (extension.Contains(".tres"))
            {
                loadedResourcePath = filePath;
                _ = LoadTresAsync(loadedResourcePath);
            }
            else if (extension.Contains(".ctex") || extension.Contains(".stex"))
            {
                loadedResourcePath = filePath;
                ReadTexture(loadedResourcePath);             
            }
            else
            {
                MessageBox.Show("Unsupported file type.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private async void ReadTexture(string texturePath)
        {
            UpdateTile();
            updateContentToolStripMenuItem.Enabled = true;
            extractDataToolStripMenuItem.Enabled = true;
            openedContentType = OpenedContentType.Texture;
            textureParser = new TextureParser();
            string? outputFilePath = null;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                byte[] textureData = await File.ReadAllBytesAsync(texturePath);
                textureResult = await Task.Run(() => textureParser.DecompressTexture(textureData));

                if (textureResult == null)
                    throw new InvalidOperationException("Texture parser returned null result.");
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private async void ExtractTexture(string texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
            {
                return;
            }
            string? outputFilePath = null;

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                outputFilePath = Path.ChangeExtension(texturePath, textureResult.Extension);
                await File.WriteAllBytesAsync(outputFilePath, textureResult.Data);

                MessageBox.Show($"Texture extracted successfully to {outputFilePath}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to extract texture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void updateContentToolStripMenuItem_Click(object sender, EventArgs e)
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
                case OpenedContentType.Texture:
                    ReplaceTexture();
                    break;
                default:
                    MessageBox.Show($"Unable to update content. Unsupported resource type: {tresData.BaseType}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private async Task ReplaceTexture()
        {
            if (string.IsNullOrEmpty(loadedResourcePath))
            {
                MessageBox.Show("No Godot texture resource is currently loaded.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png;*.webp)|*.png;*.webp|PNG Files (*.png)|*.png|WebP Files (*.webp)|*.webp";
            openFileDialog.Title = "Select Replacement Image";

            var initialDir = Path.GetDirectoryName(loadedResourcePath);
            if (Directory.Exists(initialDir))
            {
                openFileDialog.InitialDirectory = initialDir;
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var imagePath = openFileDialog.FileName;
                this.Cursor = Cursors.WaitCursor;
                var result = await Task.Run(() => ReplaceTexureWorker(imagePath));
                this.Cursor = Cursors.Default;

                if (result.success)
                {
                    MessageBox.Show(
                        result.message,
                        "Import Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"Failed to replace texture.\n\nDetails: {result.message}",
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private (bool success, string message) ReplaceTexureWorker(string texturePath)
        {
            try
            {
                if (!File.Exists(texturePath))
                    return (false, "The selected image file no longer exists.");

                byte[] imageBytes = File.ReadAllBytes(texturePath);
                int width = 0;
                int height = 0;
                bool isWebp = false;
                string extension = Path.GetExtension(texturePath).ToLower();
                if (extension.Contains(".png"))
                {
                    isWebp = true;
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        try
                        {
                            using (var img = System.Drawing.Image.FromStream(ms, false, false))
                            {
                                width = img.Width;
                                height = img.Height;
                            }
                        }
                        catch (Exception ex)
                        {
                            return (false, $"Error: {ex.Message}");
                        }
                    }
                }
                else if (extension.Contains(".webp"))
                {
                    isWebp = true;
                    var dims = ImageUtils.GetWebpDimensions(imageBytes);
                    width = dims.w;
                    height = dims.h;
                }

                if (width == 0 || height == 0)
                {
                    return (false, "Could not read image dimensions. The file may be corrupted or format is not supported.");
                }

                byte[] newImageData = null;
                if (textureResult.GodotVersion == TextureParser.GodotVersion.V4)
                {
                    newImageData = textureParser.CreateCtexV4(imageBytes, width, height, isWebp);
                }
                else
                {
                    newImageData = textureParser.CreateStexV3(imageBytes, width, height, isWebp);
                }
                File.WriteAllBytes(loadedResourcePath, newImageData);
                return (true, $"Successfully replaced with {width}x{height} {(isWebp ? "WebP" : "PNG")} image.");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        private void ShowTresText()
        {
            if (string.IsNullOrWhiteSpace(loadedResourcePath))
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
                string data = File.ReadAllText(loadedResourcePath);
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
                    outputFilePath = Path.ChangeExtension(loadedResourcePath, ".csv");
                    WriteCSV(outputFilePath, translationKeys);
                    MessageBox.Show("Translation data extracted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;

                case OpenedContentType.FontFile:
                    openedContentType = OpenedContentType.FontFile;
                    byte[] font = tresData.GetProperty<byte[]>("data");
                    outputFilePath = Path.ChangeExtension(loadedResourcePath, ".ttf");
                    File.WriteAllBytes(outputFilePath, font);
                    MessageBox.Show("Font file extracted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                case OpenedContentType.Texture:
                    ExtractTexture(loadedResourcePath);
                    break;
                default:
                    MessageBox.Show($"Unable extract data. Unsupported resource type: {tresData.BaseType}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            openFileDialog.InitialDirectory = Path.GetDirectoryName(loadedResourcePath);
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fontFilePath = openFileDialog.FileName;
                var fontData = File.ReadAllBytes(fontFilePath);
                TresUpdater.UpdateFontFile(loadedResourcePath, fontData, tresData.Format);
                MessageBox.Show("Font file updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateTextContent()
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Csv Files (*.csv)|*.csv";
            openFileDialog.InitialDirectory = Path.GetDirectoryName(loadedResourcePath);
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
                TresUpdater.UpdateTranslationFile(loadedResourcePath, updatedData, tresData.Format);
                MessageBox.Show("Translation file updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void UpdateTile()
        {
            if (!string.IsNullOrWhiteSpace(loadedResourcePath))
            {
                this.Text = $"Godot TRES Editor - {Path.GetFileName(loadedResourcePath)} - {GetApplicationVersion()}";
            }
            else
            {
                this.Text = $"Godot TRES Editor - {GetApplicationVersion()}";
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[]? filePaths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (filePaths == null || filePaths.Length == 0)
            {
                return;
            }

            var filePath = filePaths[0];

            if (File.Exists(filePath))
            {
                LoadFile(filePath);
            }

        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        public static string GetApplicationVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
