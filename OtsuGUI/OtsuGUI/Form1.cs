namespace OtsuGui;

public partial class Form1 : Form
{
    private readonly Button btnLoad = new() { Text = "Wczytaj obraz", AutoSize = true };
    private readonly Button btnProcess = new() { Text = "Przetwórz", AutoSize = true, Enabled = false };
    private readonly ComboBox cbLibrary = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 80 };
    private readonly NumericUpDown numThreads = new() { Minimum = 1, Maximum = 64, Width = 60 };
    private readonly Label lblTime = new() { Text = "Czas: -", AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly PictureBox picOriginal = CreatePictureBox();
    private readonly PictureBox picResult = CreatePictureBox();

    public Form1()
    {
        InitializeComponent();

        Text = "Otsu - progowanie obrazu";
        MinimumSize = new Size(800, 500);
        Size = new Size(1100, 650);

        cbLibrary.Items.AddRange(new object[] { "C++", "ASM" });
        cbLibrary.SelectedIndex = 0;

        // Domyœlnie liczba w¹tków = liczba procesorów logicznych (zakres 1-64)
        numThreads.Value = Math.Clamp(Environment.ProcessorCount, 1, 64);

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
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
            lblTime
        });
        lblTime.Margin = new Padding(15, 8, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(topPanel, 0, 0);
        layout.SetColumnSpan(topPanel, 2);
        layout.Controls.Add(new Label { Text = "Orygina³", AutoSize = true, Padding = new Padding(5) }, 0, 1);
        layout.Controls.Add(new Label { Text = "Wynik (Otsu)", AutoSize = true, Padding = new Padding(5) }, 1, 1);
        layout.Controls.Add(picOriginal, 0, 2);
        layout.Controls.Add(picResult, 1, 2);

        Controls.Add(layout);

        btnLoad.Click += BtnLoad_Click;
        btnProcess.Click += BtnProcess_Click;
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

        picOriginal.Image?.Dispose();
        picOriginal.Image = new Bitmap(dlg.FileName);
        picResult.Image = null;
        lblTime.Text = "Czas: -";
        btnProcess.Enabled = true;
    }

    private void BtnProcess_Click(object? sender, EventArgs e)
    {
        // TODO: wywo³anie biblioteki C++ / ASM i pomiar czasu
        MessageBox.Show(
            $"Biblioteka: {cbLibrary.SelectedItem}, w¹tki: {numThreads.Value}\nAlgorytm zostanie dodany póŸniej.",
            "Info");
    }
}