# Available Commands — autocad-addin

All commands are implemented in `AutoCADMCPPlugin.dll` and auto-discovered at startup.
Commands that need the AutoCAD document extend `DocumentContextCommandBase` and run inside a `LockDocument()` context.

---

## Test / Utility

| Command | Class | Needs Doc? |
|---------|-------|-----------|
| `say_hello` | `SayHelloCommand` | No |
| `write_message` | `WriteMessageCommand` | Yes |

### `say_hello`
Test connection to AutoCAD. Returns greeting + timestamp.
```json
{ "message": "Hello!" }
```

### `write_message`
Writes text to the AutoCAD command line (`Editor.WriteMessage`).
```json
{ "message": "Hello from Claude!" }
```

---

## Query / Read

| Command | Class | Description |
|---------|-------|-------------|
| `get_document_info` | `GetDocumentInfoCommand` | DWG file name, units, extents, limits |
| `get_layers` | `GetLayersCommand` | All layers with state, color, linetype |
| `get_entities` | `GetEntitiesCommand` | Model space entities, filterable |

### `get_document_info`
No parameters. Returns:
```json
{
  "fileName": "Drawing1.dwg",
  "fullPath": "C:\\...",
  "units": "Millimeters",
  "limMin": { "x": 0, "y": 0 },
  "limMax": { "x": 420, "y": 297 },
  "extMin": { "x": 0, "y": 0, "z": 0 },
  "extMax": { "x": 100, "y": 100, "z": 0 }
}
```

### `get_layers`
No parameters. Returns array of layer objects:
```json
[
  {
    "name": "0",
    "isOn": true,
    "isFrozen": false,
    "isLocked": false,
    "colorIndex": 7,
    "linetype": "Continuous"
  }
]
```

### `get_entities`
Returns entities from model space. All parameters optional:
```json
{
  "limit": 100,
  "layer": "WALLS",
  "type": "LINE"
}
```
`type` filters by DXF name: `LINE`, `CIRCLE`, `ARC`, `LWPOLYLINE`, `TEXT`, `MTEXT`, `INSERT`, `DIMENSION`, etc.

Returns:
```json
[
  {
    "objectId": "...",
    "type": "Line",
    "dxfType": "LINE",
    "layer": "0",
    "color": "ByLayer",
    "handle": "1A"
  }
]
```

---

## Create

| Command | Class | Description |
|---------|-------|-------------|
| `create_line` | `CreateLineCommand` | Creates a LINE entity in model space |

### `create_line`
```json
{
  "startX": 0,
  "startY": 0,
  "startZ": 0,
  "endX": 100,
  "endY": 100,
  "endZ": 0,
  "layer": "WALLS"
}
```
`startZ`, `endZ`, `layer` are optional (defaults: 0, 0, current layer "0").

Returns:
```json
{
  "objectId": "...",
  "handle": "2B",
  "startPoint": { "x": 0, "y": 0, "z": 0 },
  "endPoint": { "x": 100, "y": 100, "z": 0 },
  "layer": "WALLS"
}
```

---

## Dynamic Execution

| Command | Class | Description |
|---------|-------|-------------|
| `send_code_to_autocad` | `SendCodeToAutoCADCommand` | Compile and run C# code inside AutoCAD at runtime |

### `send_code_to_autocad`
```json
{
  "code": "var count = 0;\nusing(var tr = db.TransactionManager.StartTransaction()) {\n  var ms = (BlockTableRecord)tr.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead);\n  foreach(ObjectId id in ms) count++;\n  tr.Commit();\n}\nreturn count;"
}
```

Pre-injected variables available in code:
- `doc` — `Autodesk.AutoCAD.ApplicationServices.Document`
- `db` — `Autodesk.AutoCAD.DatabaseServices.Database`

Pre-imported namespaces:
- `Autodesk.AutoCAD.ApplicationServices`
- `Autodesk.AutoCAD.DatabaseServices`
- `Autodesk.AutoCAD.Geometry`
- `Autodesk.AutoCAD.EditorInput`

Use `return <value>;` to get output back to Claude.

---

## Adding More Commands

See [patterns.md](patterns.md) — Adding a New MCP Command (3 steps).

Commands currently missing compared to Revit plugin (good candidates to add):
- `create_circle` — `Circle` entity
- `create_text` — `DBText` / `MText` entity
- `create_layer` — add a new layer to `LayerTable`
- `get_selected_entities` — `Editor.SelectImplied()` / `SelectAll()`
- `zoom_extents` — `Editor.Command("ZOOM", "E")`
- `color_entities` — change `Entity.Color` by layer/handle
- `delete_entity` — `entity.Erase()`
- `create_polyline` — `Polyline` with multiple vertices
- `create_hatch` — `Hatch` entity with boundary
- `export_to_dxf` — `db.DxfOut()`
