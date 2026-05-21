# Kế Hoạch Triển Khai: Sheet Duplicator Add-in

> **Mục tiêu**: Thêm tính năng nhân bản Sheet (bản vẽ) vào add-in BTVN1, cho phép người dùng chọn nhiều sheet và nhân bản toàn bộ nội dung (views, legends, schedules) sang sheet mới.

---

## 1. Phân Tích Yêu Cầu

### 1.1 Yêu cầu chức năng
| # | Yêu cầu | Ghi chú |
|---|---------|---------|
| R1 | Hiển thị danh sách tất cả Sheet trong project | Sheet Number + Sheet Name + số viewport |
| R2 | Người dùng chọn nhiều sheet cùng lúc | Multi-select bằng checkbox |
| R3 | Nhân bản toàn bộ các sheet đã chọn | Tạo sheet mới cho mỗi sheet gốc |
| R4 | Sao chép tất cả viewport trên sheet | Giữ nguyên vị trí + tỷ lệ |
| R5 | Xử lý Legend View | Legend có thể đặt trên nhiều sheet — dùng lại, không cần duplicate |
| R6 | Xử lý Schedule (bảng biểu) | ScheduleSheetInstance — tạo instance mới cùng schedule |
| R7 | Xử lý View thường (FloorPlan, Section…) | Phải duplicate view trước, sau đó đặt lên sheet mới |
| R8 | Đặt tên sheet mới | Thêm prefix/suffix do người dùng nhập |
| R9 | Chọn chế độ duplicate view | Independent / WithDetailing |
| R10 | Báo kết quả | Số sheet thành công / lỗi |

### 1.2 Giới hạn Revit API quan trọng
- **View thường chỉ được đặt trên MỘT sheet** — phải duplicate view mới trước khi đặt.
- **Legend View** có thể đặt trên nhiều sheet — dùng lại nguyên bản, không cần duplicate.
- **ScheduleSheetInstance** — Schedule có thể được đặt nhiều lần qua `ScheduleSheetInstance.Create()`.
- Một số View không hỗ trợ duplicate (ví dụ: ViewSheet bản thân, 3D perspective cũ) — cần kiểm tra `view.CanViewBeDuplicated()`.
- Title block trên sheet là `FamilyInstance` thuộc category `OST_TitleBlocks`.

---

## 2. Kiến Trúc MVVM

```
Commands/
  SheetDuplicatorCommand.cs      ← ExternalCommand, điều phối UI + Service

Models/
  SheetInfo.cs                   ← DTO cho từng sheet hiển thị trong danh sách
  SheetDuplicatorSettings.cs     ← Cài đặt từ dialog (prefix, suffix, mode)
  DuplicateResult.cs             ← Kết quả từng sheet (thành công/lỗi/cảnh báo)

ViewModels/
  SheetDuplicatorViewModel.cs    ← INotifyPropertyChanged, ObservableCollection

Views/
  SheetDuplicatorWindow.xaml     ← WPF Dialog
  SheetDuplicatorWindow.xaml.cs  ← Code-behind (validate + lấy settings)

Services/
  SheetDuplicatorService.cs      ← Toàn bộ logic nhân bản (Revit API calls)
```

**Sửa đổi file có sẵn:**
- `Application.cs` — Thêm ribbon button "Nhân Bản Sheet"

---

## 3. Thiết Kế UI (WPF Dialog)

