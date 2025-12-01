# CarCANReader — Full Project Specification  
Comprehensive architecture, coding guidelines, and refactor plan for Cursor AI.

---

## 1. Project Overview
CarCANReader is a Windows Forms VB.NET application designed to communicate with vehicles via CANbus using a CANable Pro device running SLCAN firmware.

The program integrates:
- Real-time CAN data reading (SLCAN protocol)
- CAN frame transmitting
- OBD-II Mode 01 / Mode 03 / Mode 04
- UDS (Unified Diagnostic Services) support (sessions, clear DTC, custom service bytes)
- Hidden feature (coding) engine
- JSON-based command storage & editor
- “Learning Engine” (automatic signal discovery)
- Dashboard visualization (RPM, Speed, Temp, Door status, Lights)
- Fault detection system
- CAN ID filtering system
- Log analyzer
- History export (CSV)

The goal is to transform this into a **professional, modular, maintainable and scalable CAN diagnostic application.**

---

## 2. High-Level Goals
Cursor should aim to:

### ✔ Refactor the project into modular architecture  
MainForm.vb is too large. Each subsystem must be separated:

- `CANParser.vb` (SLCAN decoding, frame extraction)
- `CANSender.vb` (sending frames with validation)
- `CodingEngine.vb` (toggle logic, manual frame handling)
- `CommandRepository.vb` (load/save JSON, manage command tree)
- `LearningEngine.vb` (optimize and stabilize)
- `UDSManager.vb` (sessions, clear DTC, custom UDS)
- `OBDManager.vb` (PID requests + decoding)
- `DashboardManager.vb`
- `FaultAnalyzer.vb`
- `FilterManager.vb`
- `LogAnalyzer.vb`

MainForm should only handle:
- UI click events
- UI update logic
- Calling services

---

## 3. Required Features (Functional Requirements)

### **3.1 CAN Communication**
- Support SLCAN protocol: `tIDDLCDATA` format
- Auto-detect COM port
- Bus initialization: `C`, `S6`, `O`
- Reliable serial read buffer with CR (`\r`) termination
- Multi-thread safe buffer processing

### **3.2 CAN Parsing**
- Parse ID (3 hex chars)
- Parse DLC
- Parse data bytes
- Handle short, incomplete or malformed frames
- Expose event: `OnFrameDecoded(id, data())`

### **3.3 CAN Sending**
- Validate ID length (3 hex)
- Validate DLC (0-8)
- Validate hex data pairs
- Format: `tIDDLCDATA`
- Queue or direct write

---

## 4. Coding Engine (Hidden Features)
### System must support 3 types:

#### **A) Single Frame Command**
{
"type": "single",
"id": "123",
"data": "AABBCCDD"
}

#### **B) Toggle Command**
{
"type": "toggle",
"id": "123",
"byte": 5,
"on": "t1238FF00000000",
"off": "t12380000000000"
}

#### **C) Direct Frame String**
"t1238AABBCCDDEEFF"

UI must correctly detect the type and show preview.

Coding engine must expose:
- ApplyToggle(id, on/off)
- SendSingleFrame(id, data)
- BuildFrame(id, byteIndex, newValue)

---

## 5. Learning Engine
Must be improved to be more stable and predictable.

### Requirements:
- Detect byte-level changes over time
- Classify signal type:
  - On/Off
  - Increasing sensor
  - Decreasing sensor
  - Command burst
- Generate auto JSON-ready command entries
- Output human-readable lines
- Unified consistent output format

Cursor may refactor this into proper class structure.

---

## 6. Dashboard System

### Inputs:
- ID 0x123 → RPM (byte0*256 + byte1)
- ID 0x201 → Speed
- ID 0x300 → Coolant
- ID 0x450 → Door Open
- ID 0x321 → Lights

### Output Requirements:
- UI labels + progress bars
- History arrays (time, rpm, speed, temp)
- CSV export with stable formatting

---

## 7. Fault Analyzer
Detect module issues:
- Constant 0x00 frames
- Constant 0xFF frames
- Frozen/unmoving data
- Missing frames for >5 seconds

Make this modular and testable.

---

## 8. UDS Manager
Support:
### Sessions:
- Default (0x01)
- Programming (0x02)
- Extended (0x03)

### Clear DTC:
- send: `03 14 FF FF`

### Custom Send:
- arbitrary sequences

---

## 9. OBD-II Manager
Support:
### Mode 01:
- 0x0C: RPM
- 0x0D: Speed
- + others

### Mode 03:
- Read DTC

### Mode 04:
- Clear DTC

Decoder class should be moved into OBDManager.

---

## 10. CAN ID Filter System
Modes:
- Show only ID
- Hide ID
- Show range
- Hide range

Filtering must apply **only to log output**, not:
- dashboard
- learning engine
- fault analyzer

(Currently the filter disables entire processing — must be fixed.)

---

