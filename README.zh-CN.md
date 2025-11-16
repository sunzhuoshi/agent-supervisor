# Agent Supervisor (智能助理监督器)

[![最新版本](https://img.shields.io/github/v/release/sunzhuoshi/agent-supervisor?label=version)](https://github.com/sunzhuoshi/agent-supervisor/releases/latest)
[![构建状态](https://img.shields.io/github/actions/workflow/status/sunzhuoshi/agent-supervisor/build.yml?branch=main)](https://github.com/sunzhuoshi/agent-supervisor/actions)
[![许可证](https://img.shields.io/github/license/sunzhuoshi/agent-supervisor)](LICENSE)

一个 Windows 系统托盘应用程序，通过监控拉取请求审查并发送桌面通知来帮助改进 GitHub Copilot 智能助理的工作流程。

[English](README.md) | 简体中文

## 功能特性

- **自动更新**：自动检查 GitHub 的新版本并提示一键升级
- **任务栏徽章**：在任务栏图标上显示待处理的 PR 审查请求数量
- **系统托盘应用**：在后台运行并显示系统托盘图标
- **监控 PR 审查**：自动监控分配给当前用户的拉取请求审查
- **桌面通知**：检测到新审查时显示 Windows 气球提示通知
- **PR 审查请求追踪**：在专用对话框中查看所有审查请求及其新/已读状态
- **标记为已读**：双击审查请求以打开它们并自动标记为已读
- **批量标记为已读**：一键将所有审查请求标记为已读
- **持久化存储**：审查请求在应用程序重启之间保存和恢复
- **设置界面**：易于使用的图形界面，用于配置 GitHub 个人访问令牌和轮询间隔
- **可配置轮询**：定期轮询 GitHub API，轮询间隔可配置（默认：60秒）
- **通知历史**：维护所有通知的持久历史记录
- **浏览器集成**：点击通知在默认浏览器中打开拉取请求
- **多语言支持**：支持英文和简体中文，根据系统区域设置自动检测
- **Windows 支持**：专为 Windows 构建，使用 C# 和 Windows Forms

## 系统要求

- Windows 操作系统
- .NET 8.0 SDK 或更高版本（用于构建）
- .NET 8.0 运行时（用于运行）
- GitHub 个人访问令牌及适当的权限

## GitHub 个人访问令牌设置

1. 转到 GitHub 设置 → 开发者设置 → 个人访问令牌 → 令牌（经典）
2. 点击"生成新令牌" → "生成新令牌（经典）"
3. 选择以下范围：
   - `repo`（完全控制私有仓库）
   - `read:user`（读取用户配置文件数据）
4. 复制生成的令牌（您将无法再次看到它！）

## 构建应用程序

```bash
# 恢复依赖项
dotnet restore

# 构建应用程序
dotnet build --configuration Release

# 可执行文件将位于：
# bin/Release/net8.0-windows/AgentSupervisor.exe
```

## 运行应用程序

1. **首次运行**：
   - 双击 `AgentSupervisor.exe`
   - 将出现设置对话框
   - 输入您的 GitHub 个人访问令牌
   - 配置轮询间隔（默认：60秒）
   - 点击"保存"

2. **任务栏**：
   - 应用程序显示在 Windows 任务栏中，带有自定义图标
   - 徽章覆盖显示待处理的 PR 审查请求数量（例如，红色气泡显示"3"）
   - 待处理计数更改时徽章会自动更新
   
3. **系统托盘**：
   - 应用程序也在系统托盘（通知区域）中运行
   - 在系统托盘中查找带有"A"的自定义紫色到蓝色渐变图标
   - 工具提示显示当前连接状态

4. **使用应用程序**：
   - **右键单击托盘图标**访问菜单：
     - "Copilot 代码审查请求" - 查看所有审查请求及其新/已读状态
     - "设置" - 更改您的配置
     - "关于" - 查看应用程序信息
     - "检查更新" - 手动检查应用程序更新
     - "退出" - 关闭应用程序
   - **双击托盘图标** - 查看 PR 审查请求
   - **双击审查请求** - 在浏览器中打开 PR 并标记为已读
   - **点击气球通知** - 在浏览器中打开 PR
   - **检查任务栏徽章** - 一眼查看有多少待审查的请求

## 语言支持

智能助理监督器支持多种语言：
- **英语** (默认)
- **简体中文**

应用程序会自动检测您的系统语言并使用相应的翻译。您也可以在设置对话框中手动更改语言。更改语言后，重新启动应用程序以使更改完全生效。

## 配置

配置存储在 Windows 注册表中，位于 `HKEY_CURRENT_USER\Software\AgentSupervisor`：

| 设置 | 注册表值 | 默认值 |
|---------|---------------|---------|
| GitHub 个人访问令牌 | PersonalAccessToken | (空) |
| 轮询间隔（秒） | PollingIntervalSeconds | 60 |
| 最大历史记录条数 | MaxHistoryEntries | 100 |
| 启用桌面通知 | EnableDesktopNotifications | 1 (启用) |
| 代理 URL | ProxyUrl | (空) |
| 使用代理 | UseProxy | 0 (禁用) |
| 暂停轮询 | PausePolling | 0 (禁用) |
| 语言 | Language | (自动检测) |

您可以使用设置界面配置设置（右键单击托盘图标 → 设置）。

## 安全注意事项

- **切勿提交您的 `config.json`** - 它包含您的个人访问令牌
- `.gitignore` 文件默认排除敏感文件
- 安全存储您的令牌，切勿分享
- 使用具有最小所需权限的令牌
- 定期轮换您的令牌

## 许可证

详见 LICENSE 文件。

## 贡献

欢迎贡献！请随时提交问题或拉取请求。

有关详细信息，请参阅 [英文 README](README.md)。
