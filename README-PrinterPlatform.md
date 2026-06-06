# 打印机统一安装平台 - 使用指南

## 已交付文件

| 文件 | 说明 |
|------|------|
| `PrinterInstall-Platform.ps1` | 合并版 PowerShell 主脚本（带交互菜单） |
| `PrinterInstall-Portal.hta` | 图形化门户（双击即用，零安装） |
| `README.md` | 本文件 |

---

## 方案对比

| 方案 | 优点 | 缺点 | 推荐场景 |
|------|------|------|----------|
| **A. PowerShell 脚本** | 原生、最完整、日志清晰 | 需要右键管理员运行 | 管理员自己用，或批量部署 |
| **B. HTA 门户** | 双击即用、有图形界面、零安装 | 依赖 IE 内核渲染 | 给管理员一个好看的点击面板 |
| **C. EXE 打包** | 双击运行、看起来像正经软件 | 需要额外工具打包，可能被杀毒误报 | 要给不懂技术的人用 |

---

## 方案 A：PowerShell 脚本（最完整）

### 使用方法

1. 把 `PrinterInstall-Platform.ps1` 放到目标机器
2. **右键 → 使用 PowerShell (管理员)** 运行
3. 按菜单选择打印机编号，回车安装
4. 输入 `A` 一键安装全部

### 特性
- 自动检测管理员权限，非管理员自动拒绝
- 端口/驱动/打印机三重检测，已存在则跳过
- 夏普打印机自动强制黑白+单面
- HP 打印机仅安装不做额外配置

---

## 方案 B：HTA 图形门户（最方便）

### 使用方法

1. 把 `PrinterInstall-Portal.hta` 放到目标机器
2. **双击运行**（会自动尝试提权，或要求管理员密码）
3. 点击卡片选择打印机，再点【安装选中打印机】
4. 或点【一键安装全部】

### 注意事项
- HTA 文件需要以管理员身份运行才能调用 pnputil 和 Add-Printer
- 如果双击没有管理员权限，可右键 → 以管理员身份运行
- 第一次运行如果 Windows 提示"来自 Internet 的文件"，点击【解除锁定】

---

## 方案 C：打包成 EXE

### 方法 1：PS2EXE（推荐，最简单）

```powershell
# 在管理员 PowerShell 中执行
Install-Module -Name ps2exe -Scope CurrentUser

# 打包命令
Invoke-PS2EXE `
    -InputFile "PrinterInstall-Platform.ps1" `
    -OutputFile "打印机安装平台.exe" `
    -NoConsole `
    -Title "企业打印机自助安装平台" `
    -Description "管理员选择打印机后自动安装" `
    -Company "YourCompany" `
    -Copyright "2026" `
    -Version "1.0.0.0" `
    -IconFile "printer.ico"          # 可选：自定义图标
```

打包后的 `打印机安装平台.exe`：
- 双击运行即出现交互式控制台菜单
- 打包了完整的 PowerShell 引擎，**目标机器不需要装 PowerShell 模块**
- 建议**右键 → 以管理员身份运行**，或配置 manifest 自动提权

### 方法 2：嵌入 Manifest 自动请求管理员（进阶）

如果希望双击 EXE 时自动弹出 UAC 管理员请求，打包后需要用 Resource Hacker 或 mt.exe 嵌入管理员 manifest：

```xml
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedPrivileges>
        <requestedExecutionLevel level="requireAdministrator" uiAccess="false"/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

---

## 打印机清单

| 编号 | 地点 | 楼层 | 位置 | IP | 品牌 | 驱动 | 黑白锁定 |
|------|------|------|------|-----|------|------|----------|
| 1 | 康桥 | 1F | 财务办公室 | 192.168.10.146 | HP | LaserJet Pro MFP 4101-4104 PCL6 | ❌ |
| 2 | 康桥 | 1F | 销售办公室 | 192.168.10.210 | SHARP | MX-C4082R PCL6 | ✅ |
| 3 | 康桥 | 2F | SV办公室 | 172.168.150.27 | HP | Color LaserJet Pro MFP M377 PCL6 | ❌ |
| 4 | 康桥 | 2F | 人事办公室 | 172.168.150.46 | SHARP | MX-C4082R PCL6 | ✅ |
| 5 | 康桥 | 2F | 研发办公室 | 172.168.150.22 | SHARP | MX-C2622R PCL6 | ✅ |
| 6 | 临港 | 1F | 缓冲间 | 192.168.112.102 | SHARP | MX-C4082R PCL6 | ✅ |
| 7 | 临港 | 2F | 仓库 | 192.168.120.90 | SHARP | MX-C4082R PCL6 | ✅ |
| 8 | 临港 | 2F | 采购办公室 | 192.168.112.248 | SHARP | MX-C4082R PCL6 | ✅ |

---

## 常见问题

**Q: 域策略限制了 PowerShell 执行怎么办？**  
所有方案都已加入 `-ExecutionPolicy Bypass` 参数，但如果域策略通过 GPO 强制禁止，需要 IT 侧放行，或者用 SCCM 下发签名过的 EXE。

**Q: 驱动文件路径变了怎么办？**  
修改 `PrinterInstall-Platform.ps1` 中 `$PrinterDB` 数组的 `INFPath` 字段，或修改 HTA 中 `printers` 数组的 `inf` 字段。

**Q: 要新增打印机怎么办？**  
在 `$PrinterDB` 数组（PS1）或 `printers` 数组（HTA）中按相同格式追加一条记录即可，编号递增。

**Q: HTA 界面看起来复古？**  
HTA 用的是 IE 内核，样式受限。如果要现代化界面，建议用方案 C 打包成 EXE，或者搭一个本地 HTTP 服务 + 浏览器访问。

---

*生成时间: 2026-05-30*
