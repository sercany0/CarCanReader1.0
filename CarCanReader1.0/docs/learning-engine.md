# CarCANReader Pro 3.1 - Learning Engine

## Overview

The Learning Engine monitors CAN traffic to detect byte-level changes and classify signals. It's the core tool for reverse engineering vehicle features.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      LearningEngine                              │
│                                                                  │
│  ┌─────────────────┐    ┌──────────────────────────────────┐   │
│  │  Frame History  │    │      Change Tracking             │   │
│  │  (per CAN ID)   │    │  • Byte change counts            │   │
│  │                 │    │  • Value history (last N)        │   │
│  │  ID → Byte[]    │    │  • Transition patterns           │   │
│  └────────┬────────┘    └────────────────┬─────────────────┘   │
│           │                              │                      │
│           ▼                              ▼                      │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Signal Classifier                          │   │
│  │  • OnOff detection                                      │   │
│  │  • Sensor detection                                     │   │
│  │  • Command burst detection                              │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│                              ▼                                  │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    Events                               │   │
│  │  • OnByteChange                                         │   │
│  │  • OnSignalClassified                                   │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Signal Types

### OnOff
A byte that toggles between two distinct values.

**Characteristics:**
- Only 2 unique values observed
- Sharp transitions
- Examples: button press, door sensor, light switch

```
Byte history: 00 00 00 FF 00 00 FF FF 00
Classification: OnOff (toggles 00 ↔ FF)
```

### IncreasingSensor
A byte that gradually increases in value.

**Characteristics:**
- Values trend upward
- Multiple unique values
- Examples: RPM increasing, temperature rising

```
Byte history: 10 12 15 18 1A 20 25 2A
Classification: IncreasingSensor
```

### DecreasingSensor
A byte that gradually decreases in value.

**Characteristics:**
- Values trend downward
- Multiple unique values
- Examples: fuel level dropping, coolant temp after engine off

```
Byte history: FF F0 E0 D0 C0 B0 A0
Classification: DecreasingSensor
```

### CommandBurst
A byte that changes rapidly many times.

**Characteristics:**
- Very high change frequency
- Often cyclic or rolling
- Examples: message counter, checksum, rolling code

```
Byte history: 01 02 03 04 05 06 07 08 09 0A 0B...
Classification: CommandBurst
```

### MultiByte
Multiple bytes changing together.

**Characteristics:**
- 2+ bytes change simultaneously
- Often related values (16-bit sensor split across bytes)
- Examples: RPM (2 bytes), wheel speed

```
ID 0x123:
  Byte[0]: 03 → 04 (changed)
  Byte[1]: A0 → B0 (changed)
  Byte[2]: 00 → 00 (unchanged)
Classification: MultiByte at bytes 0-1
```

### Unknown
Insufficient data or unrecognizable pattern.

---

## API Reference

### Start/Stop

```vb
' Start monitoring
learningEngine.Start()

' Stop monitoring
learningEngine.Stop()

' Reset all collected data
learningEngine.Reset()

' Check if running
If learningEngine.IsRunning Then ...
```

### Analyze Frame

Called automatically from ProcessSlcanFrame:

```vb
learningEngine.AnalyzeFrame(id As Integer, data As Byte())
```

### Query Results

```vb
' Get IDs sorted by change frequency
Dim topIds = learningEngine.GetTopChangedIds(limit:=10)

' Get recent byte changes
Dim changes = learningEngine.GetRecentChanges(count:=50)
```

### Events

```vb
' Fires when any byte changes
Event OnByteChange(id As Integer, byteIndex As Integer, 
                   oldValue As Byte, newValue As Byte)

' Fires when signal type is determined
Event OnSignalClassified(id As Integer, byteIndex As Integer, 
                         signalType As SignalType)

Event OnStarted()
Event OnStopped()
```

---

## Usage Workflow

### Discovering a Button

1. **Start Learning**
   ```
   Click [Start Diff]
   Status: "Learning mode active"
   ```

2. **Establish Baseline**
   ```
   Wait 5-10 seconds
   Let the engine see normal traffic
   ```

3. **Trigger Feature**
   ```
   Press the button you want to discover
   (e.g., window up, lock door, turn on lights)
   ```

4. **Observe Changes**
   ```
   lstDiff shows:
   "[14:32:05] ID 0x450 Byte[2]: 00 → 01"
   ```