```
┌─────────────────────────────────────────────────────────────┐
│  [ICON]  Nhân Bản Sheet (Sheet Duplicator)                  │ ← Header (xanh)
│           Chọn sheet và cấu hình nhân bản                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─ Danh Sách Sheet ──────────────────────────────────────┐ │
│  │ [✓] Chọn tất cả                    🔍 Tìm kiếm...     │ │
│  │ ─────────────────────────────────────────────────────  │ │
│  │ [☐] A101 - Mặt bằng tầng 1        (3 views)          │ │
│  │ [☑] A102 - Mặt bằng tầng 2        (2 views, 1 legend)│ │
│  │ [☑] A201 - Mặt cắt dọc            (1 view)           │ │
│  │ [☐] A301 - Chi tiết cầu thang      (4 views, 1 sched) │ │
│  │ ...                                                   │ │
│  └───────────────────────────────────────────────────────┘ │
│    Đã chọn: 2/4 sheet                                      │
│                                                             │
│  ┌─ Tùy Chọn Nhân Bản ────────────────────────────────────┐ │
│  │  Prefix tên sheet:  [Copy of ____________]             │ │
│  │  Suffix số hiệu:   [_COPY______________]              │ │
│  │  Chế độ duplicate view:                               │ │
│  │    ○ Độc lập (Independent)                            │ │
│  │    ● Kèm chi tiết (With Detailing)  ← mặc định       │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
│  ℹ  Legend sẽ được dùng lại (không nhân bản).              │
│     Schedule sẽ được tạo instance mới.                     │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                          [Hủy]     [Nhân Bản ▶]            │ ← Footer
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Logic Nhân Bản (SheetDuplicatorService)

### 4.1 Quy trình tổng thể

```
foreach sheet được chọn:
  1. Lấy Title Block của sheet gốc (OST_TitleBlocks)
  2. Tạo ViewSheet mới  →  ViewSheet.Create(doc, titleBlockId)
  3. Đặt Sheet Number & Name mới  (thêm suffix/prefix)
  4. Lấy tất cả viewports trên sheet gốc  →  sheet.GetAllViewports()
  5. foreach viewport:
       a. Lấy View từ viewport  (viewport.ViewId)
       b. Phân loại view:
          - Legend?     → dùng nguyên viewId, tạo Viewport mới
          - Schedule?   → dùng ScheduleSheetInstance.Create()
          - View thường → Duplicate view, lấy newViewId
       c. Đặt viewport lên sheet mới tại cùng vị trí  →  Viewport.Create()
       d. Restore scale, crop region, annotation crop
  6. Ghi log kết quả
```

### 4.2 Xử lý từng loại viewport

#### Legend View
```csharp
// Legend có thể đặt trên nhiều sheet
if (view is ViewDrafting || view.ViewType == ViewType.Legend)
{
    Viewport.Create(doc, newSheet.Id, view.Id, boxCenter);
}
```

#### Schedule (Bảng biểu)
```csharp
// ScheduleSheetInstance thay vì Viewport
if (doc.GetElement(viewport.ViewId) is ViewSchedule schedule)
{
    // Lấy vị trí của ScheduleSheetInstance gốc
    var origin = GetScheduleOrigin(doc, sheet, schedule);
    ScheduleSheetInstance.Create(doc, newSheet.Id, schedule.Id, origin);
}
```

#### View thông thường (FloorPlan, Section, Elevation, Detail, Drafting...)
```csharp
var duplicateOption = settings.WithDetailing
    ? ViewDuplicateOption.WithDetailing
    : ViewDuplicateOption.Independent;

if (view.CanViewBeDuplicated(duplicateOption))
{
    var newViewId = view.Duplicate(duplicateOption);
    var newView = doc.GetElement(newViewId) as View;
    // Đặt tên view mới (tùy chọn)
    Viewport.Create(doc, newSheet.Id, newViewId, boxCenter);
}
else
{
    // Log cảnh báo: view không thể duplicate
    result.Warnings.Add($"'{view.Name}' không thể nhân bản, bỏ qua.");
}
```

### 4.3 Xử lý vị trí viewport

```csharp
// Lấy vị trí center của viewport gốc trên sheet
XYZ boxCenter = viewport.GetBoxCenter();
// Đặt viewport mới tại cùng vị trí
Viewport.Create(doc, newSheet.Id, newViewId, boxCenter);
```

### 4.4 Đặt tên sheet mới

```
Sheet Number mới  =  settings.SheetNumberSuffix + gốc.SheetNumber
Sheet Name mới    =  settings.SheetNamePrefix + gốc.Name

Ví dụ:
  gốc:   A101 / "Mặt bằng tầng 1"
  mới:   A101_COPY / "Copy of Mặt bằng tầng 1"
