﻿#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    查询AD域中指定用户最近登录的机器列表

.DESCRIPTION
    从域控安全事件日志中提取指定用户的登录记录（事件ID 4624/4768/4769），
    汇总其最近登录过的机器名和IP地址。

.PARAMETER Username
    要查询的AD用户名（sAMAccountName，不含域名）

.PARAMETER Days
    查询最近多少天的记录，默认7天

.PARAMETER DomainController
    指定要查询的域控FQDN。不指定则自动查找当前域的PDC Emulator

.PARAMETER ExportPath
    导出CSV的路径，默认导出到临时目录

.EXAMPLE
    .\Get-ADUserLoginHistory.ps1 -Username "zhangsan" -Days 3

.EXAMPLE
    .\Get-ADUserLoginHistory.ps1 -Username "zhangsan" -Days 14 -DomainController "dc01.contoso.com"
#>

[CmdletBinding()]
param(
    [Parameter(HelpMessage = "AD用户名（不含域名），不填则弹窗输入")]
    [string]$Username,

    [Parameter(HelpMessage = "查询最近多少天，默认7")]
    [int]$Days = 7,

    [Parameter(HelpMessage = "指定域控FQDN，不指定则使用PDC Emulator")]
    [string]$DomainController,

    [Parameter(HelpMessage = "CSV导出路径")]
    [string]$ExportPath
)

# ========== GUI 输入弹窗 ==========
if (-not $Username) {
    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $form = New-Object System.Windows.Forms.Form
    $form.Text = 'AD 用户登录查询'
    $form.Size = New-Object System.Drawing.Size(420, 180)
    $form.StartPosition = 'CenterScreen'
    $form.FormBorderStyle = 'FixedDialog'
    $form.MaximizeBox = $false

    $label = New-Object System.Windows.Forms.Label
    $label.Location = New-Object System.Drawing.Point(10, 15)
    $label.Size = New-Object System.Drawing.Size(380, 20)
    $label.Text = '请输入要查询的 AD 用户名（sAMAccountName，例如 zhangsan）：'
    $form.Controls.Add($label)

    $textBox = New-Object System.Windows.Forms.TextBox
    $textBox.Location = New-Object System.Drawing.Point(10, 40)
    $textBox.Size = New-Object System.Drawing.Size(380, 20)
    $form.Controls.Add($textBox)

    $btnOK = New-Object System.Windows.Forms.Button
    $btnOK.Location = New-Object System.Drawing.Point(210, 80)
    $btnOK.Size = New-Object System.Drawing.Size(80, 25)
    $btnOK.Text = '确定'
    $btnOK.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $form.Controls.Add($btnOK)

    $btnCancel = New-Object System.Windows.Forms.Button
    $btnCancel.Location = New-Object System.Drawing.Point(310, 80)
    $btnCancel.Size = New-Object System.Drawing.Size(80, 25)
    $btnCancel.Text = '取消'
    $btnCancel.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $form.Controls.Add($btnCancel)

    $form.AcceptButton = $btnOK
    $form.CancelButton = $btnCancel

    $result = $form.ShowDialog()
    if ($result -eq [System.Windows.Forms.DialogResult]::OK -and -not [string]::IsNullOrWhiteSpace($textBox.Text)) {
        $Username = $textBox.Text.Trim()
    } else {
        Write-Warn "未输入用户名，脚本已取消。"
        exit 0
    }
}

# 颜色输出辅助
function Write-Info    { param([string]$m) Write-Host $m -ForegroundColor Cyan }
function Write-Success { param([string]$m) Write-Host $m -ForegroundColor Green }
function Write-Warn    { param([string]$m) Write-Host $m -ForegroundColor Yellow }
function Write-Error2  { param([string]$m) Write-Host $m -ForegroundColor Red }

# ========== 前置检查 ==========
Write-Info "=== AD 用户登录历史查询 ==="
Write-Info "查询用户 : $Username"
Write-Info "时间范围 : 最近 $Days 天"

# 1. 确认 ActiveDirectory 模块可用
if (-not (Get-Module -ListAvailable -Name "ActiveDirectory")) {
    Write-Warn "未检测到 ActiveDirectory PowerShell 模块，正在尝试加载..."
    try {
        Import-Module ActiveDirectory -ErrorAction Stop
    } catch {
        Write-Error2 "无法加载 ActiveDirectory 模块。请在域成员机或域控上运行，并安装 RSAT 工具。"
        Write-Error2 "下载地址: https://www.microsoft.com/download/details.aspx?id=45520"
        exit 1
    }
}

# 2. 自动定位域控
if (-not $DomainController) {
    try {
        $DomainController = (Get-ADDomain -ErrorAction Stop).PDCEmulator
        Write-Info "自动使用 PDC Emulator: $DomainController"
    } catch {
        Write-Error2 "无法获取域控信息。请手动指定 -DomainController 参数，例如: -DomainController 'dc01.contoso.com'"
        exit 1
    }
} else {
    Write-Info "指定域控 : $DomainController"
}

