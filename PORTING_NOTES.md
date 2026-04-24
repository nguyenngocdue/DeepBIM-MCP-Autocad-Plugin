# Porting Notes: Revit-MCP-Plugin → AutoCAD-MCP-Plugin

## Tóm tắt kiến trúc gốc (Revit)

```
plugin/                  ← Entry point (IExternalApplication + IExternalCommand)
  Core/
    Application.cs       ← IExternalApplication: OnStartup/OnShutdown
    SocketService.cs     ← TCP + HTTP listener (background threads)
    CommandManager.cs    ← Loads commands from config (assembly scan)
    CommandExecutor.cs   ← JsonRPC dispatcher → IRevitCommand.Execute()
    RevitCommandRegistry.cs  ← Dictionary<name, IRevitCommand>
    ExternalEventManager.cs  ← Manages Revit ExternalEvents (thread-safe bridge)
    MCPServiceConnection.cs  ← IExternalCommand (Ribbon button → open control panel)
  Configuration/
    ConfigurationManager.cs  ← Load JSON config (command.json / commandRegistry.json)
    CommandConfig.cs     ← DTO
    FrameworkConfig.cs   ← Root config DTO
  Utils/
    Logger.cs            ← ILogger implementation (file + debug output)
    PathManager.cs       ← Resolves plugin/log/command directory paths

commandset/              ← IRevitCommand implementations (loaded at runtime)
  Commands/              ← Access, Architecture, DataExtraction, etc.
  Services/              ← ExternalEventHandler implementations (actual Revit API calls)
  Models/                ← DTO / result models
```

---

## Mapping: Revit API → AutoCAD API

| Revit | AutoCAD | Ghi chú |
|-------|---------|---------|
| `IExternalApplication` | `IExtensionApplication` | Entry point tương đương |
| `UIControlledApplication` | Không có tương đương trực tiếp | AutoCAD không có UIControlledApp, dùng `Application.DocumentManager` |
| `IExternalCommand` | `[CommandMethod]` attribute | AutoCAD dùng attribute trên method thay vì interface |
| `ExternalEvent` | `Application.Idle` event / queue | Revit có ExternalEvent built-in; AutoCAD cần tự implement queue |
| `UIApplication.ActiveUIDocument` | `Application.DocumentManager.MdiActiveDocument` | |
| `Document` (Revit DB) | `Database` (AutoCAD DatabaseServices) | |
| `Transaction` | `Transaction` (`DatabaseServices`) | Cùng tên nhưng khác namespace |
| `TaskDialog.Show(...)` | `Editor.WriteMessage(...)` | AutoCAD không có modal dialog built-in cho messages |
| `Ribbon / PushButtonData` | AutoCAD không có ribbon API trong .NET addin thông thường | Bỏ qua ribbon, dùng command thay thế |
| `ElementId` | `ObjectId` | |
| `FilteredElementCollector` | `SelectionSet` / `TypedValue[]` filter | |
| `RevitVersionAdapter` | Không cần | AutoCAD version đơn giản hơn |

---

## Phần đã port (100% giống Revit)

| Component | Status | Ghi chú |
|-----------|--------|---------|
| `SocketService.cs` | ✅ Ported | Không phụ thuộc Revit, chỉ cần thay `UIApplication` ref |
| `CommandExecutor.cs` | ✅ Ported | Hoàn toàn không có Revit API |
| `CommandRegistry.cs` | ✅ Ported | Chỉ thay `IRevitCommand` → `IAutoCADCommand` |
| `CommandManager.cs` | ✅ Ported | Thay `UIApplication`, `RevitVersionAdapter`, interface |
| `ConfigurationManager.cs` | ✅ Ported | Không có Revit dep |
| `CommandConfig.cs` | ✅ Ported | Không có Revit dep |
| `FrameworkConfig.cs` | ✅ Ported | Không có Revit dep |
| `ServiceSettings.cs` | ✅ Ported | Không có Revit dep |
| `Logger.cs` | ✅ Ported | Không có Revit dep |
| `PathManager.cs` | ✅ Ported | Thay app name "DeepBim-MCP" → "DeepBim-MCP-ACAD" |
| `AIResult<T>` | ✅ Ported | Không có Revit dep |
| `IAutoCADCommand` interface | ✅ Created | Thay `IRevitCommand` |