5. **Repeat & Confirm**
   ```
   Release button:
   "[14:32:06] ID 0x450 Byte[2]: 01 → 00"
   
   Pattern confirmed: 0x450 Byte[2] is the button
   ```

6. **Stop & Save**
   ```
   Click [Stop Diff]
   Save command to JSON
   ```

---

## UI Display

### lstDiff Format

```
[12:30:45] ID 0x123 Byte[0]: A5 → A6 (likely sensor)
[12:30:45] ID 0x123 Byte[1]: 00 → FF (likely toggle)
[12:30:46] ID 0x450 Byte[3]: 10 → 11 (command burst)
```

### lstLearnIds Format

```
0x123 - 145 changes - [Sensor @ B0-B1]
0x450 - 23 changes - [Toggle @ B2]
0x200 - 892 changes - [Counter @ B7]
```

---

## IdChangeStats Class

```vb
Public Class IdChangeStats
    Public Property CanId As Integer
    Public Property TotalChanges As Integer
    Public Property ByteChangeCounts As Dictionary(Of Integer, Integer)
    Public Property ClassifiedSignals As Dictionary(Of Integer, SignalType)
    Public Property LastSeen As DateTime
    
    Public Function GetMostActiveByteIndex() As Integer
    Public Function GetSummary() As String
End Class
```

---

## ByteChangeInfo Class

```vb
Public Class ByteChangeInfo
    Public Property CanId As Integer
    Public Property ByteIndex As Integer
    Public Property OldValue As Byte
    Public Property NewValue As Byte
    Public Property Timestamp As DateTime
    Public Property SignalType As SignalType
End Class
```

---

## Classification Algorithm

```vb
Private Function ClassifySignal(history As List(Of Byte)) As SignalType
    If history.Count < MinSamples Then Return SignalType.Unknown
    
    Dim uniqueValues = history.Distinct().Count()
    
    ' Only 2 values = toggle
    If uniqueValues = 2 Then Return SignalType.OnOff
    
    ' Many values, check trend
    Dim increasing = True
    Dim decreasing = True
    
    For i = 1 To history.Count - 1
        If history(i) < history(i-1) Then increasing = False
        If history(i) > history(i-1) Then decreasing = False
    Next
    
    If increasing Then Return SignalType.IncreasingSensor
    If decreasing Then Return SignalType.DecreasingSensor
    
    ' Very high change rate = counter/checksum
    Dim changeRate = CalculateChangeRate(history)
    If changeRate > 0.9 Then Return SignalType.CommandBurst
    
    Return SignalType.Unknown
End Function
```

---

## Performance Considerations

### Memory Management

```vb
' Limit history per ID to prevent memory bloat
Private Const MaxHistoryPerByte As Integer = 100

' Purge old entries
Private Sub PurgeOldData()
    Dim cutoff = DateTime.Now.AddMinutes(-5)
    ' Remove entries older than cutoff
End Sub
```

### Ignore List

Some IDs are known counters/checksums and can be ignored:

```vb
Private ReadOnly IgnoreIds As New HashSet(Of Integer) From {
    &H0AA,  ' Common checksum
    &H0AB,  ' Rolling counter
    ' Add more as discovered
}
```

---

## Best Practices

### 1. Clear Baseline First
```vb
learningEngine.Reset()
learningEngine.Start()
' Wait for baseline to establish
```

### 2. One Feature at a Time
```
❌ Don't: Press multiple buttons simultaneously
✓ Do: Press one button, wait, observe
```

### 3. Repeat for Confidence
```
❌ Don't: Accept first change as definitive
✓ Do: Trigger feature 3+ times to confirm pattern
```

### 4. Note False Positives
```
Counters and checksums change constantly
Filter these out mentally or add to ignore list
```

### 5. Use with Filter Wisely
```
Filter ONLY affects log display
LearningEngine sees ALL traffic regardless of filter
This is by design for accurate analysis
```

---

## Troubleshooting

### "Too many changes, hard to find feature"
- Add known noisy IDs to ignore list
- Focus on IDs with low change count
- Use timing: trigger feature, note timestamp

### "No changes detected"
- Ensure vehicle ignition is ON
- Verify CAN connection
- Check if feature actually sends CAN (some are internal only)

### "Button detected but frame doesn't work"
- May require specific sequence
- May require Extended Session
- May have checksum byte that needs calculation

