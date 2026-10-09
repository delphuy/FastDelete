# FastDelete 右键菜单事故复盘 (2026-10-09)

## 现象
Win11 右键文件夹，菜单"Fast Delete - 极速删除"点击后弹
"该文件没有与之关联的应用来执行该操作"。1.0.16/1.0.17 重装后依旧。

## 根因链（逐层剥开）
1. **表面**：`HKLM\...\CLSID\{6F5E2C9D...}\InprocServer32` 值带双反斜杠
   （`C:\Program Files\FastDelete\\FastDelete.Shell.dll`），COM 加载失败。
2. **中层**：安装器从未把 `FastDelete.Shell.dll` 打进 Program Files；
   CLSID 键是早期脚本残留，菜单键上没有 InprocServer32 引用。
3. **深层（真正卡了 N 轮的原因）**：Inno `[Run] postinstall` 默认
   **降回原始非提权用户**运行被调程序。fdreg.ps1 每次都跑（日志 exit 0），
   但所有 HKLM 写入被拒 —— 之前误判成"UAC 弹不出"。加 `runascurrentuser`
   后注册表一次写对。

## 修复
- build.ps1: 单独 publish FastDelete.Shell.dll 进主 publish 目录（Inno 随之打包）
- iss: [Files] 带 fdreg.ps1/fdunreg.ps1；[Registry] 只留 legacy 段
  （Inno 7 把 Subkey 里的 CLSID 花括号当常量报 Unknown constant，绕开）
- iss: [Run]/[UninstallRun] 的 powershell 调用加 `runascurrentuser`
- fdreg.ps1: CLSID/菜单键注册，DLL 路径用字面量单反斜杠，带旧值自愈

## 教训（可复用）
1. **Inno postinstall [Run] 默认非提权** —— 写 HKLM 的脚本必须显式
   `runascurrentuser`（或去掉 postinstall，但那样进不了静默流程）。
   官方文档 topic_runsection.htm 明确写死这条默认值。
2. **Inno [Registry] 不能写带 {GUID} 的 Subkey**：`{{` 转义只在常量里有效，
   值/子键位置仍报 Unknown constant。GUID 注册交给 PowerShell。
3. **注册表验证要看值，不要只看"键在不在"**：双反斜杠路径
   `Test-Path` 不报错，但 COM 加载会失败。
4. **方法学**：ShellExecuteW 手工模拟与 Explorer verb 解析不是一回事；
   先拿 Inno 安装日志（/LOG=）确认脚本是否真的被调、以什么身份跑，
   再改代码。
5. 静默安装卡住时先看 `installer\diag\inno_install.log`，
   "Run as: Original user" 一行是提权陷阱的直接证据。
