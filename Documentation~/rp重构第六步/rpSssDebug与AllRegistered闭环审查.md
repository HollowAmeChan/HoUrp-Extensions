# RP SSS Debug 与 AllRegistered 闭环审查

## 新增 DebugView

```text
SSS.Mask
SSS.Source
SSS.Diffusion
SSS.CompositeWeight
```

## AllRegistered 规则

第六阶段继续沿用第五阶段策略：

- `AllRegistered` 在下拉中置顶。
- tile 按 `SourceResource + SourceSemantic` 去重。
- tile 绘制红色边框。
- tile 无黑色 padding。
- tile 左上角绘制短标签。
- 标签字号按 grid density 自适应。

## 成功标准

- `SSS.Source` tile 显示 source color。
- `SSS.Diffusion` tile 显示扩散结果。
- `SSS.Mask` tile 显示参与区域。
- `SSS.CompositeWeight` tile 显示最终合成权重。
- 空输入时 tile 仍有红框和标签，不误判为缺 tile。
