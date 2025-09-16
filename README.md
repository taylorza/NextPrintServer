# NextPrintServer

Windows Printer Server to support Network Printing from the ZX Spectrum Next using the [WIFI Printer Driver](https://taylorza.itch.io/zx-spectrum-next-wifi-printer-driver).

## Features
- Supports any Windows Printer, local or networked
- Use any installed Windows font
- Supports a subset of the EPSON ESC/POS command set, including color printing
- Printing ZX Spectrum special characters (©, £, ↑, ▝, ▘, ▀, ▗, ▐, ▚, ▌, ▛,  ▄, ▟, ▙, █)
- Print preview

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

