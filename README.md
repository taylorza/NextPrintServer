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