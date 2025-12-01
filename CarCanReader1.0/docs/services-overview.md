# CarCANReader Pro 3.1 - Services Overview

## Service Classes

All business logic is encapsulated in service classes located in the `Services/` folder.

---

## CANParser

**Purpose:** Decode incoming SLCAN frames into structured data.

**Location:** `Services/CANParser.vb`

### Public API

```vb
Function Parse(frame As String) As CANFrame
Function IsValidFrame(frame As String) As Boolean
Function ExtractId(frame As String) As Integer
Function ParseMultiple(frames As IEnumerable(Of String)) As List(Of CANFrame)
```

### Events

```vb
Event OnFrameDecoded(id As Integer, data() As Byte)
Event OnParseError(rawFrame As String, errorMessage As String)
```

### Usage

```vb
Dim parser As New CANParser()
Dim frame As CANFrame = parser.Parse("t1238AABBCCDD")

If frame.IsValid Then
    Console.WriteLine($"ID: {frame.Id:X3}, DLC: {frame.DLC}")
End If
```

---

## CANSender

**Purpose:** Build and send CAN frames with validation.

**Location:** `Services/CANSender.vb`

### Public API

```vb
Sub SetSerialPort(port As SerialPort)
Sub Send(id As String, data As String)
Sub SendRaw(frame As String)
Sub SendBytes(id As String, data() As Byte)
Sub SendWithIntId(id As Integer, data As String)
Function ValidateFrame(id As String, data As String) As Boolean
Function BuildFrame(id As String, data As String) As String
```

### Events

```vb
Event OnFrameSent(frame As String)
Event OnSendError(message As String)
```

---

## FilterManager

**Purpose:** Filter CAN IDs for log display only.

**Location:** `Services/FilterManager.vb`

**Important:** Filter affects ONLY log output, not Dashboard or LearningEngine.

### Public API

```vb
Sub ApplyFilter(mode As FilterMode, id As Integer)
Sub ApplyRangeFilter(mode As FilterMode, fromId As Integer, toId As Integer)
Sub ClearFilter()
Function ShouldShowInLog(id As Integer) As Boolean
ReadOnly Property IsEnabled As Boolean
ReadOnly Property CurrentMode As FilterMode
```

### FilterMode Enum

```vb
Enum FilterMode
    None = 0
    ShowOnlyId = 1
    HideId = 2
    ShowRange = 3
    HideRange = 4
End Enum
```

---

## DashboardManager

**Purpose:** Process vehicle telemetry data and fire events for UI updates.

**Location:** `Services/DashboardManager.vb`

### Supported CAN IDs

| ID | Data | Description |
|----|------|-------------|
| 0x123 | byte0*256 + byte1 | RPM |
| 0x201 | byte0 | Speed (km/h) |
| 0x300 | byte0 | Temperature (°C) |
| 0x450 | byte0 (0/1) | Door Status |
| 0x321 | byte0 (0/1) | Lights Status |

### Public API

```vb
Sub ProcessFrame(id As Integer, data() As Byte)
ReadOnly Property RPM As Integer
ReadOnly Property Speed As Integer
ReadOnly Property Temperature As Integer
ReadOnly Property DoorOpen As Boolean
ReadOnly Property LightsOn As Boolean
Function ExportToCSV() As String
```

### Events

```vb
Event OnRPMChanged(value As Integer)
Event OnSpeedChanged(value As Integer)
Event OnTemperatureChanged(value As Integer)
Event OnDoorStateChanged(isOpen As Boolean)
Event OnLightStateChanged(isOn As Boolean)
```

---

## LearningEngine

**Purpose:** Detect byte-level changes for reverse engineering.

**Location:** `Services/LearningEngine.vb`

### Public API

```vb
Sub Start()
Sub [Stop]()
Sub Reset()
Sub AnalyzeFrame(id As Integer, data() As Byte)
ReadOnly Property IsRunning As Boolean
Function GetTopChangedIds(limit As Integer) As List(Of IdChangeStats)
Function GetRecentChanges(count As Integer) As List(Of ByteChangeInfo)
```

### Events

