# CarCANReader Pro 3.1 - Coding System

## Overview

The Coding System allows activation of hidden vehicle features through CAN commands. It supports single-frame commands, toggle commands, and byte-level modifications.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    MainForm UI                          │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌──────────────┐  │
│  │cmbBrand │ │cmbModel │ │cmbModule│ │lstCodeCommands│  │
│  └────┬────┘ └────┬────┘ └────┬────┘ └──────┬───────┘  │
│       │           │           │              │          │
│       ▼           ▼           ▼              ▼          │
│  ┌─────────────────────────────────────────────────┐   │
│  │              CodingEngine                       │   │
│  │  • LoadAvailableCommands()                      │   │
│  │  • GetCommandFrame()                            │   │
│  │  • BuildPreview()                               │   │
│  │  • BuildOnOffFrame()                            │   │
│  └────────────────────┬────────────────────────────┘   │
│                       │                                 │
│                       ▼                                 │
│  ┌─────────────────────────────────────────────────┐   │
│  │              CommandRepository                  │   │
│  │  • Load()                                       │   │
│  │  • GetCommand()                                 │   │
│  │  • AddCommand()                                 │   │
│  └────────────────────┬────────────────────────────┘   │
│                       │                                 │
│                       ▼                                 │
│                  commands.json                          │
└─────────────────────────────────────────────────────────┘
```

---

## Command Types

### 1. Single Frame Command

A simple SLCAN frame string.

```json
{
  "Volkswagen": {
    "Golf MK7": {
      "BCM": {
        "NeedlesSweep": "t7600810CAFEBABE"
      }
    }
  }
}
```

**Usage:**
- Select command from list
- Click Send
- Frame is transmitted as-is

---

### 2. Toggle Command

ON/OFF pair with specific byte modification.

```json
{
  "Volkswagen": {
    "Golf MK7": {
      "BCM": {
        "NeedlesSweep": {
          "type": "toggle",
          "on": "t7600810CAFEBABE",
          "off": "t760081000000000",
          "byte": 2
        }
      }
    }
  }
}
```

**Usage:**
- Click `Coding ON` → sends "on" frame
- Click `Coding OFF` → sends "off" frame

---

### 3. Direct Frame

Build frame from individual components.

**UI Fields:**
- `txtCodeID`: CAN ID (e.g., "760")
- `txtCodeByteIndex`: Byte to modify (0-7)
- `txtCodeData`: New hex value (e.g., "FF")

---

## CodingEngine API

### LoadAvailableCommands

Get list of commands for a module.

```vb
Dim commands = codingEngine.LoadAvailableCommands(
    commandsData, 
    "Volkswagen", 
    "Golf MK7", 
    "BCM"
)
' Returns: {"NeedlesSweep", "FoldMirrors", ...}
```

### GetCommandFrame

Get frame bytes for a command.

```vb
Dim frame = codingEngine.GetCommandFrame(
    commandsData,
    "Volkswagen",
    "Golf MK7", 
    "BCM",
    "NeedlesSweep"
)
' Returns: Byte() {&H76, &H00, &H08, ...}
```

### BuildPreview

Generate human-readable preview.

```vb
Dim preview = codingEngine.BuildPreview(frame, highlightIndex:=2)
' Returns: "ID: 760 | DATA: 10 CA FE [BA] BE | Byte[2] = BA"
```

### BuildOnOffFrame

Create ON/OFF frame variant.

```vb
Dim onFrame = codingEngine.BuildOnOffFrame(originalFrame, byteIndex:=3, isOn:=True)
' Byte[3] = 01

Dim offFrame = codingEngine.BuildOnOffFrame(originalFrame, byteIndex:=3, isOn:=False)
' Byte[3] = 00
```

### BuildModifiedFrame

Modify specific byte.

```vb
Dim modified = codingEngine.BuildModifiedFrame(originalFrame, byteIndex:=3, "FF")
' Byte[3] = FF
```

### IsToggleCommand

Check if command is toggle type.

```vb
If codingEngine.IsToggleCommand(data, brand, model, module, cmd) Then
    ' Show ON/OFF buttons
Else
    ' Show single Send button
End If
```

---

## UI Workflow

### Selecting a Command

```
1. User selects Brand → cmbBrand_SelectedIndexChanged
   └── Populate cmbModel with GetModels()

2. User selects Model → cmbModel_SelectedIndexChanged
   └── Populate cmbModule with GetModules()

3. User selects Module → cmbModule_SelectedIndexChanged
   └── Populate lstCodeCommands with LoadAvailableCommands()

4. User selects Command → lstCodeCommands_SelectedIndexChanged
   └── Load preview with BuildPreview()
   └── Check IsToggleCommand() for button visibility
```

### Sending a Command

```
1. btnCodeSend_Click (or btnCodingOn / btnCodingOff)
   └── GetCommandFrame() → byte[]
   └── BuildSlcanFrame() → "t7600810..."
   └── CANSender.SendRaw(frame)
   └── Log "TX: ..."
```

---

## Preview Format

The `txtCodePreview` shows command details:

```
═══════════════════════════════════════
  COMMAND: NeedlesSweep
  TYPE: Toggle Command
═══════════════════════════════════════

  ID: 760 (hex)
  
  DATA BYTES:
  ┌────┬────┬────┬────┬────┬────┬────┬────┐
  │ 10 │ CA │ FE │ BA │ BE │ 00 │ 00 │ 00 │
  └────┴────┴────┴────┴────┴────┴────┴────┘
    0    1    2    3    4    5    6    7
               ▲
        [Controlled Byte]
        
  ON:  Sets Byte[2] = 01
  OFF: Sets Byte[2] = 00

═══════════════════════════════════════
```

---

## Error Handling

All CodingEngine methods:
- Use Try/Catch internally
- Never throw exceptions
- Return empty/default values on failure
- Log errors via OnError event

```vb
Dim frame = codingEngine.GetCommandFrame(...)
If frame Is Nothing OrElse frame.Length = 0 Then
    MessageBox.Show("Command not found or invalid")
    Return
End If
```

---

## Saving Discovered Commands

After discovering a command via LearningEngine:

```vb
' Save from log
btnCodeSaveFromLog_Click:
    1. Get selected frame from lstLog
    2. Parse ID and Data
    3. Prompt for command name
    4. CommandRepository.AddCommand(brand, model, module, name, frame)
    5. Refresh UI
```

---

## Safety Considerations

⚠️ **Before sending any coding command:**

1. Ensure vehicle is in PARK
2. Engine can be OFF or ON (depends on feature)
3. Test in safe environment first
4. Never send unknown commands while driving
5. Some commands may require Extended Session first
6. Some commands may be irreversible

---

## Best Practices

1. **Organize commands logically**
   - Brand → Model → Module hierarchy
   - Use descriptive command names

2. **Test before saving**
   - Send command manually first
   - Verify expected behavior
   - Then save to commands.json

3. **Document byte meanings**
   - Add comments in JSON
   - Note which bytes do what

4. **Backup commands.json**
   - Automatic backup created on save
   - Manual backup before major changes

