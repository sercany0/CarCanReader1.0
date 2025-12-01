# CarCANReader Pro 3.1 - Architecture Overview

## Design Philosophy

CarCANReader Pro follows a **service-oriented architecture** where the MainForm acts purely as a UI orchestrator, delegating all business logic to specialized service classes.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           MainForm.vb                               │
│                    (UI Layer - Event Handlers Only)                 │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────────┐ │
│  │  CANParser   │  │  CANSender   │  │     FilterManager          │ │
│  │  (Decode RX) │  │  (Build TX)  │  │  (Log filtering only)      │ │
│  └──────────────┘  └──────────────┘  └────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────┐  ┌─────────────────┐  ┌───────────────────┐  │
│  │ DashboardManager │  │ LearningEngine  │  │   LogAnalyzer     │  │
│  │ (Vehicle gauges) │  │ (Byte diff)     │  │  (Statistics)     │  │
│  └──────────────────┘  └─────────────────┘  └───────────────────┘  │
│                                                                     │
│  ┌─────────────────────┐  ┌──────────────────────────────────────┐ │
│  │  CommandRepository  │  │        CodingEngine                  │ │
│  │  (JSON CRUD)        │  │  (Frame building, toggle logic)      │ │
│  └─────────────────────┘  └──────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                    AdvancedUdsEngine                         │  │
│  │  (ISO-TP, Security Access, DID read/write, Routines)         │  │
│  └──────────────────────────────────────────────────────────────┘  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
                    ┌─────────────────────────┐
                    │     SerialPort1         │
                    │   (SLCAN Protocol)      │
                    └─────────────────────────┘
                                   │
                                   ▼
                    ┌─────────────────────────┐
                    │   CANable Pro Device    │
                    │   (USB-to-CAN)          │
                    └─────────────────────────┘
                                   │
                                   ▼
                    ┌─────────────────────────┐
                    │     Vehicle CAN Bus     │
                    └─────────────────────────┘
```

---

## Layer Responsibilities

### UI Layer (MainForm.vb)
- Handle Windows Forms events
- Update UI controls (labels, lists, combos)
- Wire up service events to UI updates
- Thread-safe UI operations via Invoke()
- **NO business logic**

### Service Layer (Services/)
- All business logic encapsulated
- Event-driven communication
- No UI references
- Testable in isolation

### Model Layer (Models/)
- Data structures (CANFrame)
- No logic, just properties

---

## Data Flow

### Receiving CAN Frames

```
SerialPort.DataReceived
        │
        ▼
ProcessSlcanFrame(line)
        │
        ▼
CANParser.Parse(frame)
        │
        ├───────────────────────────────────────┐
        │                                       │
        ▼                                       ▼
DashboardManager.ProcessFrame()       LearningEngine.AnalyzeFrame()
        │                                       │
        ▼                                       ▼
OnRPMChanged (event)                  OnByteChange (event)
        │                                       │
        ▼                                       ▼
lblRPMValue.Text = ...                lstDiff.Items.Add(...)
        
        │
        ▼
FilterManager.ShouldShowInLog(id)
        │
        ▼ (if true)
AddLog("RX: " + frame)
```

### Sending CAN Frames

```
btnSend_Click
        │
        ▼
txtID.Text, txtData.Text
        │
        ▼
CANSender.Send(id, data)
        │
        ├── Validate ID (3 hex chars)
        ├── Validate Data (even hex length)
        ├── Build SLCAN frame: "t" + ID + DLC + DATA
        │
        ▼
SerialPort.WriteLine(frame)
        │
        ▼
OnFrameSent (event)
        │
        ▼
AddLog("TX: " + frame)
```

---

## Service Dependencies

```
MainForm
    ├── CANParser (no dependencies)
    ├── CANSender
    │       └── SerialPort (injected)
    ├── FilterManager (no dependencies)
    ├── DashboardManager (no dependencies)
    ├── LearningEngine (no dependencies)
    ├── CommandRepository (no dependencies)
    ├── CodingEngine (no dependencies)
    ├── LogAnalyzer (no dependencies)
    └── AdvancedUdsEngine
            └── CANSender (injected)
```

---

## Threading Model

- **UI Thread**: All UI updates
- **Serial Thread**: SerialPort.DataReceived runs on ThreadPool
- **Thread Safety**: InvokeRequired + Invoke() pattern

```vb
Private Sub SafeUpdateLabel(lbl As Label, text As String)
    If lbl.InvokeRequired Then
        lbl.Invoke(Sub() lbl.Text = text)
    Else
        lbl.Text = text
    End If
End Sub
```

---

## Event System

Services communicate via events:

```vb
' Service declares event
Public Event OnRPMChanged(value As Integer)

' Service raises event
RaiseEvent OnRPMChanged(newRpm)

' MainForm subscribes
AddHandler dashboardManager.OnRPMChanged, Sub(v) 
    SafeUpdateLabel(lblRPMValue, v.ToString())
End Sub
```

---

## File Organization

```
CarCanReader1.0/
├── MainForm.vb              # UI orchestrator
├── MainForm.Designer.vb     # UI control definitions
├── MainForm.resx            # UI resources
│
├── Models/
│   └── CANFrame.vb          # Parsed frame structure
│
├── Services/
│   ├── CANParser.vb         # SLCAN decoding
│   ├── CANSender.vb         # Frame building + sending
│   ├── FilterManager.vb     # ID filtering (log only)
│   ├── DashboardManager.vb  # Vehicle data processing
│   ├── LearningEngine.vb    # Byte change detection
│   ├── CommandRepository.vb # JSON command CRUD
│   ├── CodingEngine.vb      # Toggle/single frame logic
│   ├── LogAnalyzer.vb       # Statistics + insights
│   └── AdvancedUdsEngine.vb # Full UDS protocol
│
├── commands.json            # Saved commands
├── project-spec.md          # Project specification
└── docs/                    # Documentation
```

---

## Design Patterns Used

| Pattern | Usage |
|---------|-------|
| **Observer** | Events for service-to-UI communication |
| **Singleton-like** | Services instantiated once in MainForm |
| **Factory** | CANParser.Parse() creates CANFrame |
| **Strategy** | FilterMode enum + ShouldShowInLog() |
| **Repository** | CommandRepository for data access |

---

## Key Design Decisions

### 1. Filter Only Affects Log
Per project-spec.md Section 10:
- Dashboard always receives frames
- LearningEngine always receives frames
- Filter only hides frames from log display

### 2. Services Are Stateless Where Possible
- CANParser: Pure function, no state
- CANSender: Only holds SerialPort reference
- FilterManager: Holds filter config only

### 3. No Direct UI Access in Services
Services never reference:
- Form controls
- MessageBox
- Any System.Windows.Forms types

### 4. Event-Driven Updates
UI updates happen through events, not polling:
- Cleaner code
- Better performance
- Easier testing