```vb
Event OnByteChange(id As Integer, byteIndex As Integer, oldValue As Byte, newValue As Byte)
Event OnSignalClassified(id As Integer, byteIndex As Integer, signalType As SignalType)
Event OnStarted()
Event OnStopped()
```

### SignalType Enum

```vb
Enum SignalType
    Unknown = 0
    OnOff = 1
    IncreasingSensor = 2
    DecreasingSensor = 3
    CommandBurst = 4
    MultiByte = 5
    Sensor = 6
End Enum
```

---

## CommandRepository

**Purpose:** Manage JSON command database (CRUD operations).

**Location:** `Services/CommandRepository.vb`

### Public API

```vb
Sub Load()
Sub Save()
Sub AddCommand(brand, model, module, name, frame As String)
Sub UpdateCommand(brand, model, module, oldName, newName, frame As String)
Sub DeleteCommand(brand, model, module, name As String)
Function GetCommand(brand, model, module, name As String) As String
Function GetBrands() As List(Of String)
Function GetModels(brand As String) As List(Of String)
Function GetModules(brand, model As String) As List(Of String)
Function GetCommands(brand, model, module As String) As List(Of String)
```

### Events

```vb
Event OnDataLoaded()
Event OnDataSaved()
Event OnError(message As String)
Event OnCommandAdded(brand, model, module, name As String)
Event OnCommandUpdated(...)
Event OnCommandDeleted(...)
```

---

## CodingEngine

**Purpose:** Build frames for coding/hidden features.

**Location:** `Services/CodingEngine.vb`

### Public API

```vb
Function LoadAvailableCommands(commandsData, brand, model, module) As List(Of String)
Function GetCommandFrame(commandsData, brand, model, module, command) As Byte()
Function BuildPreview(frame As Byte(), highlightIndex As Integer) As String
Function BuildModifiedFrame(originalFrame, byteIndex, newHexValue) As Byte()
Function BuildOnOffFrame(originalFrame, byteIndex, isOn As Boolean) As Byte()
Function SafeParseHexArray(hexString As String) As Byte()
Function BuildSlcanFrame(canId As String, data As Byte()) As String
Function IsToggleCommand(commandsData, brand, model, module, command) As Boolean
Function GetToggleOnFrame(...) As Byte()
Function GetToggleOffFrame(...) As Byte()
```

---

## LogAnalyzer

**Purpose:** Analyze log entries for statistics and insights.

**Location:** `Services/LogAnalyzer.vb`

### Public API

```vb
Function ExtractFrames(logLines As IEnumerable(Of String)) As List(Of String)
Function CountIds(frames As IEnumerable(Of String)) As Dictionary(Of Integer, Integer)
Function ParseFrame(frame As String) As (Id As Integer, Data As Byte())
Function ComputeByteStats(frames) As Dictionary(Of Integer, Dictionary(Of Integer, Integer))
Function BuildInsights(idStats, byteStats) As List(Of String)
Function GetSummary(idStats, byteStats) As String
```

---

## AdvancedUdsEngine

**Purpose:** Full UDS protocol implementation with ISO-TP support.

**Location:** `Services/AdvancedUdsEngine.vb`

### Supported Services

| SID | Service |
|-----|---------|
| 0x10 | Diagnostic Session Control |
| 0x11 | ECU Reset |
| 0x14 | Clear DTC |
| 0x19 | Read DTC |
| 0x22 | Read Data By Identifier |
| 0x27 | Security Access |
| 0x2E | Write Data By Identifier |
| 0x2F | IO Control |
| 0x31 | Routine Control |
| 0x34 | Request Download |
| 0x35 | Request Upload |
| 0x36 | Transfer Data |
| 0x37 | Request Transfer Exit |
| 0x3E | Tester Present |

### Public API

See `docs/uds-capabilities.md` for full API documentation.

---

## Service Initialization

All services are initialized in `MainForm.InitializeServices()`:

```vb
Private Sub InitializeServices()
    canSender.SetSerialPort(SerialPort1)
    
    AddHandler canSender.OnFrameSent, Sub(f) AddLog("TX: " & f)
    AddHandler dashboardManager.OnRPMChanged, Sub(v) SafeUpdateLabel(lblRPMValue, v.ToString())
    ' ... more event handlers
End Sub
```