---

## Phần thay thế (Revit → AutoCAD)

| Component | Thay thế | Ghi chú |
|-----------|----------|---------|
| `Application.cs` (IExternalApplication) | `AutoCADMCPApplication.cs` (IExtensionApplication) | `Initialize()` / `Terminate()` thay OnStartup/OnShutdown |
| `ExternalEventManager.cs` | `DocumentContextQueue.cs` | AutoCAD không có ExternalEvent; dùng `Application.Idle` + ConcurrentQueue |
| `ExternalEventCommandBase` (RevitMCPSDK) | `DocumentContextCommandBase.cs` | Base class tự implement cho AutoCAD |
| `RevitCommandRegistry` | `AutoCADCommandRegistry` | Thay `IRevitCommand` → `IAutoCADCommand` |
| `MCPServiceConnection.cs` (Ribbon button) | `[CommandMethod("MCPSTART")]` | |
| Ribbon panel UI | Không port | AutoCAD .NET addin không có ribbon API dễ dàng |
| WPF UI (Settings, StatusWindow) | Không port (TODO) | Có thể thêm sau nếu cần |

---

## Phần chưa port / TODO

| Revit Tool | Lý do chưa port | Tương đương AutoCAD (TODO) |
|------------|-----------------|---------------------------|
| `create_room` | Rooms là concept Revit Architecture | AutoCAD không có rooms |
| `create_level` | Levels là concept Revit | AutoCAD dùng Z coordinate |
| `create_structural_framing_system` | Revit Structure specific | AutoCAD dùng LINE/POLYLINE entities |
| `tag_rooms`, `tag_walls` | Revit annotation system | AutoCAD dùng MTEXT / LEADER |
| `export_sheets_to_excel` | Revit Sheet concept | AutoCAD dùng Layout |
| `create_dimensions` | Revit dimension type | AutoCAD dùng DIMLINEAR, DIMALIGNED |
| `color_splash` | Revit Override Graphics | AutoCAD thay đổi layer/color của entity |
| `get_material_quantities` | Revit material extraction | AutoCAD không có built-in, cần custom |
| WPF Settings UI | Complex Revit-specific | TODO: port nếu cần |

---

## Các tool đã port cho AutoCAD

| AutoCAD Tool | Tương ứng Revit | Mô tả |
|--------------|-----------------|-------|
| `say_hello` | `say_hello` | Test command |
| `get_document_info` | `get_current_view_info` | Thông tin document/drawing hiện tại |
| `get_layers` | (không có tương đương trực tiếp) | Liệt kê tất cả layers |
| `create_line` | `create_line_based_element` | Tạo line đơn giản |
| `write_message` | (debug helper) | Ghi message ra AutoCAD editor |
| `get_entities` | `get_current_view_elements` | Lấy entities trong model space |
| `send_code_to_autocad` | `send_code_to_revit` | Execute dynamic C# code trong AutoCAD |

---

## Quyết định thiết kế

### 1. Thread safety bridge
Revit dùng `ExternalEvent` (built-in trong Revit API) để bridge từ background thread sang Revit main thread.  
AutoCAD không có cơ chế tương đương built-in.  
**Giải pháp**: `DocumentContextQueue` + `Application.Idle` event — khi idle, AutoCAD drains queue và thực thi action có document lock.

### 2. Không có IRevitCommandInitializable / UIApplication inject
Trong Revit, một số commands nhận `UIApplication` qua constructor hoặc `IRevitCommandInitializable`.  
Trong AutoCAD, `IAutoCADCommand` cũng cho phép inject qua constructor (truyền `Document` hoặc không cần gì vì `Application.DocumentManager.MdiActiveDocument` là global).

### 3. Không port Ribbon UI
AutoCAD .NET API có thể tạo ribbon nhưng cần RibbonControl/RibbonTab từ `Autodesk.AutoCAD.Windows` — phức tạp và không cần thiết cho MCP use case. Dùng `[CommandMethod]` là đủ.

### 4. Command registry từ JSON (giống Revit)
Giữ cơ chế load command từ `command.json` để plugin có thể extend mà không cần sửa core.
