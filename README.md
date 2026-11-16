# NextPrintServer

Windows Printer Server to support Network Printing from the ZX Spectrum Next using the [WIFI Printer Driver](https://taylorza.itch.io/zx-spectrum-next-wifi-printer-driver).

## Features
- Supports any Windows Printer, local or networked
- Use any installed Windows font
- Supports ZX Spectrum print control codes
- Supports a subset of the EPSON ESC/POS command set, including color printing
- Printing ZX Spectrum special characters (©, £, ↑, ▝, ▘, ▀, ▗, ▐, ▚, ▌, ▛,  ▄, ▟, ▙, █)
- Print preview
- Show your local IP address for easy ZX Spectrum Next configuration

## Supported ESC/POS Commands
| Command | Description |
|---------|-------------|
| `ESC @` | Initialize/Reset Printer |
| `ESC E` | Enable Bold Mode |
| `ESC F` | Disable Bold Mode |
| `ESC 4` | Enable Italics |
| `ESC 5` | Disable Italics |
| `ESC - 1` | Underline on |
| `ESC - 0` | Underline off |
| `ESC r n` | Select color n <br>0 - Black<br>1 - Blue<br>2 - Red<br>3 - Magenta<br>4 - Green<br>5 - Cyan<br>6 - Yellow<br>7 - Light gray |

## ZX Spectrum print control codes
| Code | Description |
|------|-------------|
| TAB n | Move n SPACES to the right, wraps to start of current line |
| AT y,x | Move to line y, column x of the currently printing page (0,0 is top left)|
| CHR$ 8 | Move left one character position |
| CHR$ 9 | Move right one character position |
| , | TAB to half the line width |

## Linux version (including ARM / Raspberry Pi)
`NextPrintServer.Linux` is a console version of the server for Linux. It has no window; each print job is rendered to a PDF and sent to a [CUPS](https://www.cups.org/) printer with `lp`. It supports the same control codes as the Windows version.

### Build
Requires the .NET 8 SDK. The build can be done on any machine, including Windows or an x64 Linux PC.

```
dotnet publish NextPrintServer.Linux -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=true -o publish
```

Use `-r linux-arm` for a 32-bit ARM OS or `-r linux-x64` for a PC. Copy `nextprintserver` and `libSkiaSharp.so` from the `publish` folder to the target machine.

### Requirements on the target machine
- CUPS with a configured printer (`lp` must work from the command line)
- `libfontconfig1`, plus the font you want to print with. The default font is DejaVu Sans Mono (`fonts-dejavu-core` on Debian / Raspberry Pi OS)

### Run
```
./nextprintserver --printer MyPrinter --paper A4
```

The addresses to use on the ZX Spectrum Next are shown at startup.

| Option | Description |
|--------|-------------|
| `--port N` | TCP port to listen on (default 9100) |
| `--printer NAME` | CUPS printer to print to (default: the CUPS default printer) |
| `--font NAME\|FILE` | Font family name or path to a font file (default "DejaVu Sans Mono") |
| `--font-size PT` | Font size in points (default 10) |
| `--paper SIZE` | A3, A4, A5, Letter, Legal or WIDTHxHEIGHT in mm (default A4) |
| `--margin MM` | Page margin in mm (default 6) |
| `--lp-option OPT` | Extra option passed to `lp` as `-o OPT`, may be repeated |
| `--output-dir DIR` | Keep a PDF copy of every print job in DIR, together with the raw data that was received (`.bin`) |
| `--no-print` | Do not send jobs to CUPS, requires `--output-dir` |
| `--idle-timeout SEC` | End a job when no data arrives for SEC seconds (default: wait forever) |

To start the server at boot, see the example systemd unit [nextprintserver.service](NextPrintServer.Linux/nextprintserver.service).