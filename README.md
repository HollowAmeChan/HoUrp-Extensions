# HoUrp Extensions

HoUrp Extensions 是 Hollow 用于重构 `lilToon-URP-Extensions` 的新 URP 扩展包。

本仓库明确只支持 URP，不支持 Built-in Render Pipeline，也不支持 HDRP。目标 Unity
版本为 `6000.3+`。由于工作区使用的是本地魔改版 URP，`package.json` 里刻意不声明
`com.unity.render-pipelines.universal` 或 `com.unity.render-pipelines.core`
依赖。

## 包信息

- 包名：`com.hollow.hourp-extensions`
- 显示名：`HoUrp Extensions`
- 作者：`Hollow`
- Unity 版本：`6000.3+`
- 渲染管线：仅 URP
- 技术方向：RenderGraph-first
- Manifest 依赖：无

## 重构目标

这个仓库不是直接继续堆叠旧 `ScriptableRenderPass` 兼容路径，而是把功能拆成更清晰的
RenderGraph 模块：

- 把 renderer feature 的调度逻辑和具体渲染资源声明分开。
- 用 RenderGraph texture/resource handle 表达 pass 之间的数据依赖。
- 减少全局 RT 的隐式生命周期，优先让 RenderGraph 管理创建、读取、写入和释放。
- 保留和本地 `HoToon`、`HoPbr`、`HoNpr`、`lilToon`、`lilPBR` shader pass 对接的空间。
- 旧包里的 OIT、HoAOV、角色特化、HoPost、Shoost、平面反射、HoShadowCast 等功能后续按模块迁移。

## 目录结构

- `Runtime/RenderGraph/`：RenderGraph pass、资源声明和共享调度工具。
- `Runtime/Features/`：URP RendererFeature 入口。
- `Runtime/Resources/`：运行时 shader、compute shader、材质和默认资源。
- `Editor/`：RendererFeature、配置资产和调试工具的编辑器扩展。
- `Documentation~/`：迁移记录、设计说明和旧包对照。
- `Tests/`：运行时和编辑器测试。

当前仓库只包含重构骨架，不包含旧包实现代码。

## 安装

在 Unity 项目的 `Packages/manifest.json` 中通过本地路径添加：

```json
{
  "dependencies": {
    "com.hollow.hourp-extensions": "file:D:/Unity_Fork/HoUrp-Extensions"
  }
}
```

项目中需要有可用的 URP，但本包不会通过自己的 manifest 强制声明 URP 依赖，方便接入
本地魔改版 URP。

## 迁移来源

旧实现参考仓库：

```text
D:/Unity_Fork/lilToon-URP-Extensions
```

迁移时应优先保留功能契约，而不是照搬旧实现结构。
