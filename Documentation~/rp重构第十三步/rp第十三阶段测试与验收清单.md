# rp绗崄涓夐樁娈垫祴璇曚笌楠屾敹娓呭崟

## 鑷姩娴嬭瘯

### 璧勬簮濂戠害娴嬭瘯

鏂板寤鸿锛?
```text
Tests/Runtime/HoUrpShadowCastResourceContractTests.cs
```

瑕嗙洊锛?
- `ShadowCast.Atlas` 宸叉敞鍐屻€?- `ShadowCast.SecondDirectionalAtlas` 宸叉敞鍐屻€?- `ShadowCast.LightData` 宸叉敞鍐屻€?- `ShadowCast.LightAttenuation` 宸叉敞鍐屻€?- `ShadowCast.SliceData` 宸叉敞鍐屻€?- `ShadowCast.WorldToShadow` 宸叉敞鍐屻€?- 璧勬簮 id 鍞竴銆?- Debug name 鍙拷韪€?- ShadowCast 璧勬簮涓嶅鐢?OIT/SSS/Post id銆?
### Atlas Pack 娴嬭瘯

鏂板寤鸿锛?
```text
Tests/Runtime/HoUrpShadowCastAtlasPackerTests.cs
```

瑕嗙洊锛?
- 鍗?slice 鍒嗛厤鎴愬姛銆?- 鎹㈣鍒嗛厤鎴愬姛銆?- 瓒呭閲忚繑鍥?false銆?- point light 鍏?faces 浠讳竴澶辫触鏃舵暣鍏夊洖婊氥€?- packing rect 涓嶉噸鍙犮€?
### Shader ABI 娴嬭瘯

鏂板寤鸿锛?
```text
Tests/Runtime/HoUrpShadowCastShaderAbiTests.cs
```

瑕嗙洊锛?
- `HoUrpShadowCastSampling.hlsl` 瀛樺湪銆?- 鍖呭惈 `HoUrpSampleShadowCastAttenuation`銆?- 鍖呭惈 `_HoUrpShadowCastAtlas`銆?- 鍖呭惈 `_HoUrpShadowCastActive`銆?- 鍖呭惈 `_HoUrpShadowCastSecondDirectionalAtlas`銆?- 鍖呭惈 `_HoUrpShadowCastSecondDirectionalParams`銆?- 涓嶅寘鍚棫 `_HoShadowCast` 浣滀负鏂?ABI銆?
### Generated/Debug Shader 娴嬭瘯

瑕嗙洊锛?
- Debug/generated lit shader 鍖呭惈 `UniversalForward` pass銆?- 鍖呭惈 `OIT` pass銆?- 鍖呭惈 `ShadowCaster` pass銆?- Forward path 璋冪敤 ShadowCast receiver sampling銆?- OIT path 璋冪敤 ShadowCast receiver sampling銆?- Receiver sampling 瑕嗙洊 spot/point punctual attenuation銆?- Receiver sampling 瑕嗙洊 second directional attenuation銆?- OIT path 娌℃湁閫€鍖栨垚绾?albedo 杈撳嚭銆?
### Feature/Constants 娴嬭瘯

瑕嗙洊锛?
- `HoShadowCastShaderConstants` 闆嗕腑瀹氫箟鎵€鏈?property id銆?- 瀛樺湪 reset helper銆?- reset helper 瑕嗙洊 active/count/strength/atlas銆?- reset helper 瑕嗙洊 second directional params/atlas銆?- Settings 榛樿鍊煎悎鐞嗐€?- Feature display name 涓?HoURP 鍛藉悕涓€鑷淬€?
## Unity 缂栬瘧楠屾敹

鍦?Unity 涓‘璁わ細

- 鏃?C# 缂栬瘧閿欒銆?- 鏃?shader 缂栬瘧閿欒銆?- 鏂?feature 鑳藉湪 Renderer Feature 闈㈡澘娣诲姞銆?- Feature 寮€鍚?鍏抽棴涓嶄細浜х敓 console error銆?
## 鎵嬪伐鍦烘櫙楠屾敹

### 鍦烘櫙閰嶇疆

鍒涘缓鏈€灏忓満鏅細

- Plane 浣滀负 receiver銆?- Opaque debug lit 鐗╀綋銆?- Transparent/OIT debug lit 鐗╀綋銆?- Directional Light銆?- Spot Light銆?- Point Light銆?- Extra Directional Light銆?- HoURP renderer 鍚敤 AOV銆丼SS銆丱IT銆丼hadowCast銆丳ost 涓殑甯哥敤缁勫悎銆?
### 鎶曞奖楠屾敹

- Opaque debug lit 鐗╀綋鑳戒骇鐢熸姇褰便€?- Spot light 鑳藉湪涓?atlas 涓骇鐢?1 涓?slice銆?- Point light 鑳藉湪涓?atlas 涓骇鐢?6 涓?faces銆?- Extra directional light 鑳藉湪 second directional atlas 涓骇鐢?cascades銆?- Transparent/OIT debug lit 鐗╀綋鐨勬姇褰辫涓虹鍚堝綋鍓嶇瓥鐣ワ細
  - 鑻ョ涓€鐗堝惎鐢?opaque-style shadow锛屽垯鑳戒互涓嶉€忔槑娣卞害鎶曞奖銆?  - 鑻ョ涓€鐗堢鐢ㄩ€忔槑鎶曞奖锛屽垯 UI/鏂囨。蹇呴』鏄庣‘銆?- 鍏抽棴 `ShadowCaster` pass 鐨?shader 涓嶅簲浜х敓鎶曞奖銆?
