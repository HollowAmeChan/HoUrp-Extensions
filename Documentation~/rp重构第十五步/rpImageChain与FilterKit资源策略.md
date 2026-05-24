# ImageChain 与 FilterKit 资源策略

> 本文限定纯图像后处理和 FilterKit 的关系。FilterKit 只提供可复用 shader / include，ImageChain 负责线性 image pass 的 read/write 交换，具体 effect 负责显式声明额外 transient。

## 1. 职责边界

ImageChain 负责：

- 当前图像 read / write 交换。
- WorkA / WorkB frame transient 生命周期。
- OriginalSource copy 的显式需求。
- 线性 image pass 的顺序。

FilterKit 负责：

- 通用 blur / pyramid / Kuwahara / RGB blur 等 shader 文件。
- HLSL 采样、权重、gate、kernel 函数。
- pass id / shader id helper。

FilterKit 不负责：

- 创建 ImageChain。
- 创建 history。
- 创建 pyramid resources。
- 自动调度多个 pass。
- 判断 effect 应该运行在哪里。

## 2. ImageChain 第一版执行模型

```text
Begin(cameraColorCopy)
  Current = WorkA
  Next = WorkB

For each image pass:
  Record(Current -> Next)
  Swap()

End()
  Copy Current -> CameraColor
```

要求：

- WorkA / WorkB 是 frame transient。
- 线性 pass 数量增加时，不增加同尺寸长期 RT 数量。
- 每个 pass 显式知道自己的 source / destination。

验收：

- RenderGraph viewer / Frame Debugger 能看到每个 image pass 的 source/destination。
- 新增一个 single-pass effect 不新增长期 RT 字段。

## 3. 使用 FilterKit 的方式

普通 image blur：

```text
ImageChain.Current
  -> HoUrpFilterBlur.shader Pass 1
  -> ImageChain.Next
```

RGBBlurV2：

```text
ImageChain.Current
  -> downscale transient A
  -> blur transient B
  -> blur transient A
  -> final RGB composite to ImageChain.Next
```

Glow / Bloom：

```text
ImageChain.Current
  -> pyramid level 0..N
  -> upsample chain
  -> composite to ImageChain.Next
```

Kuwahara：

```text
ImageChain.Current
  -> HoUrpImageKuwahara.shader
  -> ImageChain.Next
```

要求：

- effect 显式创建额外 transient。
- effect 显式绑定 FilterKit shader。
- effect 不通过 FilterKit 请求资源。

## 4. 第一批迁移顺序

建议：

1. Simple blur：验证 `HoUrpFilterBlur.shader`。
2. RGBBlurV2：验证 downscale + ping-pong + final composite。
3. Glow：验证 pyramid resource 命名和释放。
4. Kuwahara：验证单 pass 高采样成本 debug。
5. IrisBlur：等 blur / RGBBlur 资源策略稳定后迁移。

## 5. 资源命名

建议命名：

```text
Image.WorkA
Image.WorkB
Image.OriginalSource
Image.Effect.<EffectName>.TempA
Image.Effect.<EffectName>.TempB
Image.Effect.<EffectName>.Pyramid0
Image.Effect.<EffectName>.Pyramid1
Image.History.<EffectName>
```

要求：

- `Image.WorkA/B` 不注册为长期公共 semantic resource。
- Effect transient 只在当前 effect 或当前 frame 内可见。
- History 必须写 reset 条件。

## 6. Debug 要求

ImageChain debug 至少能看：

- current read texture 名称。
- current write texture 名称。
- 当前 effect 名称。
- 是否使用 original source。
- 是否创建额外 transient。

FilterKit shader debug 至少能看：

- source。
- weight/gate。
- result。

## 7. 禁止项

- 每个 layer 私有持有全分辨率 `RTHandle` 字段。
- FilterKit helper 内部创建纹理。
- 用 `SetGlobalTexture` 作为长期 effect 间数据链。
- 多 pass effect 不声明自己的 transient。
- 纯 image effect 偷读 AOV / SSS / OIT。

## 8. 完成条件

- 一个 simple blur 能走 ImageChain + FilterKit。
- 一个 multi-pass effect 有显式 transient，不污染长期资源。
- Debug 能说明当前画面来自哪个 pass。
- ImageChain 和 FilterKit 互相独立，调用链可读。
