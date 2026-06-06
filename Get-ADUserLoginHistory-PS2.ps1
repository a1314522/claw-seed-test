﻿#Requires -Version 2.0
<#
.SYNOPSIS
    查询AD域中指定用户最近登录的机器列表 (PowerShell 2.0兼容版)

.DESCRIPTION
    从域控安全事件日志中提取指定用户的登录记录（事件ID 4624/4768/4769），
    汇总其最近登录过的机器名和IP地址。
    兼容 PowerShell 2.0+，无需升级即可运行。

.PARAMETER Username
    要查询的AD用户名（sAMAccountName，不含域名）。不指定则弹窗输入。
    支持自动剥离 NetBIOS域名前缀，如 rorze\fengchenyang → fengchenyang

.PARAMETER Days
    查询最近多少天的记录，默认7天

.PARAMETER DomainController
    指定要查询的域控FQDN。不指定则自动查找当前域的PDC Emulator

.PARAMETER ExportPath
    导出CSV的路径，默认导出到临时目录

.EXAMPLE
    .\Get-ADUserLoginHistory-PS2.ps1 -Username "fengchenyang" -Days 3

.EXAMPLE
    .\Get-ADUserLoginHistory-PS2.ps1 -Username "rorze\fengchenyang" -Days 14 -DomainController "dc01.contoso.com"
#>

param(
    [string]$Username,
    [int]$Days = 7,
    [string]$DomainController,
    [string]$ExportPath
)