### 鍙楀奖楠屾敹

- Plane 鑳芥帴鏀?HoShadowCast attenuation銆?- Opaque debug lit 鐗╀綋鑳芥帴鏀?attenuation銆?- Transparent/OIT debug lit 鐗╀綋鑳芥帴鏀?attenuation銆?- Receiver attenuation 鍚屾椂鍝嶅簲 spot/point 涓?second directional銆?- Forward/OIT 鍙楀奖寮哄害鏂瑰悜涓€鑷淬€?
### 寮€鍏抽獙鏀?
- 鍏抽棴 ShadowCast feature 鍚庯紝receiver 绔嬪嵆鎭㈠鏃犺嚜瀹氫箟 shadow銆?- 鍏抽棴 light 鍚庯紝receiver 涓嶅彉榛戙€?- Scene View/Game View 寮€鍏充簰涓嶆薄鏌撱€?- 鍙嶅鍚仠 feature 涓嶅嚭鐜颁笂涓€甯ф畫褰便€?- 鍏抽棴 extra directional 鍚庯紝second directional atlas 涓嶆畫鐣欍€?
### 椤哄簭楠屾敹

- 寮€鍚?OIT 鍚庯紝閫忔槑鐗╀綋浠嶄繚鐣欏熀纭€鍏夌収銆?- 寮€鍚?SSS 鍚庯紝ShadowCast receiver 涓嶄涪澶便€?- 寮€鍚?ScreenPost/ImagePost 鍚庯紝鍙敼鍙樻渶缁堢敾闈紝涓嶅奖鍝?ShadowCast atlas銆?- AOV debug 涓嶆敼鍙樼敓浜ц緭鍑恒€?
## RenderDoc/Frame Debugger 楠屾敹

鍙€変絾寤鸿锛?
- 鑳界湅鍒颁富 ShadowCast atlas/depth pass銆?- 鑳界湅鍒?second directional atlas/depth pass銆?- ShadowCast pass render target 涓嶆槸 camera color銆?- Receiver draw 鍓?globals 宸插彂甯冦€?- OIT accumulation shader 缁戝畾 ShadowCast receiver 鏁版嵁銆?- Post pass 涓嶇粦瀹?ShadowCast atlas锛岄櫎闈?debug mode銆?
## 澶辫触鍒ゅ畾

浠ヤ笅浠讳竴鎯呭喌瑙嗕负绗崄涓夋鏈畬鎴愶細

- Feature 鍏抽棴鍚庝粛淇濈暀涓婁竴甯ч槾褰便€?- 涓?atlas 涓嶆敮鎸?spot/point packing銆?- Point light 鍙啓鍏ラ儴鍒?faces 浣嗕粛琚?receiver 閲囨牱銆?- Second directional atlas 鏃?debug 鎴栨棤 reset銆?- OIT 寮€鍚悗閫忔槑鐗╀綋鍏夌収鎴栬嚜瀹氫箟闃村奖娑堝け銆?- Shader 鍙湁 receiver sampling锛屾病鏈?`ShadowCaster` pass锛屽嵈琚璁や负鍙姇褰便€?- 鏂颁唬鐮侀粯璁や娇鐢ㄦ棫 `_HoShadowCast*` 浣滀负鍏叡 ABI銆?- ShadowCast pass 鍐欏叆鎴栬鍙?post chain ping-pong 璧勬簮銆?- AOV/SSS/Post 鍏抽棴椤哄簭鏀瑰彉 ShadowCast 鍩虹缁撴灉銆?
## 鍚庣画寤舵湡椤?
- alpha/cutout/dither 閫忔槑鎶曞奖绛栫暐銆?- 鏇翠紭 atlas packing銆?- 璺ㄥ抚 atlas cache銆?- PCSS/soft shadow 璐ㄩ噺璋冨弬銆?- CharacterSpecialization 闃村奖銆?- 涓庢潗璐?UI 鐨勫畬鏁村弬鏁版槧灏勩€?- legacy `_HoShadowCast*` bridge銆?

## ShadowCast 宸ヤ綔妯″紡涓?Inspector 楠屾敹琛ュ厖

- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowCast宸ヤ綔妯″紡涓庝娇鐢ㄦ柟娉?md` 宸插瓨鍦紝骞惰兘璇存槑鐢ㄦ埛宸ヤ綔娴佷笌 AI/寮€鍙戣€呯淮鎶ゅ簳绾裤€?- RendererFeature Inspector 涓荤晫闈笉鍐嶇洿鎺ュ睍寮€涓夌粍鍥哄畾鐏厜鏁扮粍銆?- 鎵嬪姩鐏厜鍒楄〃浣嶄簬楂樼骇鎶樺彔鍖猴紝骞舵槑纭鏄庨粯璁や娇鐢ㄨ嚜鍔ㄦ敹闆嗐€?- `杩愯鏃跺弬涓庣姸鎬乣 鑳芥樉绀哄弬涓庣伅鍏夈€佹潵婧愩€乻lice 鑼冨洿鍜岃烦杩囧師鍥犮€?- Atlas debug 鏄剧ず鐪熷疄 slice/block 杈圭晫锛屼笉鍐嶆樉绀哄浐瀹氬亣缃戞牸銆?- `HoURP/Generated/HoUrpShadowCastReceiverDebug` 鑳借鏉愯川閫夋嫨骞剁敤浜庨獙璇?receiver sampling銆?

