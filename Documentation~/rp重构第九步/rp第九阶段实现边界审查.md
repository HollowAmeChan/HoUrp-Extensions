# RP 第九阶段实现边界审查

第九阶段只处理 AOV 生命周期整理和 RSUV 静态对象语义前移。

## 做

- 新增 AOV 生命周期审查文档。
- 新增 RSUV 静态语义前移审查文档。
- 明确 `Object.Custom0-7`、`Object.Id`、`Object.GroupId`、`Object.Flags` 属于 renderer 静态语义。
- 明确 v1 RSUV 默认采用 HoAOV-first Compact：对象区域和 capability flags 优先，`Object.Id` / `Object.GroupId` 激进收紧，authoring / Inspector 直接限制到 compact 范围。
- 新增 `RendererStaticSemanticValue` pack/unpack helper。
- 可选新增 `RendererStaticSemanticBindingMode`。
- 让 `ObjectSemanticAuthoring` 支持 RSUV-preferred / MPB-only 的写入策略。
- 让 `AovOutputFallback.shader` 读取 `unity_RendererUserValue` 并回退 `_HoUrp*` MPB 属性。
- 补 pack/unpack 和 authoring 一致性测试。
- 更新第九阶段验收清单。

## 不做

- 不引入全局 active group 列表。
- 不引入 priority / hierarchy distance resolver。
- 不引入全局对象分组 resolver。
- 不把 RSUV 作为唯一对象语义 ABI。
- 不把 material class、SSS profile、thickness、curvature、SSS source 塞进第一版 RSUV。
- 不为了保留 8-bit group/id 而牺牲 HoAOV 已验证对象 capability flags。
- 不静默截断 compact RSUV 放不下的 `Object.Id` / `Object.GroupId`；authoring UI 不提供越界输入，底层 packer 拒绝异常越界值。
- 不新增 AOV MRT 资源。
- 不新增承载 `ObjectFeatureFlags.bit8-12` 的 AOV 资源；第九阶段仅让 RSUV/authoring 先携带 high flags。
- 不改 `SubsurfaceScattering` 的滤波和 composite 逻辑。
- 不改 `SemanticPostProcess` 的 rule/effect 范围。
- 不做完整 Debug Framework、Debug HUD、overlay、capture。
- 不迁移 CharacterSpecialization。
- 不接 lilToon / lilPBR shader 或 inspector。

## 成功标准

- AOV 生命周期表能覆盖当前所有 `Aov.*`、`Sss.*`、`SemanticPost.*` 资源。
- 当前 object semantic 的来源层级清楚：RSUV fast path 或 MPB fallback。
- RSUV v1 pack/unpack helper 有测试，不依赖全局分组 resolver。
- v1 compact pack/unpack 覆盖 `ObjectId 0..15`、`GroupId 0..7`、capability flags 和底层越界拒绝。
- `Object.Custom0-7` 与 feature flags 都允许在 Inspector 中全部关闭。
- MPB-only 与 RSUV-preferred 两条路径能产生一致的 AOV object semantic 输出。
- 禁用 authoring 后不会残留 renderer user value。
- 现有 AOV Debug 下游按 `Aov.MaskId.a` low byte 消费：bit0 为空洞，bit1 是 `PostReceiver`，bit2-7 是 low feature flags。
- 新实现没有引入 `_HoAov*` / `_lilHoAov*` 公共 ABI。

## 边界原则

RSUV 是 renderer 静态语义输入路径，不是资源系统。

```text
RSUV:
  per-renderer input

AOV:
  per-camera resource output

Debug:
  registered view over resource / semantic
```

三者不能混用。AOV consumer 只能消费 `Aov.*` 或派生资源，不能直接依赖某个 renderer 是否使用 RSUV。

因此 debug view 也不能直接读取 `unity_RendererUserValue`。当前 debug view 只验证已经落到 `Aov.MaskId.a` 的 low 8 位；`ObjectFeatureFlags.bit8-12` 在第九阶段属于已打包但未资源化的静态输入。

## 与后续阶段关系

第九阶段完成后，后续有两条自然路线：

| 后续方向 | 前提 |
| --- | --- |
| Debug Framework | 能读取 AOV 生命周期表，解释 debug view 来源 |
| 材质系统重构 | 能按 `Material.*` 和 `Shading.*` 生命周期接入 producer |

如果第九步跳过生命周期表直接做 Debug Framework，debug 只能看到最终资源，无法解释来源。

如果第九步把 RSUV 做成唯一路径，后续材质系统和通用 renderer 类型会被绑死。

## 风险

- 因为已有参考实现可用就复制全局分组 resolver。
- 为了快速验证 shader，忽略 RSUV 清理。
- binding mode 切换时 MPB 和 RSUV 同时存在，shader 优先级导致用户误判。
- 测试只测 pack/unpack，不测 `ReceivesSemanticPost` 与 feature flags bit1 的一致性。
- 测试只测 pack/unpack，不测 v1 compact 越界拒绝和 authoring clamp。
- 文档里把 `partId` 写成新长期 ABI；第九步只能临时映射为 `Object.Id`。
