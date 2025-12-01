# CarCANReader Pro 3.1 - UDS Capabilities

## Overview

The AdvancedUdsEngine provides full Unified Diagnostic Services (UDS) protocol support based on ISO 14229-1.

---

## Supported UDS Services

### Diagnostic Session Control (0x10)

Control the ECU diagnostic mode.

```vb
' Start extended session
Dim response = udsEngine.StartExtendedSession(&H7E0)

' Return to default
Dim response = udsEngine.ReturnToDefaultSession(&H7E0)

' Start programming session (caution!)
Dim response = udsEngine.StartProgrammingSession(&H7E0)
```

**Session Types:**
- `DefaultSession` (0x01) - Basic diagnostics
- `ProgrammingSession` (0x02) - ECU reprogramming
- `ExtendedDiagnosticSession` (0x03) - Full diagnostic access
- `SafetySystemSession` (0x04) - Safety-critical systems

---

### ECU Reset (0x11)

Reset the ECU.

```vb
' Hard reset
Dim response = udsEngine.HardResetEcu(&H7E0)

' Soft reset
Dim response = udsEngine.SoftResetEcu(&H7E0)

' Specific reset type
Dim response = udsEngine.ResetEcu(&H7E0, EcuResetType.KeyOffOnReset)
```

**Reset Types:**
- `HardReset` (0x01) - Power cycle
- `KeyOffOnReset` (0x02) - Simulates key cycle
- `SoftReset` (0x03) - Software restart

---

### Read Data By Identifier (0x22)

Read ECU data by DID (Data Identifier).

```vb
' Read VIN
Dim response = udsEngine.ReadVIN(&H7E0)

' Read ECU serial number
Dim response = udsEngine.ReadEcuSerialNumber(&H7E0)

' Read software version
Dim response = udsEngine.ReadSoftwareVersion(&H7E0)

' Read any DID
Dim response = udsEngine.ReadDataByIdentifier(&H7E0, &HF190)

' Read multiple DIDs
Dim response = udsEngine.ReadMultipleDataByIdentifier(&H7E0, {&HF190, &HF18C, &HF195})
```

**Common DIDs:**
| DID | Description |
|-----|-------------|
| F186 | Active Diagnostic Session |
| F18C | ECU Serial Number |
| F190 | VIN |
| F191 | Hardware Number |
| F195 | Software Version |

---

### Write Data By Identifier (0x2E)

Write data to ECU (requires security access in most cases).

```vb
' Write data
Dim data As Byte() = {&H01, &H02, &H03}
Dim response = udsEngine.WriteDataByIdentifier(&H7E0, &HF199, data)
```

**⚠️ Warning:** Writing incorrect data can damage the ECU!

---

### Security Access (0x27)

Unlock ECU security levels.

```vb
' Step 1: Request seed
Dim seedResponse = udsEngine.RequestSecuritySeed(&H7E0, &H1)

If seedResponse.IsSuccess Then
    ' Step 2: Calculate key (algorithm depends on ECU)
    Dim key = udsEngine.CalculateSecurityKey(seedResponse.Data, algorithm:=0)
    
    ' Step 3: Send key
    Dim keyResponse = udsEngine.SendSecurityKey(&H7E0, &H2, key)
End If
```

**Security Levels:**
- Odd numbers (1, 3, 5...) = Request Seed
- Even numbers (2, 4, 6...) = Send Key

**Note:** Key calculation algorithm is ECU-specific. The built-in algorithm is a placeholder.

---

### Routine Control (0x31)

Execute ECU routines.

```vb
' Start a routine
Dim response = udsEngine.StartRoutine(&H7E0, &HFF00)

' Stop a routine
Dim response = udsEngine.StopRoutine(&H7E0, &HFF00)

' Get routine results
Dim response = udsEngine.RequestRoutineResults(&H7E0, &HFF00)

' Start routine with options
Dim options As Byte() = {&H01, &H02}
Dim response = udsEngine.StartRoutine(&H7E0, &HFF00, options)
```

**Common Routines:**
- Clear Adaptation
- Activate Actuator
- Check Programming Preconditions
- Erase Memory

---

### IO Control By Identifier (0x2F)

Control ECU inputs/outputs directly.

```vb
' Return control to ECU
Dim response = udsEngine.ReturnIoControlToEcu(&H7E0, &H3000)

' Short-term adjustment
Dim value As Byte() = {&HFF}
Dim response = udsEngine.ShortTermIoAdjustment(&H7E0, &H3000, value)
```

