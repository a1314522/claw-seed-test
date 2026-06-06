#requires -RunAsAdministrator
# ==========================================
# 打印机统一安装平台 - 合并版主脚本
# 支持：管理员选择打印机 → 自动提权 → 一键安装
# ==========================================

# --- 打印机配置数据库 ---
$PrinterDB = @(
    # 康桥
    [PSCustomObject]@{
        ID        = 1
        Location  = "康桥"
        Floor     = "1F"
        Name      = "财务办公室"
        IP        = "192.168.10.146"
        Driver    = "HP LaserJet Pro MFP 4101 4102 4103 4104 PCL 6 (V3)"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\财务办公室打印机驱动\hplo0374_x64.inf"
        Brand     = "HP"
        LockBW    = $false   # HP 不需要强制黑白单面
    },
    [PSCustomObject]@{
        ID        = 2
        Location  = "康桥"
        Floor     = "1F"
        Name      = "销售办公室"
        IP        = "192.168.10.210"
        Driver    = "SHARP MX-C4082R PCL6"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\租用打印机驱动\Chinese1\Chinese1\PCL6\64bit\su2emchs.inf"
        Brand     = "SHARP"
        LockBW    = $true
    },
    [PSCustomObject]@{
        ID        = 3
        Location  = "康桥"
        Floor     = "2F"
        Name      = "SV办公室"
        IP        = "172.168.150.27"
        Driver    = "HP Color LaserJet Pro MFP M377 PCL 6"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\sv办公室打印机驱动\hpne862A_x64.inf"
        Brand     = "HP"
        LockBW    = $false
    },
    [PSCustomObject]@{
        ID        = 4
        Location  = "康桥"
        Floor     = "2F"
        Name      = "人事办公室"
        IP        = "172.168.150.46"
        Driver    = "SHARP MX-C4082R PCL6"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\租用打印机驱动\Chinese1\Chinese1\PCL6\64bit\su2emchs.inf"
        Brand     = "SHARP"
        LockBW    = $true
    },
    [PSCustomObject]@{
        ID        = 5
        Location  = "康桥"
        Floor     = "2F"
        Name      = "研发办公室"
        IP        = "172.168.150.22"
        Driver    = "SHARP MX-C2622R PCL6"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\01.二楼研发打印机驱动\Chinese1\PCL6\64bit\su2emchs.inf"
        Brand     = "SHARP"
        LockBW    = $true
    },
    # 临港
    [PSCustomObject]@{
        ID        = 6
        Location  = "临港"
        Floor     = "1F"
        Name      = "缓冲间"
        IP        = "192.168.112.102"
        Driver    = "SHARP MX-C4082R PCL6"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\租用打印机驱动\Chinese1\Chinese1\PCL6\64bit\su2emchs.inf"
        Brand     = "SHARP"
        LockBW    = $true
    },
    [PSCustomObject]@{
        ID        = 7
        Location  = "临港"
        Floor     = "2F"
        Name      = "仓库"
        IP        = "192.168.120.90"
        Driver    = "SHARP MX-C4082R PCL6"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\租用打印机驱动\Chinese1\Chinese1\PCL6\64bit\su2emchs.inf"
        Brand     = "SHARP"
        LockBW    = $true
    },
    [PSCustomObject]@{
        ID        = 8
        Location  = "临港"
        Floor     = "2F"
        Name      = "采购办公室"
        IP        = "192.168.112.248"
        Driver    = "SHARP MX-C4082R PCL6"
        INFPath   = "\\192.168.10.247\Share\Share Disk\软件安装包\打印机\租用打印机驱动\Chinese1\Chinese1\PCL6\64bit\su2emchs.inf"
        Brand     = "SHARP"
        LockBW    = $true
    }
)

