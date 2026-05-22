# RP 第七阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 待补 |
| `SemanticPostProcess` descriptor tests | 待补 |
| `SemanticPost.Mask` resource descriptor tests | 待定：若登记正式资源则必须补 |
| `SemanticPost.Mask` debug view mapping tests | 待补 |
| rule source -> semantic/resource mapping tests | 待补 |
| shader property mapping tests | 待补 |
| `git diff --check` | 待跑 |

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

## 风险点

- 规则语言过早膨胀。
- effect shader 各自实现 rule，导致语义规则散落。
- 直接读取 live camera color，重新触发第六阶段遇到的 RenderGraph 冲突。
- 把 Shoost final image effect 混入 SemanticPost。
