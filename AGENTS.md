# MeloongCore Agents 指南

- 使用中文沟通；文本使用 UTF-8 无 BOM（65001），PowerShell 文本读写显式指定 UTF-8 编码。
- 通用代码兼顾 Windows 与 macOS；Shared 面向 .NET Standard 2.0，不依赖 WPF。Wpf 与 Tests 面向 .NET Framework 4.8。
- 本库是独立仓库，不依赖引用方的业务、私有目录或开发流程。

## 按任务查阅

| 任务 | 文档 |
| --- | --- |
| 修改代码或公共 API | [代码约定](.agents/仓库指示.md) |
| 查找通用能力、扩展方法及其语义差异 | [Shared 复用指南](.agents/Shared/架构分析.md) |
| 修改 Windows/WPF 能力或构建 WPF 工程 | [Wpf 说明](.agents/Wpf/架构分析.md) |
| 定位测试与测试环境 | [Tests 说明](.agents/Tests/架构分析.md) |

只读当前任务相关资料；入口、约定或关键行为变化时更新对应文档。
