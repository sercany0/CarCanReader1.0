# CarCANReader Pro 3.1 - Filter System

## Overview

The Filter System controls which CAN frames appear in the log display. **Critically, filtering affects ONLY the log output** - Dashboard, LearningEngine, and other subsystems receive ALL frames regardless of filter settings.

---

## Design Philosophy

```
                    ┌──────────────────────┐
                    │   Incoming Frame     │
                    └──────────┬───────────┘
                               │
           ┌───────────────────┼───────────────────┐
           │                   │                   │
           ▼                   ▼                   ▼
   ┌───────────────┐  ┌───────────────┐  ┌───────────────┐
   │ Dashboard     │  │ Learning      │  │ Log Display   │
   │ Manager       │  │ Engine        │  │               │
   │ (NO filter)   │  │ (NO filter)   │  │ (Filter HERE) │
   └───────────────┘  └───────────────┘  └───────────────┘
```

**Why?**
- Dashboard must show real-time vehicle data regardless of filter
- LearningEngine must see all frames for accurate analysis
- Filter is purely a UI convenience for the log display

---

## Filter Modes

### None (Disabled)
All frames appear in log.

### ShowOnlyId
Only frames with the specified ID appear.

```
Filter: ShowOnlyId = 0x123
Frame 0x123 → SHOW
Frame 0x456 → HIDE
Frame 0x789 → HIDE
```

### HideId
Hide frames with the specified ID, show all others.

```
Filter: HideId = 0x123
Frame 0x123 → HIDE
Frame 0x456 → SHOW
Frame 0x789 → SHOW
```

### ShowRange
Show frames within the specified ID range (inclusive).

```
Filter: ShowRange = 0x100 to 0x200
Frame 0x0FF → HIDE
Frame 0x100 → SHOW
Frame 0x150 → SHOW
Frame 0x200 → SHOW
Frame 0x201 → HIDE
```

### HideRange
Hide frames within the specified ID range.

```
Filter: HideRange = 0x100 to 0x200
Frame 0x0FF → SHOW
Frame 0x100 → HIDE
Frame 0x150 → HIDE
Frame 0x200 → HIDE
Frame 0x201 → SHOW
```

---

## FilterManager API

### Properties

```vb
ReadOnly Property IsEnabled As Boolean
ReadOnly Property CurrentMode As FilterMode
```

### Methods

```vb
' Apply single-ID filter
Sub ApplyFilter(mode As FilterMode, id As Integer)

' Apply range filter
Sub ApplyRangeFilter(mode As FilterMode, fromId As Integer, toId As Integer)

' Disable filter
Sub ClearFilter()

' Check if ID should appear in log
Function ShouldShowInLog(id As Integer) As Boolean
```

---

## UI Controls

| Control | Purpose |
|---------|---------|
| `cmbFilterMode` | Select filter mode |
| `txtFilterId` | Single ID input |
| `txtFilterFrom` | Range start |
| `txtFilterTo` | Range end |
| `btnApplyFilter` | Apply filter |
| `btnClearFilter` | Clear filter |

---

## Usage in MainForm

### ProcessSlcanFrame Flow

```vb
Private Sub ProcessSlcanFrame(frame As String)
    ' Parse the frame
    Dim parsed = canParser.Parse(frame)
    If Not parsed.IsValid Then Return
    
    ' ALWAYS send to Dashboard (no filter!)
    dashboardManager.ProcessFrame(parsed.Id, parsed.Data)
    
    ' ALWAYS send to Learning Engine (no filter!)
    If learningEngine.IsRunning Then
        learningEngine.AnalyzeFrame(parsed.Id, parsed.Data)
    End If
    
    ' Apply filter ONLY to log display
    If Not filterManager.ShouldShowInLog(parsed.Id) Then
        Return  ' Don't add to log, but processing already done
    End If
    
    ' Add to log
    AddLog($"RX: {frame}")
End Sub
```

### btnApplyFilter_Click

```vb
Private Sub btnApplyFilter_Click(sender As Object, e As EventArgs)
    Try
        Dim mode = CType(cmbFilterMode.SelectedIndex, FilterMode)
        
        If mode = FilterMode.ShowRange OrElse mode = FilterMode.HideRange Then
            Dim fromId = Convert.ToInt32(txtFilterFrom.Text, 16)
            Dim toId = Convert.ToInt32(txtFilterTo.Text, 16)
            filterManager.ApplyRangeFilter(mode, fromId, toId)
        Else
            Dim id = Convert.ToInt32(txtFilterId.Text, 16)
            filterManager.ApplyFilter(mode, id)
        End If
        
        AddLog($"[Filter] {mode} applied")
    Catch ex As Exception
        AddLog($"[Filter] Error: {ex.Message}")
    End Try
End Sub
```

### btnClearFilter_Click

```vb
Private Sub btnClearFilter_Click(sender As Object, e As EventArgs)
    filterManager.ClearFilter()
    AddLog("[Filter] Cleared")
End Sub
```

---

## Filter State Display

Optional: Show current filter in status area.

```vb
Private Sub UpdateFilterStatus()
    If filterManager.IsEnabled Then
        lblFilterStatus.Text = $"Filter: {filterManager.CurrentMode}"
    Else
        lblFilterStatus.Text = "Filter: Off"
    End If
End Sub
```

---

## Common Use Cases

### Monitor Specific ECU

```
Goal: Focus on Engine ECU (ID 0x7E0-0x7E8)
Mode: ShowRange
From: 7E0
To:   7E8
```

### Hide Noisy IDs

```
Goal: Hide ABS spam (ID 0x450)
Mode: HideId
ID:   450
```

### Focus on Unknown IDs

```
Goal: Show only non-standard IDs (above 0x700)
Mode: ShowRange
From: 700
To:   7FF
```

### Hide Known Dashboard IDs

```
Goal: Hide known dashboard traffic
Mode: HideRange
From: 100
To:   400
Result: Show only IDs 0x401 and above
```

---

## Important Notes

### DO
- Use filter to reduce log noise
- Apply filter when analyzing specific subsystem
- Clear filter before using LearningEngine (for best results)

### DON'T
- Expect filter to affect Dashboard updates
- Expect filter to affect LearningEngine analysis
- Use filter during full system discovery

---

## Troubleshooting

### "Dashboard stopped updating"
- Filter does NOT affect Dashboard
- Check CAN connection
- Check if relevant frames are being received

### "LearningEngine not detecting changes"
- Filter does NOT affect LearningEngine
- Ensure LearningEngine.IsRunning = True
- Check if the button/feature you're testing sends CAN frames

### "No frames in log"
- Check filter settings
- Try Clear Filter
- Verify CAN connection

