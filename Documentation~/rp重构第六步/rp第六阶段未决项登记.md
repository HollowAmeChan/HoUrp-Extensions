# RP 第六阶段未决项登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| 完整 Diffusion Profile asset | 不做 | Profile registry 阶段 |
| HDRP Burley compute | 不做完整 compute | SSS quality 阶段 |
| Transmission gather / blur | 不做 | Transmission 阶段 |
| Half / quarter resolution | 不做 | Filter / SSS 性能阶段 |
| Temporal / bilateral filter backend | 不抽象 backend | Filter backend 阶段 |
| 旧材质 SSS 属性迁移 | 不做 | 新材质系统 / migration tool |
| Diffuse lighting source | 使用 `Aov.SssSource` | Lighting source 阶段 |
| Transparent SSS | 不做 | Transparent / OIT 阶段 |