# 3. 验证目标用户存在
try {
    $ADUser = Get-ADUser -Identity $Username -Properties DisplayName -ErrorAction Stop
    Write-Success "用户存在: $($ADUser.Name) ($Username)"
} catch {
    Write-Error2 "AD中未找到用户 '$Username'，请检查用户名是否正确"
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

# ========== 构造查询 ==========
$StartTime = (Get-Date).AddDays(-$Days).ToUniversalTime()
$StartTimeStr = $StartTime.ToString("o")

# 查询事件ID说明:
#   4624 - 账户已成功登录 (来源: 本地安全日志)
#   4768 - Kerberos 身份验证票证(TGT)已请求 (来源: 域控)
#   4769 - Kerberos 服务票证(TGS)已请求 (来源: 域控)
#   4776 - 域控制器尝试验证账户的凭据 (来源: NTLM认证)
#
# 提示: 域控上最可靠的用户来源机器信息通常在 4624 的 WorkstationName 字段中

$FilterXPath = @"
<QueryList>
  <Query Id="0" Path="Security">
    <Select Path="Security">
      *[System[(EventID=4624 or EventID=4768 or EventID=4769 or EventID=4776)
        and TimeCreated[@SystemTime&gt;='$StartTimeStr']]]
      and
      *[EventData[Data[@Name='TargetUserName'] and contains(Data, '$Username')]]
    </Select>
  </Query>
</QueryList>
"@

Write-Info "正在从 $DomainController 拉取安全日志，请稍候..."
Write-Warn "注意: 如果日志量很大，可能需要几分钟时间"

# ========== 拉取事件 ==========
try {
    $Events = Get-WinEvent -ComputerName $DomainController -FilterXml $FilterXPath -ErrorAction SilentlyContinue
} catch {
    Write-Error2 "查询事件日志时出错: $_"
    exit 1
}

if (-not $Events) {
    Write-Warn "未找到用户 '$Username' 在最近 $Days 天内的任何登录/认证记录。"
    Write-Warn "可能原因:"
    Write-Warn "  1. 审计策略未开启（需要开启'审核登录事件'和'审核账户登录事件'）"
    Write-Warn "  2. 安全日志已被覆盖（域控日志默认保留周期较短）"
    Write-Warn "  3. 该用户在指定时间段内确实没有登录"
    Write-Warn "  4. 用户名格式问题（建议用 sAMAccountName，即登录名）"
    exit 0
}

Write-Success "共找到 $($Events.Count) 条相关记录，正在解析..."

# ========== 解析事件 ==========
$Results = [System.Collections.ArrayList]::new()
$SeenWorkstations = [System.Collections.Generic.HashSet[string]]::new()

foreach ($Event in $Events) {
    try {
        $Xml = [xml]$Event.ToXml()
        $EventData = $Xml.Event.EventData.Data
        $TimeCreated = $Event.TimeCreated.ToLocalTime()
        $EventId = $Event.Id

        # 通用字段提取辅助函数
        function Get-EventDataValue($dataArray, $name) {
            $node = $dataArray | Where-Object { $_.Name -eq $name }
            if ($node) { return $node.'#text' }
            return $null
        }

        # 根据事件ID提取不同字段
        $Workstation = $null
        $IpAddress   = $null
        $LogonType   = $null
        $AuthPackage = $null

        switch ($EventId) {
            4624 {  # 成功登录
                $Workstation = Get-EventDataValue $EventData 'WorkstationName'
                $IpAddress   = Get-EventDataValue $EventData 'IpAddress'
                $LogonType   = Get-EventDataValue $EventData 'LogonType'
                $AuthPackage = Get-EventDataValue $EventData 'AuthenticationPackageName'
            }
            4768 {  # Kerberos TGT
                $Workstation = Get-EventDataValue $EventData 'WorkstationName'
                $IpAddress   = Get-EventDataValue $EventData 'IpAddress'
                $AuthPackage = 'Kerberos'
            }
            4769 {  # Kerberos TGS
                $Workstation = Get-EventDataValue $EventData 'WorkstationName'
                $IpAddress   = Get-EventDataValue $EventData 'IpAddress'
                $AuthPackage = 'Kerberos'
            }
            4776 {  # NTLM认证
                $Workstation = Get-EventDataValue $EventData 'Workstation'
                $IpAddress   = Get-EventDataValue $EventData 'IpAddress'
                $AuthPackage = 'NTLM'
            }
        }

        # 清洗无效值
        if ($IpAddress -in @('-', '::1', '127.0.0.1', $null)) { $IpAddress = $null }
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

        $record = [PSCustomObject]@{
            Time         = $TimeCreated
            EventID      = $EventId
            User         = $Username
            Workstation  = $Workstation
            IPAddress    = $IpAddress
            LogonType    = $LogonTypeDesc
            AuthPackage  = $AuthPackage
            RawEvent     = $EventId  # 用于后续去重判断
        }

        [void]$Results.Add($record)

        if ($Workstation) {
            [void]$SeenWorkstations.Add($Workstation.ToUpper())
        }
    } catch {
        Write-Warn "解析单条事件时出错: $_"
    }
}

# ========== 输出结果 ==========
Write-Host "`n" -NoNewline
Write-Success "===== 详细登录记录（按时间倒序） ====="
$Results | Sort-Object Time -Descending | Format-Table Time, EventID, Workstation, IPAddress, LogonType, AuthPackage -AutoSize

Write-Host "`n" -NoNewline
Write-Success "===== 去重后的登录机器列表 ====="
if ($SeenWorkstations.Count -gt 0) {
    $SeenWorkstations | Sort-Object | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
} else {
    Write-Warn "  未能提取到有效的工作站名称（可能日志中未记录）"
}

# 统计
Write-Host "`n" -NoNewline
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
Write-Host "`n" -NoNewline
Write-Warn "===== 使用提示 ====="
Write-Warn "1. 此脚本依赖域控安全日志，如日志已被循环覆盖则查不到早期记录"
Write-Warn "2. 如需长期保留，建议启用 SIEM (如Splunk/ELK/Defender for Identity)"
Write-Warn "3. 事件ID 4768/4769 记录的是Kerberos认证，来源Workstation可能为$null"
Write-Warn "4. 最准确的'登录机器'信息通常在本机安全日志（非域控）的事件4624中"
Write-Warn "5. 如需查询所有域控（多域控环境），请对每台DC分别运行此脚本"
