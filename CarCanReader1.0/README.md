# CarCANReader Pro 3.1

## Full CAN/OBD/UDS Reverse Engineering Suite

![Version](https://img.shields.io/badge/version-3.1-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![.NET](https://img.shields.io/badge/.NET-Framework%204.8-purple)

CarCANReader Pro is a professional-grade Windows Forms application for automotive CAN bus reverse engineering, diagnostics, and hidden feature activation. Designed for automotive enthusiasts, mechanics, and developers.

---

## ✨ Features

### 🔌 CAN Communication
- SLCAN protocol support (CANable, CANable Pro, LAWICEL)
- Auto-detect COM port
- Real-time frame monitoring
- Frame filtering (show/hide by ID or range)
- TX/RX logging with timestamps

### 📊 Dashboard
- Real-time vehicle data display:
  - RPM, Speed, Temperature
  - Door status, Light status
- Configurable CAN ID mappings
- Historical data tracking
- CSV export for analysis

### 🔍 Learning Engine
- Automatic byte-change detection
- Signal classification (Toggle, Sensor, Command)
- Pattern recognition for reverse engineering
- Export discovered signals

### 🔧 Coding System (Hidden Features)
- Toggle command support (ON/OFF)
- Single frame commands
- Byte-level modification
- Command preview before sending
- Save discovered commands to JSON

### 🩺 OBD-II Support
- Mode 01: Live data (PIDs)
- Mode 03: Read DTCs
- Mode 04: Clear DTCs

### 🔐 UDS (Unified Diagnostic Services)
- Diagnostic sessions (Default, Extended, Programming)
- ECU Reset
- Read/Write Data by Identifier
- Security Access (Seed/Key)
- Routine Control
- IO Control
- ISO-TP multi-frame support

### 📋 Command Management
- JSON-based command storage
- Hierarchical organization (Brand → Model → Module → Command)
- Built-in command editor
- Import/Export capabilities

---

## 🚗 Supported Vehicles

CarCANReader Pro works with any vehicle that has:
- Standard OBD-II port (all cars since 2001 in EU, 1996 in USA)
- CAN bus operating at 500 kbps (most common)
- 11-bit standard CAN IDs

### Tested With:
- Volkswagen Group (VW, Audi, Skoda, Seat)
- BMW
- Mercedes-Benz
- Ford
- Toyota
- Hyundai/Kia
- And many more...

---

## 🔌 Hardware Requirements

### Recommended: CANable Pro
- USB-to-CAN adapter with SLCAN firmware
- 500 kbps / 250 kbps support
- Galvanic isolation (Pro version)

### Compatible Adapters:
- CANable (original)
- CANable Pro
- LAWICEL CANUSB
- Any SLCAN-compatible adapter

### Connection:
```
Vehicle OBD-II Port          CANable
     [CAN-H] ─────────────── [CAN-H]
     [CAN-L] ─────────────── [CAN-L]
     [GND]   ─────────────── [GND]
```

---

## 🚀 Quick Start

### 1. Connect Hardware
1. Plug CANable into USB port
2. Connect CANable to vehicle OBD-II port
3. Turn vehicle ignition ON (engine can be off)

### 2. Launch Application
1. Run `CarCanReader1.0.exe`
2. Click **Connect** - port auto-detected
3. Status should show "Bağlı" (Connected)

### 3. Monitor CAN Traffic
- Frames appear in the log automatically
- Use filters to focus on specific IDs
- Click **Analyze Log** for statistics

### 4. Discover Features
1. Go to **Learning** tab
2. Click **Start Diff**
3. Operate a feature (e.g., press a button)
4. Observe which bytes change
5. Save discovered command

---

## 📁 File Structure

```
CarCanReader1.0/
├── CarCanReader1.0.exe     # Main application
├── commands.json           # Saved commands database
├── commands.json.backup    # Automatic backup
└── logs/                   # Exported logs (optional)
```

---

## 📝 commands.json Format

```json
{
  "Volkswagen": {
    "Golf MK7": {
      "BCM": {
        "UnlockAllDoors": "t3D08DEADBEEF1234",
        "WindowsDown": {
          "type": "toggle",
          "on": "t3D08FF00000000",
          "off": "t3D0800FF000000",
          "byte": 0
        }
      }
    }
  }
}
```

### Command Types:
- **String**: Direct SLCAN frame
- **Toggle Object**: ON/OFF commands with byte index

---

## ⚠️ Safety Notes

### ⚡ IMPORTANT
- **Never send unknown commands while driving**
- **Some commands can affect vehicle safety systems**
- **Always test in a safe, stationary environment first**
- **Keep engine OFF when experimenting with unknown IDs**

### 🔒 Security
- Some ECUs require security access (seed/key)
- Programming sessions can modify ECU parameters
- Always backup ECU data before modifications

### 🚨 Disclaimer
This software is for educational and diagnostic purposes. The developers are not responsible for any damage to vehicles or injury resulting from improper use.

---

## 🛠️ Troubleshooting

### No COM Port Found
- Check USB connection
- Install CANable drivers (if needed)
- Try different USB port

### No CAN Frames
- Verify CAN-H/CAN-L connections
- Check vehicle ignition is ON
- Confirm baud rate (usually 500k)

### Application Freezes
- High CAN traffic can cause UI delays
- Use filters to reduce displayed frames
- Check available system memory

---

## 📖 Documentation

- [Architecture Overview](docs/architecture.md)
- [Services Overview](docs/services-overview.md)
- [UDS Capabilities](docs/uds-capabilities.md)
- [Coding System](docs/coding-system.md)
- [Filter System](docs/filter-system.md)
- [Learning Engine](docs/learning-engine.md)
- [Commands JSON Format](docs/commands-json-format.md)

---

## 🔄 Version History

### v3.1 (Current)
- Complete architecture refactor
- Modular service-based design
- Advanced UDS engine with ISO-TP
- Improved learning engine
- Enhanced coding system

### v3.0
- Initial public release
- Basic CAN monitoring
- Simple command system

---

## 📄 License

This project is for educational purposes. See LICENSE file for details.

---

## 🤝 Contributing

Contributions are welcome! Please read the contributing guidelines before submitting pull requests.

---

**Made with ❤️ for the automotive enthusiast community**

