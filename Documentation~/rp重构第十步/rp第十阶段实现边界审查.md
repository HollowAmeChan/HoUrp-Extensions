# RP 第十阶段实现边界审查

## 边界原则

第十阶段只做材质生产者契约，不做完整材质系统，也不做 Weighted OIT runtime。

```text
第十步:
  material shader ABI
  generated shader prototype
  preset / feature block descriptor
  AOV / SSS / SemanticPost producer 验证
  OIT-ready material pass

第十一步:
  Weighted OIT RenderGraph runtime
  Oit.* resources
  accumulation / revealage / composite
```

## 做

- 新 HLSL ABI include。
- 独立最小 generated shader。
- `UniversalForward` 最小显示 pass。
- `HoUrpAovOutput` pass。
- `HoUrpOitAccumulation` pass。
- Material preset / feature block 描述模型。
- `GeneratedMaterial` producer 契约。
- `SupportsOit` / `ParticipatesOit` 元数据。
- 相关 tests / 文档。

## 不做

- 不迁移旧 `lilToon/lilPBR`。
- 不复制旧材质 inspector。
- 不接 URP Lit full shader。
- 不做完整 shader generator。
- 不做材质 UI。
- 不做 Weighted OIT runtime。
- 不创建 `Oit.*` RenderGraph resources。
- 不做 OIT composite shader。
- 不做透明 SSS / transparent AOV。
- 不做 HoShadow receiver。
- 不做 CharacterSpecialization 半透明排序。

## 允许的依赖

| 依赖 | 说明 |
| --- | --- |
| URP `Core.hlsl` | 基础矩阵、坐标变换 |
| Unity texture/sampler 宏 | 基础采样 |
| HoURP 自己的 shader ABI include | 第十步新增 |
| HoURP contract registry | producer / semantic / debug 查询 |

## 禁止的依赖

| 依赖 | 原因 |
| --- | --- |
| `lilToon` include | 旧材质系统 |
| `lilPBR` include | 旧材质系统 |
| URP Lit full pass include | 隐藏 keyword / inspector 假设 |
| `_lilHoAov*` | 旧资源名 |
| `_HoAov*` | 旧材质属性名 |
| `_lilOIT*` | 旧 OIT ABI |
| `lilToonOIT` | 旧 OIT pass 名 |

## 与大纲一致性

第十阶段必须遵守 `rp设计哲学底线.md`：

- 新 RP 契约先于新材质系统。
- 语义必须显式注册。
- Feature 必须声明自己。
- Debug 是一等公民。
- 旧系统只能作为迁移参照。

第十阶段也必须遵守 `rp重构初步大纲.md`：

- 第十优先级是材质系统接入新 RP 的契约准备。
- 第十一优先级才是 Weighted OIT runtime 验证。
- 第十二优先级才是 Weighted OIT 完整化与透明语义扩展。

## 风险

- 因为要给第十一步 OIT 铺路，越界实现 OIT runtime。
- 因为想快速看到 lit 效果，继承 URP Lit 全套结构。
- 因为旧项目已有 weighted OIT，直接复制旧 pass / global 名。
- 因为 AOV 输出需要材质语义，重新把材质 UI 变成 shader 结构控制器。
