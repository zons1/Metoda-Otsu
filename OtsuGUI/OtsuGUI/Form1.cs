using System.Diagnostics;
using System.Drawing.Imaging;

namespace OtsuGui;

public partial class Form1 : Form
{
    private readonly Button btnLoad = new() { Text = "Wczytaj obraz", AutoSize = true };
    private readonly Button btnProcess = new() { Text = "Przetwórz", AutoSize = true, Enabled = false };
    private readonly Button btnSave = new() { Text = "Zapisz wynik", AutoSize = true, Enabled = false };
    private readonly Button btnBenchmark = new() { Text = "Pomiary (wkrótce)", AutoSize = true, Enabled = false };
    private readonly ComboBox cbLibrary = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
    private readonly NumericUpDown numThreads = new() { Minimum = 1, Maximum = 64, Width = 60 };
    private readonly Label lblTime = new() { Text = "Czas: -", AutoSize = true };
    private readonly PictureBox picOriginal = CreatePictureBox();
    private readonly PictureBox picResult = CreatePictureBox();
    private readonly DataGridView gridResults = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = SystemColors.Window
    };

    private string currentFileName = "";

    // Konstruktor bez parametrów zostaje dla designera
    public Form1() : this(null) { }

    public Form1(int? threads)
    {
        InitializeComponent();

        Text = "Otsu - progowanie obrazu";
        MinimumSize = new Size(900, 600);
        Size = new Size(1150, 750);

        cbLibrary.Items.AddRange(new object[] { "C++", "ASM" });
        cbLibrary.SelectedIndex = 0;

        // Parametr programu, a jeœli go brak: liczba procesorów logicznych (zakres 1-64)
        numThreads.Value = Math.Clamp(threads ?? Environment.ProcessorCount, 1, 64);

        gridResults.Columns.Add("file", "Plik");
        gridResults.Columns.Add("lib", "Biblioteka");
        gridResults.Columns.Add("threads", "W¹tki");
        gridResults.Columns.Add("time", "Czas [ms]");

        lblTime.Margin = new Padding(15, 8, 0, 0);

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(5)
        };
        topPanel.Controls.AddRange(new Control[]
        {
            btnLoad,
            new Label { Text = "Biblioteka:", AutoSize = true, Margin = new Padding(15, 8, 0, 0) },
            cbLibrary,
            new Label { Text = "W¹tki:", AutoSize = true, Margin = new Padding(15, 8, 0, 0) },
            numThreads,
            btnProcess,
            btnSave,
            btnBenchmark,
            lblTime
        });

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));

        layout.Controls.Add(topPanel, 0, 0);
        layout.SetColumnSpan(topPanel, 2);
        layout.Controls.Add(new Label { Text = "Orygina³", AutoSize = true, Padding = new Padding(5) }, 0, 1);
        layout.Controls.Add(new Label { Text = "Wynik (Otsu)", AutoSize = true, Padding = new Padding(5) }, 1, 1);
        layout.Controls.Add(picOriginal, 0, 2);
        layout.Controls.Add(picResult, 1, 2);
        layout.Controls.Add(gridResults, 0, 3);
        layout.SetColumnSpan(gridResults, 2);

        Controls.Add(layout);

        btnLoad.Click += BtnLoad_Click;
        btnProcess.Click += BtnProcess_Click;
        btnSave.Click += BtnSave_Click;
    }

    private static PictureBox CreatePictureBox() => new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.WhiteSmoke
    };

    private void BtnLoad_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog { Filter = "Obrazy BMP|*.bmp|Wszystkie pliki|*.*" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            // Wczytanie i konwersja do 24 bpp (BGR), plik nie zostaje zablokowany
            using var tmp = new Bitmap(dlg.FileName);
            var bmp = tmp.Clone(new Rectangle(0, 0, tmp.Width, tmp.Height), PixelFormat.Format24bppRgb);

            picOriginal.Image?.Dispose();
            picOriginal.Image = bmp;
            picResult.Image?.Dispose();
            picResult.Image = null;

            currentFileName = Path.GetFileName(dlg.FileName);
            lblTime.Text = "Czas: -";
            btnProcess.Enabled = true;
            btnSave.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Nie uda³o siê wczytaæ obrazu:\n" + ex.Message, "B³¹d");
        }
    }

    private void BtnProcess_Click(object? sender, EventArgs e)
    {
        if (picOriginal.Image is not Bitmap source) return;

        string lib = cbLibrary.SelectedItem?.ToString() ?? "";
        int threads = (int)numThreads.Value;

        var sw = Stopwatch.StartNew();
        // TODO (etap B): tutaj wywo³anie biblioteki C++ / ASM przez P/Invoke.
        // Na razie tylko kopia obrazu, ¿eby sprawdziæ ca³y przep³yw.
        var result = (Bitmap)source.Clone();
        sw.Stop();

        double ms = sw.Elapsed.TotalMilliseconds;

        picResult.Image?.Dispose();
        picResult.Image = result;

        lblTime.Text = $"Czas: {ms:F3} ms";
        gridResults.Rows.Add(currentFileName, lib, threads, ms.ToString("F3"));
        btnSave.Enabled = true;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (picResult.Image is null) return;

        using var dlg = new SaveFileDialog { Filter = "Obraz BMP|*.bmp", DefaultExt = "bmp" };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            picResult.Image.Save(dlg.FileName, ImageFormat.Bmp);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Nie uda³o siê zapisaæ obrazu:\n" + ex.Message, "B³¹d");
        }
    }
}