# --- 核心安装函数 ---
function Install-Printer {
    param([PSCustomObject]$P)

    $portName    = "IP_$($P.IP)"
    $printerName = "$($P.Location)-$($P.Floor)$($P.Name)打印机"

    Write-Host "`n>>> 正在启动安装流程: $printerName" -ForegroundColor Cyan

    # 1. 创建 TCP/IP 端口
    if (-not (Get-PrinterPort -Name $portName -ErrorAction SilentlyContinue)) {
        Write-Host "  [1/4] 创建端口 $portName..." -ForegroundColor Gray
        Add-PrinterPort -Name $portName -PrinterHostAddress $P.IP
    } else {
        Write-Host "  [1/4] 端口已存在，跳过。" -ForegroundColor DarkGray
    }

    # 2. 注入驱动
    if (Test-Path $P.INFPath) {
        Write-Host "  [2/4] 注入驱动 $($P.Driver)..." -ForegroundColor Gray
        pnputil.exe /add-driver $P.INFPath /install | Out-Null
    } else {
        Write-Host "  [2/4] 错误：驱动文件不存在: $($P.INFPath)" -ForegroundColor Red
        return $false
    }

    # 3. 注册驱动
    if (-not (Get-PrinterDriver -Name $P.Driver -ErrorAction SilentlyContinue)) {
        Write-Host "  [3/4] 注册驱动名称..." -ForegroundColor Gray
        Add-PrinterDriver -Name $P.Driver
    } else {
        Write-Host "  [3/4] 驱动已注册，跳过。" -ForegroundColor DarkGray
    }

    # 4. 创建打印机
    if (-not (Get-Printer -Name $printerName -ErrorAction SilentlyContinue)) {
        Write-Host "  [4/4] 创建打印机对象..." -ForegroundColor Gray
        try {
            Add-Printer -Name $printerName -DriverName $P.Driver -PortName $portName -ErrorAction Stop
            Write-Host "  打印机创建成功！" -ForegroundColor Green
        } catch {
            Write-Host "  创建失败: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "  [4/4] 打印机已存在，跳过。" -ForegroundColor Yellow
    }

    # 5. 夏普专用：强制黑白+单面
    if ($P.LockBW) {
        Write-Host "  [5/5] 强制应用：黑白 + 单面..." -ForegroundColor Cyan
        try {
            Set-PrintConfiguration -PrinterName $printerName -Color $false -DuplexingMode OneSided -ErrorAction Stop
            Write-Host "    → 系统指令推送成功" -ForegroundColor Green
        } catch {
            Write-Host "    → 系统指令失败，尝试对象注入..." -ForegroundColor Yellow
            $config = Get-PrintConfiguration -PrinterName $printerName
            $config.Color = $false
            $config.DuplexingMode = "OneSided"
            Set-PrintConfiguration -InputObject $config
        }

        # 注册表加固
        $regPath = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers\$printerName\PrinterDriverData"
        if (Test-Path $regPath) {
            Set-ItemProperty -Path $regPath -Name "Duplex"     -Value 1 -ErrorAction SilentlyContinue
            Set-ItemProperty -Path $regPath -Name "ColorMode"   -Value 1 -ErrorAction SilentlyContinue
            Write-Host "    → 注册表加固完成" -ForegroundColor Green
        }

        # 验证
        $fc = Get-PrintConfiguration -PrinterName $printerName
        Write-Host "    → 颜色=$($fc.Color), 双面=$($fc.DuplexingMode)" -ForegroundColor $(if($fc.DuplexingMode -eq "OneSided"){"Green"}else{"Red"})
    }

    Write-Host "  ────────────────────────" -ForegroundColor DarkGray
    return $true
}

# --- 交互式选择菜单 ---
function Show-Menu {
    Clear-Host
    Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║       企业打印机自助安装平台 (管理员模式)               ║" -ForegroundColor Cyan
    Write-Host "╠════════════════════════════════════════════════════════╣" -ForegroundColor Cyan

    # 按地点分组展示
    $locations = $PrinterDB | Group-Object -Property Location | Sort-Object Name
    foreach ($loc in $locations) {
        Write-Host "║  【$($loc.Name)】" -ForegroundColor Yellow
        foreach ($p in ($loc.Group | Sort-Object Floor, ID)) {
            Write-Host "║     [$($p.ID)]  $($loc.Name)-$($p.Floor)$($p.Name)  ($($p.Brand) | $($p.IP))" -ForegroundColor White
        }
    }

    Write-Host "╠════════════════════════════════════════════════════════╣" -ForegroundColor Cyan
    Write-Host "║  [A] 一键安装全部打印机                               ║" -ForegroundColor Magenta
    Write-Host "║  [Q] 退出                                             ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

# --- 主程序 ---
Clear-Host
Write-Host "正在检查管理员权限..." -ForegroundColor Gray
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "错误：需要以管理员权限运行本脚本！" -ForegroundColor Red
    Write-Host "右键 → 使用 PowerShell (管理员) 重新运行。" -ForegroundColor Yellow
    pause
    exit 1
}

# 循环交互
while ($true) {
    Show-Menu
    $choice = Read-Host "请输入编号选择打印机，或输入 A(全部)/Q(退出)"

    switch ($choice.Trim().ToUpper()) {
        'Q' { Write-Host "退出安装程序。"; exit 0 }
        'A' {
            Write-Host "`n>>> 开始批量安装全部 $($PrinterDB.Count) 台打印机..." -ForegroundColor Magenta
            $success = 0; $fail = 0
            foreach ($p in $PrinterDB) {
                if (Install-Printer -P $p) { $success++ } else { $fail++ }
            }
            Write-Host "`n安装完成: 成功 $success 台, 失败 $fail 台" -ForegroundColor $(if($fail -eq 0){"Green"}else{"Yellow"})
            pause
        }
        default {
            $sel = $PrinterDB | Where-Object { $_.ID -eq [int]$choice }
            if ($sel) {
                Install-Printer -P $sel
                pause
            } else {
                Write-Host "无效选择，请重新输入。" -ForegroundColor Red
                Start-Sleep -Seconds 1
            }
        }
    }
}