# ========== 用户名清洗 ==========
# 自动剥离 NetBIOS域名\用户名 格式中的域名前缀
if ($Username -and $Username.Contains('\')) {
    $Username = ($Username -split '\\')[1]
    Write-Warning "检测到域名前缀，已自动提取用户名: $Username"
}

# ========== GUI 输入弹窗 ==========
function Show-InputDialog {
    param([string]$Title = "输入", [string]$Message = "请输入：")

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $form = New-Object System.Windows.Forms.Form
    $form.Text = $Title
    $form.Size = New-Object System.Drawing.Size(420, 180)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = "FixedDialog"
    $form.MaximizeBox = $false

    $label = New-Object System.Windows.Forms.Label
    $label.Location = New-Object System.Drawing.Point(10, 15)
    $label.Size = New-Object System.Drawing.Size(380, 20)
    $label.Text = $Message
    $form.Controls.Add($label)

    $textBox = New-Object System.Windows.Forms.TextBox
    $textBox.Location = New-Object System.Drawing.Point(10, 40)
    $textBox.Size = New-Object System.Drawing.Size(380, 20)
    $form.Controls.Add($textBox)

    $btnOK = New-Object System.Windows.Forms.Button
    $btnOK.Location = New-Object System.Drawing.Point(210, 80)
    $btnOK.Size = New-Object System.Drawing.Size(80, 25)
    $btnOK.Text = "确定"
    $btnOK.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $form.Controls.Add($btnOK)

    $btnCancel = New-Object System.Windows.Forms.Button
    $btnCancel.Location = New-Object System.Drawing.Point(310, 80)
    $btnCancel.Size = New-Object System.Drawing.Size(80, 25)
    $btnCancel.Text = "取消"
    $btnCancel.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $form.Controls.Add($btnCancel)

    $form.AcceptButton = $btnOK
    $form.CancelButton = $btnCancel

    $result = $form.ShowDialog()
    if ($result -eq [System.Windows.Forms.DialogResult]::OK) {
        $rawInput = $textBox.Text.Trim()
        # 弹窗输入也做域名前缀清洗
        if ($rawInput -and $rawInput.Contains('\')) {
            $cleaned = ($rawInput -split '\\')[1]
            Write-Warning "检测到域名前缀，已自动提取用户名: $cleaned"
            return $cleaned
        }
        return $rawInput
    }
    return $null
}

if (-not $Username) {
    $Username = Show-InputDialog -Title "AD 用户登录查询" -Message "请输入要查询的 AD 用户名（例如 fengchenyang）："
    if (-not $Username) {
        Write-Warning "未输入用户名，脚本已取消。"
        exit 0
    }
}

# 颜色输出辅助 (PS2.0兼容)
function Write-Info    { param([string]$m) Write-Host $m -ForegroundColor Cyan }
function Write-Success { param([string]$m) Write-Host $m -ForegroundColor Green }
function Write-Warn    { param([string]$m) Write-Host $m -ForegroundColor Yellow }
function Write-Error2  { param([string]$m) Write-Host $m -ForegroundColor Red }

# ========== 前置检查 ==========
Write-Info "=== AD 用户登录历史查询 (PS2.0兼容版) ==="
Write-Info "查询用户 : $Username"
Write-Info "时间范围 : 最近 $Days 天"

# 1. 确认 ActiveDirectory 模块可用
try {
    Import-Module ActiveDirectory -ErrorAction Stop
} catch {
    Write-Warn "未检测到 ActiveDirectory PowerShell 模块，正在尝试从 RSAT 加载..."
    try {
        $ADModulePath = Join-Path $env:SystemRoot "System32\WindowsPowerShell\v1.0\Modules\ActiveDirectory\ActiveDirectory.psd1"
        if (Test-Path $ADModulePath) {
            Import-Module $ADModulePath -ErrorAction Stop
        } else {
            throw "模块文件不存在"
        }
    } catch {
        Write-Error2 "无法加载 ActiveDirectory 模块。请在域成员机或域控上运行，并安装 RSAT 工具。"
        Write-Error2 "Windows 7/Server 2008 R2: 安装 Windows Management Framework 3.0+ 和 RSAT"
        exit 1
    }
}

# 2. 自动定位域控
if (-not $DomainController) {
    try {
        $Domain = [System.DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain()
        $DomainController = $Domain.PdcRoleOwner.Name
        Write-Info "自动使用 PDC Emulator: $DomainController"
    } catch {
        Write-Error2 "无法获取域控信息。请手动指定 -DomainController 参数，例如: -DomainController 'dc01.contoso.com'"
        exit 1
    }
} else {
    Write-Info "指定域控 : $DomainController"
}

# 3. 验证目标用户存在 - 使用Filter更灵活，支持多种匹配
try {
    $ADUser = Get-ADUser -Filter {sAMAccountName -eq $Username} -Properties DisplayName, sAMAccountName, Name -ErrorAction Stop
    if (-not $ADUser) {
        # 尝试用 Name/CN 再查一次
        $ADUser = Get-ADUser -Filter {Name -eq $Username} -Properties DisplayName, sAMAccountName, Name -ErrorAction Stop
    }
    if ($ADUser) {
        # 更新为正确的sAMAccountName
        $CorrectUsername = $ADUser.sAMAccountName
        if ($CorrectUsername -ne $Username) {
            Write-Warn "用户名已标准化: $Username -> $CorrectUsername"
            $Username = $CorrectUsername
        }
        Write-Success "用户存在: $($ADUser.Name) (sAMAccountName: $Username)"
    } else {
        throw "用户未找到"
    }
} catch {
    Write-Error2 "AD中未找到用户 '$Username'，请检查用户名是否正确"
    Write-Warn "提示: 应使用 sAMAccountName（登录名），而非显示名称或邮件地址"
    Write-Warn "      例如用 'fengchenyang'，而非 'Feng ChenYang' 或 'rorze\fengchenyang'"
    exit 1
}

# 4. 检查事件日志访问权限
try {
    $null = Get-WinEvent -ComputerName $DomainController -LogName Security -MaxEvents 1 -ErrorAction Stop
} catch {
    Write-Error2 "无法读取 $DomainController 的安全事件日志。"
    Write-Warn "可能原因:"
    Write-Warn "  1. 当前账号没有读取权限（需要域管理员或委派了Manage Auditing and Security Log权限）"
    Write-Warn "  2. 域控防火墙阻止了远程事件日志访问"
    Write-Warn "  3. 请在域控本机以管理员身份运行此脚本"
    exit 1
}

# 5. 检查日志保留情况（预估）
try {
    $LogInfo = Get-WinEvent -ComputerName $DomainController -ListLog Security -ErrorAction Stop
    $LogSizeMB = [math]::Round($LogInfo.FileSize / 1MB, 2)
    $LogMaxMB = [math]::Round($LogInfo.MaximumSizeInBytes / 1MB, 2)
    Write-Info "安全日志大小: $LogSizeMB MB / $LogMaxMB MB (记录数: $($LogInfo.RecordCount))"
    if ($LogInfo.RecordCount -eq 0) {
        Write-Warn "安全日志为空！请检查审计策略是否已启用。"
    }
} catch {
    Write-Warn "无法获取日志状态信息: $_"
}

# ========== 构造查询 ==========
$StartTime = (Get-Date).AddDays(-$Days).ToUniversalTime()
$StartTimeStr = $StartTime.ToString("o")

Write-Info "正在从 $DomainController 拉取安全日志，请稍候..."
Write-Warn "注意: 如果日志量很大，可能需要几分钟时间"

# ========== 拉取事件 (PS2.0兼容XPath构建) ==========
try {
    # 构建XPath查询字符串
    $EventIds = "EventID=4624 or EventID=4768 or EventID=4769 or EventID=4776"
    $XPathQuery = "*[System[($EventIds) and TimeCreated[@SystemTime>='$StartTimeStr']]] and *[EventData[Data[@Name='TargetUserName'] and contains(Data, '$Username')]]"
    
    $Events = Get-WinEvent -ComputerName $DomainController -FilterXPath $XPathQuery -ErrorAction SilentlyContinue
} catch {
    Write-Error2 "查询事件日志时出错: $_"
    exit 1
}

if (-not $Events) {
    Write-Warn "未找到用户 '$Username' 在最近 $Days 天内的任何登录/认证记录。"
    Write-Warn ""
    Write-Warn "常见原因及解决方案:"
    Write-Warn "  1. 【审计策略未开启】"
    Write-Warn "     → 在域控上打开 gpedit.msc 或 GPO:"
    Write-Warn "     计算机配置 → Windows 设置 → 安全设置 → 本地策略 → 审核策略"
    Write-Warn "     确保'审核登录事件'和'审核账户登录事件'均为'成功'或'成功和失败'"
    Write-Warn ""
    Write-Warn "  2. 【日志已被覆盖】"
    Write-Warn "     → 域控安全日志默认保留周期可能只有几天"
    Write-Warn "     → 建议增加日志大小限制，或部署 SIEM (如 Splunk/ELK)"
    Write-Warn ""
    Write-Warn "  3. 【多台域控环境】"
    Write-Warn "     → Kerberos认证日志分散在各台域控上"
    Write-Warn "     → 建议对每台DC分别运行此脚本，或在本机安全日志查询"
    Write-Warn ""
    Write-Warn "  4. 【用户名不匹配】"
    Write-Warn "     → 当前查询使用的是 sAMAccountName: '$Username'"
    Write-Warn "     → 请确认登录时使用的用户名与此一致"
    Write-Warn ""
    Write-Warn "  5. 【登录类型问题】"
    Write-Warn "     → 某些登录（如已缓存的凭据、某些服务账户）可能不产生4624事件"
    exit 0
}

Write-Success "共找到 $($Events.Count) 条相关记录，正在解析..."

# ========== 解析事件 ==========
$Results = New-Object System.Collections.ArrayList
$SeenWorkstations = New-Object System.Collections.Generic.HashSet[string]

foreach ($Event in $Events) {
    try {
        $Xml = [xml]$Event.ToXml()
        $EventDataNodes = $Xml.Event.EventData.Data
        $TimeCreated = $Event.TimeCreated
        $EventId = $Event.Id

        # 辅助函数: 按Name获取Data值
        function Get-EventDataValue {
            param($dataArray, [string]$name)
            foreach ($node in $dataArray) {
                if ($node.Name -eq $name) {
                    return $node.'#text'
                }
            }
            return $null
        }

        # 根据事件ID提取不同字段
        $Workstation = $null
        $IpAddress   = $null
        $LogonType   = $null
        $AuthPackage = $null

        switch ($EventId) {
            4624 {  # 成功登录
                $Workstation = Get-EventDataValue $EventDataNodes 'WorkstationName'
                $IpAddress   = Get-EventDataValue $EventDataNodes 'IpAddress'
                $LogonType   = Get-EventDataValue $EventDataNodes 'LogonType'
                $AuthPackage = Get-EventDataValue $EventDataNodes 'AuthenticationPackageName'
            }
            4768 {  # Kerberos TGT
                $Workstation = Get-EventDataValue $EventDataNodes 'WorkstationName'
                $IpAddress   = Get-EventDataValue $EventDataNodes 'IpAddress'
                $AuthPackage = 'Kerberos'
            }
            4769 {  # Kerberos TGS
                $Workstation = Get-EventDataValue $EventDataNodes 'WorkstationName'
                $IpAddress   = Get-EventDataValue $EventDataNodes 'IpAddress'
                $AuthPackage = 'Kerberos'
            }
            4776 {  # NTLM认证
                $Workstation = Get-EventDataValue $EventDataNodes 'Workstation'
                $IpAddress   = Get-EventDataValue $EventDataNodes 'IpAddress'
                $AuthPackage = 'NTLM'
            }
        }

        # 清洗无效值
        $invalidIps = @('-', '::1', '127.0.0.1', $null)
        if ($IpAddress -in $invalidIps) { $IpAddress = $null }
        if ($Workstation -in @('-', $null)) { $Workstation = $null }

        # 登录类型映射
        $LogonTypeDesc = switch ($LogonType) {
            2  { "Interactive (本地交互登录)" }
            3  { "Network (网络共享/SMB)" }
            4  { "Batch (计划任务)" }
            5  { "Service (服务启动)" }
            7  { "Unlock (解锁工作站)" }
            8  { "NetworkCleartext (IIS Basic Auth)" }
            9  { "NewCredentials (Runas)" }
            10 { "RemoteInteractive (RDP)" }
            11 { "CachedInteractive (缓存登录)" }
            default { if ($LogonType) { "Type $LogonType" } else { "N/A" } }
        }

        # PS2.0兼容: 使用 New-Object PSObject 代替 [PSCustomObject]
        $record = New-Object PSObject
        Add-Member -InputObject $record -MemberType NoteProperty -Name "Time" -Value $TimeCreated
        Add-Member -InputObject $record -MemberType NoteProperty -Name "EventID" -Value $EventId
        Add-Member -InputObject $record -MemberType NoteProperty -Name "User" -Value $Username
        Add-Member -InputObject $record -MemberType NoteProperty -Name "Workstation" -Value $Workstation
        Add-Member -InputObject $record -MemberType NoteProperty -Name "IPAddress" -Value $IpAddress
        Add-Member -InputObject $record -MemberType NoteProperty -Name "LogonType" -Value $LogonTypeDesc
        Add-Member -InputObject $record -MemberType NoteProperty -Name "AuthPackage" -Value $AuthPackage

        [void]$Results.Add($record)

        if ($Workstation) {
            [void]$SeenWorkstations.Add($Workstation.ToUpper())
        }
    } catch {
        Write-Warn "解析单条事件时出错: $_"
    }
}

# ========== 输出结果 ==========
Write-Host "`n"
Write-Success "===== 详细登录记录（按时间倒序） ====="
$Results | Sort-Object Time -Descending | Format-Table Time, EventID, Workstation, IPAddress, LogonType, AuthPackage -AutoSize

Write-Host "`n"
Write-Success "===== 去重后的登录机器列表 ====="
if ($SeenWorkstations.Count -gt 0) {
    $SortedWorkstations = $SeenWorkstations | Sort-Object
    foreach ($ws in $SortedWorkstations) {
        Write-Host "  $ws" -ForegroundColor White
    }
} else {
    Write-Warn "  未能提取到有效的工作站名称（可能日志中未记录WorkstationName字段）"
}

# 统计
Write-Host "`n"
Write-Info "统计:"
Write-Info "  总记录数    : $($Results.Count)"
Write-Info "  唯一机器数  : $($SeenWorkstations.Count)"

# ========== 导出CSV ==========
if (-not $ExportPath) {
    $ExportPath = "$env:TEMP\ADUserLoginHistory_$Username`_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
}

try {
    $Results | Export-Csv -Path $ExportPath -NoTypeInformation -Encoding UTF8 -Force
    Write-Success "`n详细结果已导出到: $ExportPath"
} catch {
    Write-Warn "CSV导出失败: $_"
}

# ========== 补充建议 ==========
Write-Host "`n"
Write-Warn "===== 使用提示 ====="
Write-Warn "1. 此脚本依赖域控安全日志，如日志已被循环覆盖则查不到早期记录"
Write-Warn "2. 如需长期保留，建议启用 SIEM (如Splunk/ELK/Defender for Identity)"
Write-Warn "3. 事件ID 4768/4769 记录的是Kerberos认证，来源Workstation可能为空"
Write-Warn "4. 最准确的'登录机器'信息通常在本机安全日志（非域控）的事件4624中"
Write-Warn "5. 如需查询所有域控（多域控环境），请对每台DC分别运行此脚本"
Write-Warn "6. PowerShell版本: $($PSVersionTable.PSVersion)"

# 保持窗口打开，等待用户按键退出
Write-Host "`n脚本执行完毕。按 Enter 键关闭窗口..." -ForegroundColor Cyan
Read-Host
