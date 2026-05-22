# RP 旧 HoPost 子集迁移审查

## 旧实现来源

```text
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\HoPostProcessing
```

## 可参考内容

- `HoPostProcessLayer.cs`：layer、AOV rule、blend、effect 参数。
- `HoPostAovMask.hlsl`：rule source/operator/combine 思路。
- `LayerBlit.shader`：layer composite 思路。
- `EdgeLight.shader` / `Outline.shader`：第一批效果候选。

## 第一版迁移内容

```text
Rule source / operator / combine 的概念
Layer 顺序执行的概念
SemanticTint 最小效果
```

## 不迁移内容

- 旧 Volume stack。
- 旧 shader property 命名。
- 旧全局纹理 ABI。
- 旧 compatibility path。
- 旧 effect enum 全量。
- DropShadow。
- DepthOfField。
- PostLighting。
- CustomMaterial。

## 判定标准

旧代码只能回答“行为上应该支持什么”，不能决定新 RP 的命名、资源生命周期和 ABI。