```

Nếu sheet number đã tồn tại → tự động thêm `_1`, `_2`...

---

## 5. Danh Sách File Cần Tạo / Sửa

### Tạo mới
| File | Mô tả |
|------|-------|
| `Models/SheetInfo.cs` | DTO: SheetId, Number, Name, ViewportCount, IsSelected |
| `Models/SheetDuplicatorSettings.cs` | Prefix, Suffix, DuplicateOption |
| `Models/DuplicateResult.cs` | SheetId, NewSheetId, Success, Warnings, Error |
| `ViewModels/SheetDuplicatorViewModel.cs` | ObservableCollection, SelectAll, Filter |
| `Views/SheetDuplicatorWindow.xaml` | WPF Dialog |
| `Views/SheetDuplicatorWindow.xaml.cs` | Code-behind |
| `Commands/SheetDuplicatorCommand.cs` | ExternalCommand entry point |
| `Services/SheetDuplicatorService.cs` | Logic nhân bản |

### Sửa đổi
| File | Thay đổi |
|------|---------|
| `Application.cs` | Thêm ribbon button "Nhân Bản\nSheet" → SheetDuplicatorCommand |

---

## 6. Xử Lý Edge Cases

| Tình huống | Cách xử lý |
|-----------|------------|
| View không thể duplicate | Bỏ qua, ghi cảnh báo vào DuplicateResult |
| Sheet number đã tồn tại | Tự động tăng suffix: `_COPY`, `_COPY_1`, `_COPY_2`... |
| Sheet không có title block | Tạo sheet trống không có title block |
| View đặt sai vị trí do Sheet boundary | Dùng `GetBoxCenter()` chuẩn từ viewport gốc |
| Schedule không phải Viewport | Phát hiện qua `ScheduleSheetInstance` thay vì `Viewport` |
| Không chọn sheet nào | Validate: hiện thông báo lỗi, không thực thi |
| Project không có sheet | Hiện thông báo "Không tìm thấy sheet nào trong project" |

---

## 7. Thứ Tự Triển Khai

```
Bước 1: Models (SheetInfo, SheetDuplicatorSettings, DuplicateResult)
Bước 2: SheetDuplicatorViewModel  (INotifyPropertyChanged, collections, commands)
Bước 3: SheetDuplicatorWindow.xaml  (UI layout)
Bước 4: SheetDuplicatorWindow.xaml.cs  (validate + lấy settings)
Bước 5: SheetDuplicatorService  (core duplication logic)
Bước 6: SheetDuplicatorCommand  (orchestration)
Bước 7: Application.cs  (thêm ribbon button)
```

---

## 8. Revit API References

| API | Mục đích |
|-----|---------|
| `FilteredElementCollector(doc).OfClass(typeof(ViewSheet))` | Lấy tất cả sheets |
| `sheet.GetAllViewports()` | Lấy IDs của tất cả viewport trên sheet |
| `doc.GetElement(id) as Viewport` | Lấy Viewport object |
| `viewport.ViewId` | ID của view đặt trong viewport |
| `viewport.GetBoxCenter()` | Vị trí center của viewport trên sheet |
| `view.CanViewBeDuplicated(option)` | Kiểm tra view có thể duplicate |
| `view.Duplicate(option)` | Duplicate view, trả về ElementId mới |
| `ViewSheet.Create(doc, titleBlockFamilySymbolId)` | Tạo sheet mới |
| `Viewport.Create(doc, sheetId, viewId, center)` | Đặt view lên sheet |
| `ScheduleSheetInstance.Create(doc, sheetId, scheduleId, origin)` | Đặt schedule lên sheet |
| `new FilteredElementCollector(doc, sheet.Id).OfCategory(OST_TitleBlocks)` | Lấy title block của sheet |
| `sheet.SheetNumber` / `sheet.Name` | Đọc/ghi số hiệu và tên sheet |

---

## 9. Kết Quả Mong Đợi

Sau khi hoàn thành:
- Ribbon có button "Nhân Bản Sheet" bên cạnh "Đim Định Vị Cột"
- Dialog hiện danh sách toàn bộ sheet với checkbox, filter tìm kiếm
- Người dùng chọn sheet, cấu hình prefix/suffix, nhấn "Nhân Bản"
- Add-in tạo sheet mới kèm tất cả views (duplicate), legends (reuse), schedules (new instance)
- Dialog kết quả hiện số sheet thành công / cảnh báo / lỗi