**Control Types:**
- `ReturnControlToEcu` (0x00) - Release control
- `ResetToDefault` (0x01) - Reset to default values
- `FreezeCurrentState` (0x02) - Lock current state
- `ShortTermAdjustment` (0x03) - Temporary change

---

### Request Download/Upload (0x34, 0x35)

Flash programming support (structure only).

```vb
' Request download (prepare for firmware upload)
Dim response = udsEngine.RequestDownload(&H7E0, &H10000, &H2000)

' Transfer data
Dim block As Byte() = {...firmware data...}
Dim response = udsEngine.TransferData(&H7E0, &H1, block)

' Exit transfer
Dim response = udsEngine.RequestTransferExit(&H7E0)
```

**⚠️ Warning:** Incorrect firmware can permanently brick the ECU!

---

### DTC Operations (0x14, 0x19)

Read and clear Diagnostic Trouble Codes.

```vb
' Read all DTCs
Dim response = udsEngine.ReadAllDtcs(&H7E0)

' Clear all DTCs
Dim response = udsEngine.ClearDtc(&H7E0)

' Clear specific group
Dim response = udsEngine.ClearDtc(&H7E0, &H010000)
```

---

### Tester Present (0x3E)

Keep diagnostic session alive.

```vb
' Send Tester Present (suppress response)
Dim response = udsEngine.SendTesterPresent(&H7E0)

' Send Tester Present (expect response)
Dim response = udsEngine.SendTesterPresent(&H7E0, suppressPositiveResponse:=False)
```

**Best Practice:** Send every 2-5 seconds to prevent session timeout.

---

## ISO-TP Support

The engine handles ISO-TP (ISO 15765-2) segmentation automatically.

### Single Frame (≤7 bytes)
```
[PCI] [Data...]
 0N    N bytes of data
```

### Multi Frame
```
First Frame:  [1L] [LL] [Data...]     (6 bytes data)
Consecutive:  [2N] [Data...]          (7 bytes data)
Flow Control: [30] [BS] [ST]          (Block size, Separation time)
```

### API

```vb
' Build ISO-TP frames from message
Dim frames = udsEngine.BuildIsoTpFrames(udsMessage)

' Parse ISO-TP frames to message
Dim message = udsEngine.ParseIsoTpFrames(receivedFrames)

' Build flow control frame
Dim fc = udsEngine.BuildFlowControlFrame(blockSize:=0, stMin:=10)
```

---

## Response Handling

### UdsResponse Object

```vb
Public Class UdsResponse
    Property Status As UdsResponseStatus
    Property ServiceId As Byte
    Property Data As Byte()
    Property RawFrames As List(Of Byte())
    Property NegativeResponseCode As Byte
    Property ErrorMessage As String
    
    ReadOnly Property IsSuccess As Boolean
    Function GetNrcDescription() As String
End Class
```

### Status Types

```vb
Enum UdsResponseStatus
    Success = 0
    Pending = 1
    NegativeResponse = 2
    Timeout = 3
    InvalidFormat = 4
    SecurityDenied = 5
    ' ...
End Enum
```

### Negative Response Codes (NRC)

| Code | Description |
|------|-------------|
| 0x10 | General Reject |
| 0x11 | Service Not Supported |
| 0x12 | Sub-Function Not Supported |
| 0x13 | Incorrect Message Length |
| 0x22 | Conditions Not Correct |
| 0x33 | Security Access Denied |
| 0x35 | Invalid Key |
| 0x78 | Response Pending |

---

## Best Practices

1. **Always start with Default Session**
   ```vb
   udsEngine.StartDiagnosticSession(ecuId, UdsSessionType.DefaultSession)
   ```

2. **Send Tester Present regularly**
   ```vb
   ' Every 3 seconds
   udsEngine.SendTesterPresent(ecuId)
   ```

3. **Check response status**
   ```vb
   If response.IsSuccess Then
       ' Process data
   Else
       Console.WriteLine(response.GetNrcDescription())
   End If
   ```

4. **Handle security access properly**
   - Request seed first
   - Calculate key using correct algorithm
   - Send key within timeout

5. **Use Extended Session for most operations**
   ```vb
   udsEngine.StartExtendedSession(ecuId)
   ```

