#!/bin/bash
# PowerShell 脚本语法预检工具
# 在 Linux 上模拟验证 PowerShell 脚本的基本语法和逻辑

SCRIPT_PATH="$1"

if [ -z "$SCRIPT_PATH" ]; then
    echo "Usage: $0 <script.ps1>"
    exit 1
fi

echo "===== PowerShell 脚本预检 ====="
echo "文件: $SCRIPT_PATH"
echo ""

# 1. 检查 BOM
BOM=$(head -c 3 "$SCRIPT_PATH" | xxd -p 2>/dev/null || head -c 3 "$SCRIPT_PATH" | od -An -tx1 | tr -d ' ')
if [ "$BOM" = "efbbbf" ] || [ "$BOM" = "efbbbf0a" ]; then
    echo "✅ 编码: UTF-8 with BOM"
else
    echo "⚠️  编码: 无 BOM (Windows PowerShell ISE 可能显示乱码)"
fi

# 2. 检查基本语法结构
echo ""
echo "===== 语法结构检查 ====="

# param 块检查
if grep -q '^param(' "$SCRIPT_PATH" 2>/dev/null || grep -q '^param (' "$SCRIPT_PATH" 2>/dev/null; then
    echo "✅ param() 参数块存在"
else
    echo "⚠️  未找到 param() 参数块"
fi

# 检查是否有高级特性（CmdletBinding 等）
if grep -q '\[CmdletBinding' "$SCRIPT_PATH" 2>/dev/null; then
    echo "❌ 发现 [CmdletBinding()] - 需要 PowerShell 3.0+"
fi

# 检查 [Parameter()] 属性
if grep -q '\[Parameter' "$SCRIPT_PATH" 2>/dev/null; then
    echo "❌ 发现 [Parameter()] 属性 - 需要 PowerShell 3.0+"
fi

# 检查 [PSCustomObject]
if grep -q '\[PSCustomObject\]' "$SCRIPT_PATH" 2>/dev/null; then
    echo "❌ 发现 [PSCustomObject] - 需要 PowerShell 3.0+"
fi

# 检查 [ValidateNotNullOrEmpty]
if grep -q '\[ValidateNotNullOrEmpty' "$SCRIPT_PATH" 2>/dev/null; then
    echo "❌ 发现 [ValidateNotNullOrEmpty] - 需要 PowerShell 3.0+"
fi

# 3. 检查 PS2.0 兼容写法
echo ""
echo "===== PS 2.0 兼容性检查 ====="

if grep -q 'New-Object PSObject' "$SCRIPT_PATH" 2>/dev/null; then
    echo "✅ 使用 New-Object PSObject (PS2.0 兼容)"
else
    echo "⚠️  未使用 New-Object PSObject"
fi

if grep -q 'Add-Member' "$SCRIPT_PATH" 2>/dev/null; then
    echo "✅ 使用 Add-Member (PS2.0 兼容)"
else
    echo "⚠️  未使用 Add-Member"
fi

if grep -q '\[System.Collections.Generic.HashSet' "$SCRIPT_PATH" 2>/dev/null; then
    echo "⚠️  使用 HashSet - 需要 .NET Framework 3.5+，PS2.0 可能不支持"
fi

# 4. 检查括号匹配
echo ""
echo "===== 括号匹配检查 ====="

OPEN_PAREN=$(grep -o '(' "$SCRIPT_PATH" | wc -l)
CLOSE_PAREN=$(grep -o ')' "$SCRIPT_PATH" | wc -l)
OPEN_BRACE=$(grep -o '{' "$SCRIPT_PATH" | wc -l)
CLOSE_BRACE=$(grep -o '}' "$SCRIPT_PATH" | wc -l)
OPEN_BRACKET=$(grep -o '\[' "$SCRIPT_PATH" | wc -l)
CLOSE_BRACKET=$(grep -o '\]' "$SCRIPT_PATH" | wc -l)

if [ "$OPEN_PAREN" -eq "$CLOSE_PAREN" ]; then
    echo "✅ 圆括号匹配: $OPEN_PAREN = $CLOSE_PAREN"
else
    echo "❌ 圆括号不匹配: 开=$OPEN_PAREN 关=$CLOSE_PAREN"
fi

if [ "$OPEN_BRACE" -eq "$CLOSE_BRACE" ]; then
    echo "✅ 花括号匹配: $OPEN_BRACE = $CLOSE_BRACE"
else
    echo "❌ 花括号不匹配: 开=$OPEN_BRACE 关=$CLOSE_BRACE"
fi

if [ "$OPEN_BRACKET" -eq "$CLOSE_BRACKET" ]; then
    echo "✅ 方括号匹配: $OPEN_BRACKET = $CLOSE_BRACKET"
else
    echo "❌ 方括号不匹配: 开=$OPEN_BRACKET 关=$CLOSE_BRACKET"
fi

# 5. 检查关键函数/变量引用
echo ""
echo "===== 关键代码检查 ====="

if grep -q 'Get-ADUser' "$SCRIPT_PATH" 2>/dev/null; then
    echo "✅ 使用 Get-ADUser (需要 RSAT / ActiveDirectory 模块)"
fi

if grep -q 'Get-WinEvent' "$SCRIPT_PATH" 2>/dev/null; then
    echo "✅ 使用 Get-WinEvent (Windows 事件日志)"
fi

if grep -q 'System.Windows.Forms' "$SCRIPT_PATH" 2>/dev/null; then
    echo "✅ 使用 Windows Forms (弹窗功能)"
fi

if grep -q 'Read-Host' "$SCRIPT_PATH" 2>/dev/null; then
    echo "✅ 使用 Read-Host (暂停等待用户输入)"
fi

# 6. 检查常见错误
echo ""
echo "===== 常见错误检查 ====="

# 检查是否有未闭合的字符串
UNCLOSED_STRINGS=$(grep -c '"[^"]*$' "$SCRIPT_PATH" 2>/dev/null)
if [ "$UNCLOSED_STRINGS" -gt 0 ]; then
    echo "⚠️  发现 $UNCLOSED_STRINGS 个可能未闭合的双引号字符串"
fi

UNCLOSED_SINGLE=$(grep -c "'[^']*$" "$SCRIPT_PATH" 2>/dev/null)
if [ "$UNCLOSED_SINGLE" -gt 0 ]; then
    echo "⚠️  发现 $UNCLOSED_SINGLE 个可能未闭合的单引号字符串"
fi

# 检查 backtick 在字符串中的使用（Windows 转义符，Linux bash 会误解）
if grep -E '\$.*`[nrt]' "$SCRIPT_PATH" > /dev/null 2>&1; then
    echo "✅ 检测到 PowerShell 反引号转义序列 (``n, ``r 等)"
fi

echo ""
echo "===== 预检完成 ====="
echo ""
echo "注意: 此工具仅做静态分析，无法验证:"
echo "  - ActiveDirectory 模块是否存在"
echo "  - 域控连接是否成功"
echo "  - 事件日志 XPath 查询是否正确"
echo "  - 实际运行时的权限和策略"