## 11. Log Analyzer
Count ID frequencies, detect busy IDs.

---

## 12. Commands JSON Specification

Must load/save from:
commands.json

Requirements:
- Normalize command structures
- Auto-create missing brand/model/module
- Prevent invalid JSON overwrites
- Prevent toggle commands from losing ON/OFF metadata

---

## 13. Refactor Rules (VERY IMPORTANT)

Cursor must follow these rules:

### ✔ No logic inside MainForm (only UI)
### ✔ Each feature → separate class
### ✔ Use Invoke wrapper helper to avoid duplicates
### ✔ Error messages must be consistent
### ✔ All CAN code must be robust to malformed frames
### ✔ Avoid repeated code (deduplicate)
### ✔ Add comments in Turkish for maintainability
### ✔ Keep backward compatibility (don’t break current UI)

---

## 14. Future Expandability (Cursor should keep in mind)
- Support extended CAN (29-bit)
- Support database of DBC file parsing (optional)
- Add customizable dashboards
- Add multi-frame UDS (ISO-TP) support in future phases

---

## 15. Tasks Cursor Will Perform Step-by-Step

Cursor should follow these tasks in future instructions:

1. Analyze entire project structure
2. Identify broken logic, fragile code, repetitive code
3. Create required new class files
4. Move logic out of MainForm into services
5. Update MainForm to use new classes
6. Improve thread safety, error handling, readability
7. Clean up JSON system
8. Improve Learning Engine stability
9. Optimize CAN parsing speed
10. Provide final refactored architecture summary

---

## 16. OBD-II Advanced Support (Phase OBD-ADVANCED)

### Diagnostics Tab
A dedicated Diagnostics tab provides access to all advanced OBD-II modes:

#### Mode 02 - Freeze Frame
- Stores engine parameters at the moment a DTC was set
- UI: `grpFreezeFrame`, `lstFreezeFrame`, `btnReadFreezeFrame`
- Method: `OBDService.RequestFreezeFrame()`

#### Mode 07 - Pending DTCs
- Reads pending (not yet confirmed) diagnostic trouble codes
- UI: `grpPendingDTC`, `lstPendingDTC`, `btnReadPendingDTC`, `btnClearDTC`
- Method: `OBDService.RequestPendingDTC()`
- Clear: `OBDService.ClearDTC()` (Mode 04)

#### Mode 06 - On-Board Monitoring
- Reads test results from emission-related components
- UI: `grpMode06`, `lstMode06`, `btnReadMode06`
- Method: `OBDService.RequestMode06()`

#### Mode 09 - Vehicle Information
- Reads VIN, Calibration ID, CVN, ECU Name
- UI: `grpVehicleInfo`, `lblVIN`, `lblCalibrationID`, `lblCVN`, `lblECUName`
- Method: `OBDService.RequestVehicleInfo()`

#### PID Auto-Scan
- Automatically detects which PIDs are supported by the vehicle
- UI: `grpPIDAutoScan`, `lstSupportedPIDs`, `btnAutoScanPIDs`, `lblScanStatus`
- Method: `OBDService.AutoScanPIDs()`

### OBDService.vb Extensions
```vb
' Models
Class Mode06TestResult   ' Test ID, value, limits, pass/fail
Class VehicleInfoModel   ' VIN, Calibration ID, CVN, ECU Name
Class DTCInfo            ' Code, description, pending/freeze status

' Events
OnPendingDTCReceived(dtcs As List(Of DTCInfo))
OnFreezeFrameReceived(dtc As String, data As Dictionary)
OnMode06Received(tests As List(Of Mode06TestResult))
OnVehicleInfoReceived(info As VehicleInfoModel)
OnAutoScanCompleted(supportedPIDs As Boolean())
OnDTCCleared()

' Methods
RequestFreezeFrame(frameNumber As Byte)
RequestPendingDTC()
RequestMode06(testId As Byte)
RequestVehicleInfo()
AutoScanPIDs()
ClearDTC()
ProcessFrameAdvanced(frameId, data)  ' Handles all modes 01-09
```

### DashboardManager.vb Extensions
```vb
' Fields
_pendingDTCs As List(Of DTCInfo)
_freezeFrameData As Dictionary(Of Byte, Double)
_mode06Tests As List(Of Mode06TestResult)
_vehicleInfo As VehicleInfoModel
_supportedPIDs(255) As Boolean

' Methods
UpdatePendingDTC(dtcs)
UpdateFreezeFrame(data)
UpdateMode06(tests)
UpdateVehicleInfo(info)
UpdateSupportedPIDFlags(pids)
```

### Filter Rules
OBD-II response IDs (0x7E8-0x7EF) are always allowed through the filter, regardless of filter settings.

---

This specification defines the complete behavior, architecture and rewrite rules for the CarCANReader project. Cursor must use this file as the MAIN REFERENCE when working on any part of the code.
