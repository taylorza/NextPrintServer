using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using NextPrintServer.Core;

namespace NextPrintServer.Linux
{
    internal class Options
    {
        public int Port = 9100;
        public string? Printer;
        public string Font = "DejaVu Sans Mono";
        public float FontSize = 10;
        public string Paper = "A4";
        public float MarginMm = 6;
        public string? OutputDir;
        public bool NoPrint;
        public int IdleTimeoutSeconds;
        public List<string> LpOptions = new();
    }

    /// <summary>Copies every byte read from a stream to a file, to help diagnose print problems.</summary>
    internal class CaptureStream(Stream source, Stream capture) : Stream
    {
        public override int ReadByte()
        {
            int b = source.ReadByte();
            if (b != -1)
            {
                capture.WriteByte((byte)b);
                capture.Flush();
            }
            return b;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = source.Read(buffer, offset, count);
            capture.Write(buffer, offset, n);
            capture.Flush();
            return n;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    internal static class Program
    {
        private const string Usage = """
            Usage: nextprintserver [options]

            Print server for the ZX Spectrum Next WIFI printer driver. Each connection is
            rendered to a PDF and sent to a CUPS printer with 'lp'.

            Options:
              --port N              TCP port to listen on (default 9100)
              --printer NAME        CUPS printer to print to (default: the CUPS default printer)
              --font NAME|FILE      Font family name or path to a font file (default "DejaVu Sans Mono")
              --font-size PT        Font size in points (default 10)
              --paper SIZE          A3, A4, A5, Letter, Legal or WIDTHxHEIGHT in mm (default A4)
              --margin MM           Page margin in mm (default 6)
              --lp-option OPT       Extra option passed to lp as '-o OPT', may be repeated
              --output-dir DIR      Keep a PDF copy of every print job in DIR, together with
                                    the raw data that was received (.bin)
              --no-print            Do not send jobs to CUPS, requires --output-dir
              --idle-timeout SEC    End a job when no data arrives for SEC seconds (default: wait forever)
              --help                Show this help
              --version             Show the version
            """;

        private static int Main(string[] args)
        {
            Options options;
            try
            {
                var parsed = ParseArgs(args);
                if (parsed == null) return 0;
                options = parsed;
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine($"nextprintserver: {ex.Message}");
                Console.Error.WriteLine("Try 'nextprintserver --help' for more information.");
                return 2;
            }

            PageSetup.TryParsePaper(options.Paper, out float widthMm, out float heightMm);
            var page = new PageSetup(widthMm, heightMm, options.MarginMm);

            // Check the font up front so a bad name is reported at startup, not on the first job
            using (var probe = new PdfPrintSurface(Stream.Null, page, options.Font, options.FontSize))
            {
                Log($"Font: {probe.FontFamily} {options.FontSize}pt");
                if (!File.Exists(options.Font) && !probe.FontFamily.Equals(options.Font, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"Warning: font '{options.Font}' was not found, using '{probe.FontFamily}' instead");
                }
            }

            if (options.OutputDir != null) Directory.CreateDirectory(options.OutputDir);

            TcpListener server;
            try
            {
                server = new TcpListener(new IPEndPoint(IPAddress.Any, options.Port));
                server.Start();
            }
            catch (SocketException ex)
            {
                Console.Error.WriteLine($"nextprintserver: cannot listen on port {options.Port}: {ex.Message}");
                return 1;
            }

            Log(options.NoPrint ? "Printer: none (saving PDF files only)" : $"Printer: {options.Printer ?? "CUPS default"}");
            LogAddresses(options.Port);

            while (true)
            {
                try
                {
                    Log("Waiting for connection");
                    using var tcpClient = server.AcceptTcpClient();
                    Log($"Connection from {tcpClient.Client.RemoteEndPoint}");
                    if (options.IdleTimeoutSeconds > 0) tcpClient.ReceiveTimeout = options.IdleTimeoutSeconds * 1000;
                    HandleJob(tcpClient, page, options);
                }
                catch (Exception ex)
                {
                    Log($"Error: {ex.Message}");
                }
            }
        }

        private static void HandleJob(TcpClient tcpClient, PageSetup page, Options options)
        {
            string fileName = $"zxnext-{DateTime.Now:yyyyMMdd-HHmmss-fff}.pdf";
            string pdfPath = Path.Combine(options.OutputDir ?? Path.GetTempPath(), fileName);
            int pageCount;
            bool hasContent;
            try
            {
                using (var output = File.Create(pdfPath))
                using (var surface = new PdfPrintSurface(output, page, options.Font, options.FontSize))
                using (var raw = options.OutputDir != null ? File.Create(Path.ChangeExtension(pdfPath, ".bin")) : null)
                using (var input = new BufferedStream(tcpClient.GetStream()))
                {
                    var job = new PrintJob(raw != null ? new CaptureStream(input, raw) : input, surface);
                    job.Run();
                    pageCount = job.PageCount;
                    hasContent = job.HasContent;
                }

                if (!hasContent)
                {
                    // Do not waste a sheet of paper on a job with nothing on it
                    Log("Nothing to print");
                    File.Delete(pdfPath);
                    if (options.OutputDir != null) Log($"Saved {Path.ChangeExtension(pdfPath, ".bin")}");
                    return;
                }

                Log($"Received {pageCount} page(s)");
                if (options.OutputDir != null) Log($"Saved {pdfPath}");
                if (!options.NoPrint) SendToPrinter(pdfPath, options);
            }
            finally
            {
                if (options.OutputDir == null) File.Delete(pdfPath);
            }
        }

        private static void SendToPrinter(string pdfPath, Options options)
        {
            var startInfo = new ProcessStartInfo("lp")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            if (options.Printer != null)
            {
                startInfo.ArgumentList.Add("-d");
                startInfo.ArgumentList.Add(options.Printer);
            }
            startInfo.ArgumentList.Add("-t");
            startInfo.ArgumentList.Add("ZX Spectrum Next");
            foreach (var option in options.LpOptions)
            {
                startInfo.ArgumentList.Add("-o");
                startInfo.ArgumentList.Add(option);
            }
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add(pdfPath);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("could not start lp");
            var stderr = process.StandardError.ReadToEndAsync();
            string stdout = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"lp failed: {stderr.Result.Trim()}");
            }
            Log($"Sent to printer: {stdout}");
        }

