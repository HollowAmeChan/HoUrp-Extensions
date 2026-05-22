# RP 第七阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 已补：资源/语义/debug view 计数更新 |
| `SemanticPostProcess` descriptor tests | 已补：produced `SemanticPost.Mask`，consumed AOV + SSS 输入 |
| `SemanticPost.Mask` resource descriptor tests | 已补：正式资源，`R8G8B8A8_UNorm`，zero clear |
| `SemanticPost.Mask` debug view mapping tests | 已补：`SemanticPost.Mask -> Composite.SemanticPostMask` |
| rule source -> semantic/resource mapping tests | 部分覆盖：契约覆盖 AOV/SSS 消费，具体 rule shader 需 Unity 复测 |
| shader property mapping tests | 已补：mask/layer/rule property id |
| `git diff --check` | 已跑：仅 LF/CRLF warning，无 whitespace error |

## 手动 Unity 验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering`（可选）
3. `HoURP Semantic Post Process`
4. `HoURP AOV Debug`

测试对象：

```text
ObjectSemanticAuthoring:
  maskWeight = 1
  objectCustom = non-zero

MaterialSemanticAuthoring:
  materialClass = non-zero
  thickness = non-zero
  materialCustom = non-zero
  sssWeight = optional non-zero
```

期望：

| 场景 | 期望 |
| --- | --- |
| SemanticPost disabled | 画面与第六阶段一致 |
| Always rule | 全屏 tint 可控 |
| ObjectCustom rule | 只影响指定 object custom 区域 |
| Material rule | 只影响指定材质语义区域 |
| SSS rule | 可选择 SSS 参与区域 |
| Mask debug | 显示 SemanticPost rule mask |
| Transparent present | 无 `_CameraTargetAttachment` RenderGraph 读写冲突 |
| AllRegistered | 包含 SemanticPost debug tile |

当前代码状态：

- `SemanticPostProcessRendererFeature` 已从单 pass AOV read probe 改为 `SemanticPost Mask -> SemanticTint Composite` 两段。
- `SemanticPost.Mask` 已登记为正式 RenderGraph 资源，并进入 DebugComposite / AllRegistered。
- 第一版固定最多 4 个 layer、每层最多 4 条 rule；effect 只实现 `SemanticTint`。
- SSS 输入为可选读取：若 `Sss.Source` / `Sss.Diffusion` 未声明，则回退到 `Aov.SssSource`，避免依赖旧全局纹理状态。
- Composite 读取的是 camera color copy，不直接读写 live camera color。

## 风险点

- 规则语言过早膨胀。
- effect shader 各自实现 rule，导致语义规则散落。
- 直接读取 live camera color，重新触发第六阶段遇到的 RenderGraph 冲突。
- 把 Shoost final image effect 混入 SemanticPost。
- 尚未运行 Unity batchmode / EditMode tests；当前包目录没有独立 Unity project / `.csproj`。
