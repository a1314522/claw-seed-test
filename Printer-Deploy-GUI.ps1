#Requires -Version 5.1
<#
.SYNOPSIS
    管理员批量远程安装打印机工具

.DESCRIPTION
    IT 管理员运行此脚本，输入域管理员凭据，选择打印机和目标机器列表，
    通过 PowerShell Remoting (WinRM) 在多台机器上远程静默安装打印机。

.Notes
    - 需要目标机器启用 WinRM (域内默认已启用)
    - 需要域管理员或具有目标机器本地管理员权限的凭据
    - 运行此脚本的管理机本身需要有 RSAT 工具（如需从 AD 读取 OU）
    - 如果目标机器没有打印机驱动，需先通过 GPO / pnputil 预装驱动
#

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

#region ==================== GUI 构建 ====================

$form = New-Object System.Windows.Forms.Form
$form.Text = "批量打印机部署工具 (管理员)"
$form.Size = New-Object System.Drawing.Size(800, 650)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false

$Y = 15
$LH = 25  # label height
$TH = 23  # textbox height
$GAP = 35

# --- 凭据区 ---
$lblCred = New-Object System.Windows.Forms.Label
$lblCred.Location = New-Object System.Drawing.Point(15, $Y)
$lblCred.Size = New-Object System.Drawing.Size(760, 20)
$lblCred.Text = "===== 凭据 ====="
$lblCred.Font = New-Object System.Drawing.Font("Microsoft YaHei", 9, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($lblCred)
$Y += $LH

$lblDomain = New-Object System.Windows.Forms.Label
$lblDomain.Location = New-Object System.Drawing.Point(15, $Y)
$lblDomain.Size = New-Object System.Drawing.Size(60, 20)
$lblDomain.Text = "域名:"
$form.Controls.Add($lblDomain)

$txtDomain = New-Object System.Windows.Forms.TextBox
$txtDomain.Location = New-Object System.Drawing.Point(75, $Y)
$txtDomain.Size = New-Object System.Drawing.Size(150, $TH)
$form.Controls.Add($txtDomain)

$lblAdminUser = New-Object System.Windows.Forms.Label
$lblAdminUser.Location = New-Object System.Drawing.Point(245, $Y)
$lblAdminUser.Size = New-Object System.Drawing.Size(60, 20)
$lblAdminUser.Text = "用户名:"
$form.Controls.Add($lblAdminUser)

$txtAdminUser = New-Object System.Windows.Forms.TextBox
$txtAdminUser.Location = New-Object System.Drawing.Point(305, $Y)
$txtAdminUser.Size = New-Object System.Drawing.Size(150, $TH)
$form.Controls.Add($txtAdminUser)

$lblAdminPass = New-Object System.Windows.Forms.Label
$lblAdminPass.Location = New-Object System.Drawing.Point(475, $Y)
$lblAdminPass.Size = New-Object System.Drawing.Size(60, 20)
$lblAdminPass.Text = "密码:"
$form.Controls.Add($lblAdminPass)

$txtAdminPass = New-Object System.Windows.Forms.TextBox
$txtAdminPass.Location = New-Object System.Drawing.Point(535, $Y)
$txtAdminPass.Size = New-Object System.Drawing.Size(150, $TH)
$txtAdminPass.PasswordChar = '*'
$form.Controls.Add($txtAdminPass)

$btnLoadDomain = New-Object System.Windows.Forms.Button
$btnLoadDomain.Location = New-Object System.Drawing.Point(695, $Y - 2)
$btnLoadDomain.Size = New-Object System.Drawing.Size(80, 26)
$btnLoadDomain.Text = "获取域名"
$form.Controls.Add($btnLoadDomain)
$Y += $GAP + 10

# --- 打印机配置区 ---
$lblPrinter = New-Object System.Windows.Forms.Label
$lblPrinter.Location = New-Object System.Drawing.Point(15, $Y)
$lblPrinter.Size = New-Object System.Drawing.Size(760, 20)
$lblPrinter.Text = "===== 打印机配置 ====="
$lblPrinter.Font = New-Object System.Drawing.Font("Microsoft YaHei", 9, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($lblPrinter)
$Y += $LH

$lblPName = New-Object System.Windows.Forms.Label
$lblPName.Location = New-Object System.Drawing.Point(15, $Y)
$lblPName.Size = New-Object System.Drawing.Size(80, 20)
$lblPName.Text = "打印机名称:"
$form.Controls.Add($lblPName)

$txtPName = New-Object System.Windows.Forms.TextBox
$txtPName.Location = New-Object System.Drawing.Point(95, $Y)
$txtPName.Size = New-Object System.Drawing.Size(200, $TH)
$txtPName.Text = "Floor3-HP-Color"
$form.Controls.Add($txtPName)

$lblPIP = New-Object System.Windows.Forms.Label
$lblPIP.Location = New-Object System.Drawing.Point(315, $Y)
$lblPIP.Size = New-Object System.Drawing.Size(60, 20)
$lblPIP.Text = "IP地址:"
$form.Controls.Add($lblPIP)

$txtPIP = New-Object System.Windows.Forms.TextBox
$txtPIP.Location = New-Object System.Drawing.Point(375, $Y)
$txtPIP.Size = New-Object System.Drawing.Size(120, $TH)
$txtPIP.Text = "192.168.1.100"
$form.Controls.Add($txtPIP)

$lblDriver = New-Object System.Windows.Forms.Label
$lblDriver.Location = New-Object System.Drawing.Point(515, $Y)
$lblDriver.Size = New-Object System.Drawing.Size(70, 20)
$lblDriver.Text = "驱动名称:"
$form.Controls.Add($lblDriver)

$txtDriver = New-Object System.Windows.Forms.TextBox
$txtDriver.Location = New-Object System.Drawing.Point(585, $Y)
$txtDriver.Size = New-Object System.Drawing.Size(180, $TH)
$txtDriver.Text = "HP Universal PCL6"
$form.Controls.Add($txtDriver)

$Y += $GAP

$chkDefault = New-Object System.Windows.Forms.CheckBox
$chkDefault.Location = New-Object System.Drawing.Point(95, $Y)
$chkDefault.Size = New-Object System.Drawing.Size(200, 20)
$chkDefault.Text = "设为默认打印机"
$chkDefault.Checked = $true
$form.Controls.Add($chkDefault)

$Y += $GAP

# --- 目标机器区 ---
$lblTargets = New-Object System.Windows.Forms.Label
$lblTargets.Location = New-Object System.Drawing.Point(15, $Y)
$lblTargets.Size = New-Object System.Drawing.Size(760, 20)
$lblTargets.Text = "===== 目标机器 ====="
$lblTargets.Font = New-Object System.Drawing.Font("Microsoft YaHei", 9, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($lblTargets)
$Y += $LH

# TabControl: 手动输入 / AD OU / 从文件导入
$tabTargets = New-Object System.Windows.Forms.TabControl
$tabTargets.Location = New-Object System.Drawing.Point(15, $Y)
$tabTargets.Size = New-Object System.Drawing.Size(760, 200)
$form.Controls.Add($tabTargets)

# Tab 1: 手动输入
$tabManual = New-Object System.Windows.Forms.TabPage
$tabManual.Text = "手动输入 (逗号/换行分隔)"
$txtManual = New-Object System.Windows.Forms.TextBox
$txtManual.Multiline = $true
$txtManual.Dock = "Fill"
$txtManual.Font = New-Object System.Drawing.Font("Consolas", 10)
$txtManual.Text = "PC-IT-001`nPC-IT-002`nPC-FINANCE-003"
$tabManual.Controls.Add($txtManual)
$tabTargets.TabPages.Add($tabManual)

# Tab 2: AD OU
$tabAD = New-Object System.Windows.Forms.TabPage
$tabAD.Text = "从 AD OU 读取"
$lblOU = New-Object System.Windows.Forms.Label
$lblOU.Location = New-Object System.Drawing.Point(10, 10)
$lblOU.Size = New-Object System.Drawing.Size(60, 20)
$lblOU.Text = "OU路径:"
$tabAD.Controls.Add($lblOU)

$txtOU = New-Object System.Windows.Forms.TextBox
$txtOU.Location = New-Object System.Drawing.Point(70, 8)
$txtOU.Size = New-Object System.Drawing.Size(500, 23)
$txtOU.Text = "OU=Workstations,DC=rorze,DC=local"
$tabAD.Controls.Add($txtOU)

$btnLoadOU = New-Object System.Windows.Forms.Button
$btnLoadOU.Location = New-Object System.Drawing.Point(580, 7)
$btnLoadOU.Size = New-Object System.Drawing.Size(120, 26)
$btnLoadOU.Text = "加载该OU下的电脑"
$tabAD.Controls.Add($btnLoadOU)

$lvOU = New-Object System.Windows.Forms.ListView
$lvOU.Location = New-Object System.Drawing.Point(10, 40)
$lvOU.Size = New-Object System.Drawing.Size(730, 120)
$lvOU.View = "Details"
$lvOU.Columns.Add("计算机名", 200) | Out-Null
$lvOU.Columns.Add("操作系统", 250) | Out-Null
$lvOU.Columns.Add("最后登录", 200) | Out-Null
$lvOU.CheckBoxes = $true
$tabAD.Controls.Add($lvOU)
$tabTargets.TabPages.Add($tabAD)

# Tab 3: 从文件
$tabFile = New-Object System.Windows.Forms.TabPage
$tabFile.Text = "从文件导入 (TXT/CSV)"
$btnBrowse = New-Object System.Windows.Forms.Button
$btnBrowse.Location = New-Object System.Drawing.Point(10, 10)
$btnBrowse.Size = New-Object System.Drawing.Size(100, 26)
$btnBrowse.Text = "浏览文件..."
$tabFile.Controls.Add($btnBrowse)

$lblFilePath = New-Object System.Windows.Forms.Label
$lblFilePath.Location = New-Object System.Drawing.Point(120, 14)
$lblFilePath.Size = New-Object System.Drawing.Size(600, 20)
$lblFilePath.Text = "未选择文件"
$tabFile.Controls.Add($lblFilePath)

$txtFilePreview = New-Object System.Windows.Forms.TextBox
$txtFilePreview.Multiline = $true
$txtFilePreview.ReadOnly = $true
$txtFilePreview.Location = New-Object System.Drawing.Point(10, 45)
$txtFilePreview.Size = New-Object System.Drawing.Size(730, 110)
$txtFilePreview.Font = New-Object System.Drawing.Font("Consolas", 10)
$tabFile.Controls.Add($txtFilePreview)
$tabTargets.TabPages.Add($tabFile)

$Y += 210

# --- 进度与日志 ---
$lblProgress = New-Object System.Windows.Forms.Label
$lblProgress.Location = New-Object System.Drawing.Point(15, $Y)
$lblProgress.Size = New-Object System.Drawing.Size(760, 20)
$lblProgress.Text = "就绪"
$form.Controls.Add($lblProgress)
$Y += $LH

$progressBar = New-Object System.Windows.Forms.ProgressBar
$progressBar.Location = New-Object System.Drawing.Point(15, $Y)
$progressBar.Size = New-Object System.Drawing.Size(760, 20)
$progressBar.Minimum = 0
$progressBar.Maximum = 100
$form.Controls.Add($progressBar)
$Y += $GAP

$txtLog = New-Object System.Windows.Forms.TextBox
$txtLog.Multiline = $true
$txtLog.ScrollBars = "Vertical"
$txtLog.ReadOnly = $true
$txtLog.Location = New-Object System.Drawing.Point(15, $Y)
$txtLog.Size = New-Object System.Drawing.Size(760, 120)
$txtLog.Font = New-Object System.Drawing.Font("Consolas", 9)
$form.Controls.Add($txtLog)
$Y += 130

# --- 操作按钮 ---
$btnDeploy = New-Object System.Windows.Forms.Button
$btnDeploy.Location = New-Object System.Drawing.Point(15, $Y)
$btnDeploy.Size = New-Object System.Drawing.Size(120, 32)
$btnDeploy.Text = "开始部署"
$btnDeploy.Font = New-Object System.Drawing.Font("Microsoft YaHei", 9, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($btnDeploy)

$btnTestConn = New-Object System.Windows.Forms.Button
$btnTestConn.Location = New-Object System.Drawing.Point(145, $Y)
$btnTestConn.Size = New-Object System.Drawing.Size(120, 32)
$btnTestConn.Text = "测试连接"
$form.Controls.Add($btnTestConn)

$btnExport = New-Object System.Windows.Forms.Button
$btnExport.Location = New-Object System.Drawing.Point(275, $Y)
$btnExport.Size = New-Object System.Drawing.Size(120, 32)
$btnExport.Text = "导出结果"
$form.Controls.Add($btnExport)

#endregion

#region ==================== 事件处理 ====================

function Write-Log {
    param([string]$msg)
    $timestamp = Get-Date -Format "HH:mm:ss"
    $txtLog.AppendText("[$timestamp] $msg`r`n")
    $txtLog.ScrollToCaret()
}

# 获取当前域名
$btnLoadDomain.Add_Click({
    try {
        $domain = [System.DirectoryServices.ActiveDirectory.Domain]::GetCurrentDomain().Name
        $txtDomain.Text = $domain
        Write-Log "自动获取域名: $domain"
    } catch {
        Write-Log "获取域名失败: $($_.Exception.Message)"
        [System.Windows.Forms.MessageBox]::Show("无法自动获取域名，请手动填写", "提示", "OK", "Warning")
    }
})

# 加载 AD OU 中的计算机
$btnLoadOU.Add_Click({
    $lvOU.Items.Clear()
    if (-not (Get-Module -ListAvailable -Name ActiveDirectory)) {
        Write-Log "错误: 未安装 ActiveDirectory 模块 (RSAT)，无法读取 AD"
        return
    }
    Import-Module ActiveDirectory -ErrorAction SilentlyContinue
    $ou = $txtOU.Text.Trim()
    Write-Log "正在读取 OU: $ou ..."
    try {
        $computers = Get-ADComputer -Filter * -SearchBase $ou -Properties OperatingSystem, LastLogonDate -ErrorAction Stop
        foreach ($comp in $computers) {
            $item = New-Object System.Windows.Forms.ListViewItem($comp.Name)
            $item.SubItems.Add($comp.OperatingSystem)
            $item.SubItems.Add($comp.LastLogonDate)
            $item.Checked = $true
            $lvOU.Items.Add($item)
        }
        Write-Log "加载完成: 共 $($computers.Count) 台计算机"
    } catch {
        Write-Log "读取 OU 失败: $($_.Exception.Message)"
    }
})

# 浏览文件
$btnBrowse.Add_Click({
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Filter = "Text/CSV files (*.txt;*.csv)|*.txt;*.csv|All files (*.*)|*.*"
    if ($dlg.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $lblFilePath.Text = $dlg.FileName
        $lines = Get-Content $dlg.FileName | Where-Object { $_.Trim() -ne "" }
        $txtFilePreview.Text = ($lines | Select-Object -First 50) -join "`r`n"
        if ($lines.Count -gt 50) {
            $txtFilePreview.AppendText("`r`n... 共 $($lines.Count) 行，仅显示前50")
        }
        Write-Log "导入文件: $($dlg.FileName), 共 $($lines.Count) 行"
    }
})

# 获取目标机器列表
function Get-TargetComputers {
    $tab = $tabTargets.SelectedTab
    $list = @()
    switch ($tab.Text) {
        "手动输入 (逗号/换行分隔)" {
            $list = $txtManual.Text -split "[`,\r\n]" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
        }
        "从 AD OU 读取" {
            $list = $lvOU.Items | Where-Object { $_.Checked } | ForEach-Object { $_.Text }
        }
        "从文件导入 (TXT/CSV)" {
            if (Test-Path $lblFilePath.Text) {
                $list = Get-Content $lblFilePath.Text | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
            }
        }
    }
    return $list | Select-Object -Unique
}

# 测试连接
$btnTestConn.Add_Click({
    $computers = Get-TargetComputers
    if ($computers.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show("未选择任何目标机器", "提示", "OK", "Warning")
        return
    }

    $cred = Get-CredentialObject
    if (-not $cred) { return }

    $lblProgress.Text = "正在测试 $($computers.Count) 台机器的 WinRM 连接..."
    $progressBar.Value = 0
    $progressBar.Maximum = $computers.Count
    $txtLog.Clear()

    $success = 0
    $fail = 0

    foreach ($comp in $computers) {
        try {
            $session = New-PSSession -ComputerName $comp -Credential $cred -ErrorAction Stop
            Remove-PSSession $session
            Write-Log "✅ $comp - WinRM 连接成功"
            $success++
        } catch {
            Write-Log "❌ $comp - 连接失败: $($_.Exception.Message)"
            $fail++
        }
        $progressBar.Value++
        [System.Windows.Forms.Application]::DoEvents()
    }

    $lblProgress.Text = "测试完成: $success 成功, $fail 失败 (共 $($computers.Count))"
    [System.Windows.Forms.MessageBox]::Show("测试完成`n成功: $success`n失败: $fail", "结果", "OK", "Information")
})

# 构建凭据对象
function Get-CredentialObject {
    $domain = $txtDomain.Text.Trim()
    $user = $txtAdminUser.Text.Trim()
    $pass = $txtAdminPass.Text

    if (-not $user) {
        [System.Windows.Forms.MessageBox]::Show("请输入管理员用户名", "提示", "OK", "Warning")
        return $null
    }
    if (-not $pass) {
        [System.Windows.Forms.MessageBox]::Show("请输入管理员密码", "提示", "OK", "Warning")
        return $null
    }

    $fullUser = if ($domain) { "$domain\$user" } else { $user }
    return New-Object System.Management.Automation.PSCredential($fullUser, (ConvertTo-SecureString $pass -AsPlainText -Force))
}

# 部署打印机
$btnDeploy.Add_Click({
    $computers = Get-TargetComputers
    if ($computers.Count -eq 0) {
        [System.Windows.Forms.MessageBox]::Show("未选择任何目标机器", "提示", "OK", "Warning")
        return
    }

    $pName = $txtPName.Text.Trim()
    $pIP = $txtPIP.Text.Trim()
    $pDriver = $txtDriver.Text.Trim()

    if (-not $pName -or -not $pIP -or -not $pDriver) {
        [System.Windows.Forms.MessageBox]::Show("请填写完整的打印机配置信息", "提示", "OK", "Warning")
        return
    }

    $cred = Get-CredentialObject
    if (-not $cred) { return }

    $setDefault = $chkDefault.Checked

    $confirm = [System.Windows.Forms.MessageBox]::Show(
        "即将在 $($computers.Count) 台机器上安装打印机:`n名称: $pName`nIP: $pIP`n驱动: $pDriver`n设为默认: $setDefault`n`n确认开始部署?",
        "确认部署",
        "YesNo",
        "Question"
    )
    if ($confirm -ne [System.Windows.Forms.DialogResult]::Yes) { return }

    $lblProgress.Text = "正在部署到 $($computers.Count) 台机器..."
    $progressBar.Value = 0
    $progressBar.Maximum = $computers.Count
    $txtLog.Clear()

    $results = @()

    foreach ($comp in $computers) {
        Write-Log "正在处理: $comp ..."
        try {
            $result = Invoke-Command -ComputerName $comp -Credential $cred -ScriptBlock {
                param($PortName, $PrinterName, $IP, $DriverName, $SetDefault)

                $errorMsg = $null
                try {
                    # 检查驱动是否已存在
                    $existingDriver = Get-PrinterDriver -Name $DriverName -ErrorAction SilentlyContinue
                    if (-not $existingDriver) {
                        # 注意: 如果驱动不在系统驱动库中，需要提前导入 .inf
                        # 这里假设驱动已在系统库中，或已通过其他方式部署
                        return @{ Status = "需要预装驱动"; Detail = "驱动 '$DriverName' 不在系统驱动库中，请先上传 .inf 并安装驱动包" }
                    }

                    # 创建端口（如果不存在）
                    $existingPort = Get-PrinterPort -Name $PortName -ErrorAction SilentlyContinue
                    if (-not $existingPort) {
                        Add-PrinterPort -Name $PortName -PrinterHostAddress $IP -PortNumber 9100
                    }

                    # 创建打印机（如果不存在）
                    $existingPrinter = Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue
                    if (-not $existingPrinter) {
                        Add-Printer -Name $PrinterName -PortName $PortName -DriverName $DriverName
                    }

                    # 设为默认
                    if ($SetDefault) {
                        Set-Printer -Name $PrinterName -IsDefault $true
                    }

                    return @{ Status = "成功"; Detail = "打印机已安装" }
                } catch {
                    return @{ Status = "失败"; Detail = $_.Exception.Message }
                }
            } -ArgumentList "IP_$pIP", $pName, $pIP, $pDriver, $setDefault -ErrorAction Stop

            Write-Log "✅ $comp - $($result.Status): $($result.Detail)"
            $results += [PSCustomObject]@{
                Computer = $comp
                Status   = $result.Status
                Detail   = $result.Detail
            }
        } catch {
            Write-Log "❌ $comp - 部署失败: $($_.Exception.Message)"
            $results += [PSCustomObject]@{
                Computer = $comp
                Status   = "失败"
                Detail   = $_.Exception.Message
            }
        }

        $progressBar.Value++
        [System.Windows.Forms.Application]::DoEvents()
    }

    $successCount = ($results | Where-Object { $_.Status -eq "成功" }).Count
    $lblProgress.Text = "部署完成: $successCount / $($computers.Count) 成功"
    [System.Windows.Forms.MessageBox]::Show("部署完成`n成功: $successCount / $($computers.Count)", "结果", "OK", "Information")

    # 保存结果到表单属性供导出使用
    $form.Tag = $results
})

# 导出结果
$btnExport.Add_Click({
    $results = $form.Tag
    if (-not $results) {
        [System.Windows.Forms.MessageBox]::Show("没有可导出的结果，请先运行部署", "提示", "OK", "Warning")
        return
    }
    $dlg = New-Object System.Windows.Forms.SaveFileDialog
    $dlg.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
    $dlg.FileName = "PrinterDeploy_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
    if ($dlg.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
        $results | Export-Csv -Path $dlg.FileName -NoTypeInformation -Encoding UTF8
        Write-Log "结果已导出: $($dlg.FileName)"
    }
})

#endregion

[System.Windows.Forms.Application]::Run($form)