using System.Text;
using System.Globalization;
using System.Reflection;
using CsvHelper;
using CsvHelper.Configuration;
using GodotTresEditor.Core;
using GodotTresEditor.Core.Models;
using GodotTresEditor.Utilities.Extensions;
using GodotTresEditor.Utilities;

namespace GodotTresEditor;

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

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Tres Files (*.tres)|*.tres|Image Files (*.ctex;*.stex)|*.ctex;*.stex|All Files (*.*)|*.*";
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
            SetLoadingState(true);
            UpdateTile();
            tresData = await Task.Run(() => TresParser.Parse(tresPath));
            await ShowTresText();
            updateContentToolStripMenuItem.Enabled = true;
            extractDataToolStripMenuItem.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to load content: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void ReadTexture(string texturePath)
    {
        UpdateTile();
        updateContentToolStripMenuItem.Enabled = true;
        extractDataToolStripMenuItem.Enabled = true;
        openedContentType = OpenedContentType.Texture;
        textureParser = new TextureParser();

        try
        {
            SetLoadingState(true);
            byte[] textureData = await File.ReadAllBytesAsync(texturePath);
            textureResult = await Task.Run(() => textureParser.DecompressTexture(textureData));

            if (textureResult == null)
                throw new InvalidOperationException("Texture parser returned null result.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to read texture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void updateContentToolStripMenuItem_Click(object sender, EventArgs e)
    {
        switch (openedContentType)
        {
            case OpenedContentType.OptimizedTranslation:
                UpdateTextContent();
                await ShowTresText();
                break;
            case OpenedContentType.FontFile:
                UpdateFontFile();
                await ShowTresText();
                break;
            case OpenedContentType.Texture:
                await ReplaceTexture();
                break;
            case OpenedContentType.InkResource:
                await ReplaceInkJson();
                break;
            default:
                MessageBox.Show($"Unable to update content. Unsupported resource type: {tresData?.BaseType}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            SetLoadingState(true);
            var result = await Task.Run(() => ReplaceTexureWorker(imagePath));
            SetLoadingState(false);

            if (result.success)
            {
                MessageBox.Show(result.message, "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Failed to replace texture.\n\nDetails: {result.message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                using var ms = new MemoryStream(imageBytes);
                try
                {
                    using var img = Image.FromStream(ms, false, false);
                    width = img.Width;
                    height = img.Height;
                }
                catch (Exception ex)
                {
                    return (false, $"Error processing PNG: {ex.Message}");
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

            byte[] newImageData = textureResult.GodotVersion == TextureParser.GodotVersion.V4
                ? textureParser.CreateCtexV4(imageBytes, width, height, isWebp)
                : textureParser.CreateStexV3(imageBytes, width, height, isWebp);

            File.WriteAllBytes(loadedResourcePath, newImageData);
            return (true, $"Successfully replaced with {width}x{height} {(isWebp ? "WebP" : "PNG")} image.");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    private async Task ReplaceInkJson()
    {
        if (string.IsNullOrEmpty(loadedResourcePath))
        {
            MessageBox.Show("No Godot resource is currently loaded.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Json Files (*.json)|*.json|All Files (*.*)|*.*";
        openFileDialog.Title = "Select Replacement json File";

        var initialDir = Path.GetDirectoryName(loadedResourcePath);
        if (Directory.Exists(initialDir))
        {
            openFileDialog.InitialDirectory = initialDir;
        }

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            var jsonPath = openFileDialog.FileName;
            SetLoadingState(true);

            try
            {
                string newJsonContent = await File.ReadAllTextAsync(jsonPath);
                string escapedJson = newJsonContent.EscapeString();

                bool success = await Task.Run(() => TresUpdater.UpdateTresProperty(loadedResourcePath, "json", escapedJson));

                if (success)
                {
                    tresData = await Task.Run(() => TresParser.Parse(loadedResourcePath));
                    await ShowTresText();

                    MessageBox.Show("InkResource json replaced successfully.", "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to find 'json' property in the tres file.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to replace json.\n\nDetails: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }
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
                    var text = csv.GetField<string>(1);
                    editedStrings.Add(StringExtentions.ConvertMarkersToNewlines(text));
                }
            }

            var updatedData = TresUpdater.GenEditedStrings(tresData, editedStrings);
            TresUpdater.UpdateTranslationFile(loadedResourcePath, updatedData, tresData.Format);
            MessageBox.Show("Translation file updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }


    private async void extractDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        try
        {
            SetLoadingState(true);
            var outputFilePath = string.Empty;

            switch (openedContentType)
            {
                case OpenedContentType.OptimizedTranslation:
                    var translationKeys = OptimizedTranslationParser.GetTranslatedMessages(tresData);
                    outputFilePath = Path.ChangeExtension(loadedResourcePath, ".csv");
                    WriteCSV(outputFilePath, translationKeys);
                    MessageBox.Show("Translation data extracted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case OpenedContentType.FontFile:
                    byte[] font = tresData.GetProperty<byte[]>("data");
                    outputFilePath = Path.ChangeExtension(loadedResourcePath, ".ttf");
                    await File.WriteAllBytesAsync(outputFilePath, font);
                    MessageBox.Show("Font file extracted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                case OpenedContentType.Texture:
                    ExtractTexture(loadedResourcePath);
                    break;

                case OpenedContentType.InkResource:
                    string json = tresData.GetProperty<string>("json");
                    outputFilePath = Path.ChangeExtension(loadedResourcePath, ".json");
                    await File.WriteAllTextAsync(outputFilePath, json);
                    MessageBox.Show("InkResource extracted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                default:
                    MessageBox.Show($"Unable to extract data. Unsupported resource type: {tresData.BaseType}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to extract data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private async void ExtractTexture(string texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
            return;

        try
        {
            string outputFilePath = Path.ChangeExtension(texturePath, textureResult.Extension);
            await File.WriteAllBytesAsync(outputFilePath, textureResult.Data);
            MessageBox.Show($"Texture extracted successfully to {outputFilePath}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to extract texture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void WriteCSV(string filePath, IEnumerable<string> lines)
    {
        if (filePath is null) throw new ArgumentNullException(nameof(filePath));
        if (lines is null) throw new ArgumentNullException(nameof(lines));

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false };

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


    private async Task ShowTresText()
    {
        if (string.IsNullOrWhiteSpace(loadedResourcePath))
            return;

        try
        {
            SetLoadingState(true);

            if (tresData != null)
            {
                if (tresData.BaseType == "OptimizedTranslation")
                {
                    openedContentType = OpenedContentType.OptimizedTranslation;
                }
                else if (tresData.BaseType == "FontFile")
                {
                    openedContentType = OpenedContentType.FontFile;
                }
                else if (tresData.BaseType == "Resource" && tresData.ScriptClass == "InkResource")
                {
                    openedContentType = OpenedContentType.InkResource;
                }
            }

            string data = await File.ReadAllTextAsync(loadedResourcePath);

            string normalizedData = data.Replace("\r\n", "\n")
                                        .Replace("\r", "\n")
                                        .Replace("\n", Environment.NewLine);

            textBox.SuspendLayout();
            textBox.WordWrap = normalizedData.Length < 1500;
            textBox.Text = normalizedData;
            textBox.DeselectAll();
            textBox.ResumeLayout();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to load tres text: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        this.UseWaitCursor = isLoading;
        Cursor.Current = isLoading ? Cursors.WaitCursor : Cursors.Default;
    }

    private void UpdateTile()
    {
        string fileName = !string.IsNullOrWhiteSpace(loadedResourcePath) ? Path.GetFileName(loadedResourcePath) + " - " : "";
        this.Text = $"Godot tres Editor - {fileName}{GetApplicationVersion()}";
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    public static string GetApplicationVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }


    private void MainForm_DragDrop(object sender, DragEventArgs e)
    {
        if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        string[]? filePaths = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (filePaths != null && filePaths.Length > 0 && File.Exists(filePaths[0]))
        {
            LoadFile(filePaths[0]);
        }
    }

    private void MainForm_DragEnter(object sender, DragEventArgs e)
    {
        e.Effect = (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void TextBox_Enter(object? sender, EventArgs e)
    {
        this.BeginInvoke(new Action(() =>
        {
            textBox.SelectionLength = 0;
        }));
    }
}