namespace OtsuGui;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Liczba w¹tków jako parametr: "OtsuGui.exe 7" albo "OtsuGui.exe --threads 7"
        int? threads = null;
        int n;
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "--threads" || args[i] == "-t") && i + 1 < args.Length
                && int.TryParse(args[i + 1], out n))
            {
                threads = n;
                break;
            }
            if (int.TryParse(args[i], out n))
            {
                threads = n;
                break;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1(threads));
    }
}