        // Shows the addresses to enter on the ZX Spectrum Next, like the IP Address window of the Windows version
        private static void LogAddresses(int port)
        {
            IPAddress? preferred = null;
            try
            {
                // Connecting a UDP socket sends nothing, it only selects the outgoing interface
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                preferred = (socket.LocalEndPoint as IPEndPoint)?.Address;
            }
            catch (SocketException)
            {
                // No default route
            }

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    string marker = ua.Address.Equals(preferred) ? "  <-- use this address" : "";
                    Log($"Listening on {ua.Address}:{port} ({ni.Name}){marker}");
                }
            }
        }

        private static void Log(string message) => Console.WriteLine(message);

        /// <returns>null if the program should exit successfully (help or version was shown)</returns>
        private static Options? ParseArgs(string[] args)
        {
            var options = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"option '{arg}' requires a value");
                float Number()
                {
                    string value = Value();
                    return float.TryParse(value, CultureInfo.InvariantCulture, out float n) && n >= 0
                        ? n
                        : throw new ArgumentException($"invalid value '{value}' for option '{arg}'");
                }

                switch (arg)
                {
                    case "--port": options.Port = (int)Number(); break;
                    case "--printer": options.Printer = Value(); break;
                    case "--font": options.Font = Value(); break;
                    case "--font-size": options.FontSize = Number(); break;
                    case "--paper": options.Paper = Value(); break;
                    case "--margin": options.MarginMm = Number(); break;
                    case "--lp-option": options.LpOptions.Add(Value()); break;
                    case "--output-dir": options.OutputDir = Value(); break;
                    case "--no-print": options.NoPrint = true; break;
                    case "--idle-timeout": options.IdleTimeoutSeconds = (int)Number(); break;
                    case "--help":
                    case "-h":
                        Console.WriteLine(Usage);
                        return null;
                    case "--version":
                        Console.WriteLine(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
                        return null;
                    default:
                        throw new ArgumentException($"unknown option '{arg}'");
                }
            }

            if (options.Port is < 1 or > 65535) throw new ArgumentException("port must be between 1 and 65535");
            if (options.FontSize <= 0) throw new ArgumentException("font size must be greater than zero");
            if (options.NoPrint && options.OutputDir == null) throw new ArgumentException("--no-print requires --output-dir");
            if (!PageSetup.TryParsePaper(options.Paper, out float widthMm, out float heightMm))
            {
                throw new ArgumentException($"unknown paper size '{options.Paper}'");
            }
            if (options.MarginMm * 2 >= Math.Min(widthMm, heightMm)) throw new ArgumentException("margin is too large for the paper size");

            return options;
        }
    }
}
