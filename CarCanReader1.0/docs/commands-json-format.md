# CarCANReader Pro 3.1 - Commands JSON Format

## Overview

The `commands.json` file stores all discovered and saved CAN commands in a hierarchical JSON structure.

## File Location

```
CarCanReader1.0/
├── commands.json          # Main command database
└── commands.json.backup   # Automatic backup (created on save)
```

## Structure

```json
{
  "Brand": {
    "Model": {
      "Module": {
        "CommandName": "frame_data"
      }
    }
  }
}
```

### Hierarchy Levels

| Level | Description | Example |
|-------|-------------|---------|
| Brand | Vehicle manufacturer | "Volkswagen", "BMW", "Toyota" |
| Model | Specific vehicle model | "Golf MK7", "3 Series F30" |
| Module | ECU or system | "BCM", "Gateway", "Engine" |
| Command | Feature name | "NeedlesSweep", "FoldMirrors" |

## Command Formats

### 1. Simple String (Direct Frame)

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

### 2. Toggle Object (ON/OFF)

```json
{
  "Volkswagen": {
    "Golf MK7": {
      "BCM": {
        "ComingHome": {
          "type": "toggle",
          "on": "t7600810FF00000000",
          "off": "t760081000FF000000",
          "byte": 1
        }
      }
    }
  }
}
```

## API Usage

```vb
' Load commands
Dim repo As New CommandRepository()
repo.Load()

' Get hierarchy
Dim brands = repo.GetBrands()
Dim models = repo.GetModels("Volkswagen")
Dim modules = repo.GetModules("Volkswagen", "Golf MK7")
Dim commands = repo.GetCommands("Volkswagen", "Golf MK7", "BCM")

' Add command
repo.AddCommand("VW", "Golf", "BCM", "Test", "t7600810AABB")
repo.Save()
```

