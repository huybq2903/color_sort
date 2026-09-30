# Module Manager @Importer

## Tổng quan
- **Tên:** `Importer`
- **Giới thiệu:** Cung cấp một giao diện Editor cho Unity để quản lý và cài đặt các module (package) từ một registry từ xa. Công cụ này cho phép người dùng xem danh sách các module có sẵn, kiểm tra phiên bản, xem changelog và cài đặt chúng vào dự án một cách dễ dàng.
- **Tính năng chính:**
    - **Hiển thị Module từ xa:** Tải và hiển thị danh sách các module có sẵn từ một file registry trung tâm trên CDN.
    - **Quản lý phiên bản:** So sánh phiên bản của các module đã cài đặt với phiên bản mới nhất trên registry.
    - **Cài đặt Module:** Tự động tải file `.unitypackage` của module từ CDN và import vào project.
    - **Xem Changelog:** Hiển thị cửa sổ changelog phiên bản của module.
    - **Giao diện:** Cung cấp cửa sổ Editor trực quan để thực hiện các thao tác trên.

## Quick Start

1.  Mở cửa sổ Importer bằng cách vào menu `Falcon > Manager > Importer`.
2.  Cửa sổ sẽ hiển thị danh sách các module từ registry.
3.  Để cài đặt hoặc cập nhật một module, nhấn nút **Install** hoặc **Update** bên cạnh tên module đó.
4.  Xóa module, nhấn nút **Delete**.
4.  Xem lịch sử thay đổi của một module, nhấn vào xem ChangeLog.

## Danh sách đầy đủ API
Module này chủ yếu được sử dụng qua giao diện Editor. Điểm truy cập chính là `MenuItem`:
- `Falcon > Manager > Importer`: Mở cửa sổ quản lý và cài đặt các module.

## Chi tiết

### Luồng hoạt động
1.  **Tải Registry:** Khi cửa sổ được mở, `RemotePackageRepository` sẽ tải file registry từ CDN.
2.  **Hiển thị Module:** `ModulesShowingController` xử lý logic để hiển thị danh sách các module, so sánh phiên bản và trạng thái cài đặt.
3.  **Cài đặt:** Khi người dùng nhấn "Install", `PackageManageService` và `UnityPackageManageService` sẽ phối hợp để:
    - Check depedencies của module
    - Tải file `.unitypackage` về máy.
    - Tự động import package vào Unity.
4.  **Hỏi người dùng:** `ModuleAskingController` được sử dụng để hiển thị các hộp thoại xác nhận trước khi thực hiện các hành động quan trọng.

### Phụ thuộc
- `falcon.helpers.devkit`
- `falcon.manager.shared.editor`