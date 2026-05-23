# rp閲嶆瀯绗崄涓夋鎵ц璁″垝

## 鐩爣

绗崄涓夋杩涘叆 HoShadowCast 瀛愮郴缁熻縼绉荤涓€闃舵锛氭妸鏃т粨搴撻噷宸茬粡鍏峰鐨?atlas packing銆佽仛鍏?鐐瑰厜鎶曞奖銆侀澶栨柟鍚戝厜 atlas銆乨ebug view 鍜?receiver sampling 鎷嗘垚鏂扮殑璧勬簮濂戠害銆丷enderGraph 鎵ц鑺傜偣銆丼hader 鎺ユ敹 ABI 鍜岄獙鏀惰矾寰勩€?
杩欎竴闃舵涓嶆槸閲嶅缓瀹屾暣鏉愯川绯荤粺锛屼篃涓嶆槸鎶婃棫瀹炵幇鏁存鎼洖銆傜洰鏍囨槸璁╂柊 HoURP 绠＄嚎鍏峰涓€鏉℃帴杩戞棫 ShadowCast 鑳藉姏涓嬮檺銆佷絾鍛藉悕鍜岃祫婧愯竟鐣岄噸鏂版暣鐞嗚繃鐨勮嚜瀹氫箟闃村奖閾捐矾锛?
- 鑳藉湪 RenderGraph 涓垎閰嶅苟鍙戝竷 HoURP 鑷湁 ShadowCast 璧勬簮銆?- 鑳界敤鏍囧噯 `ShadowCaster` pass 鍐欏叆 atlas slice銆?- 鑳?pack spot/point light slice锛岀偣鍏夋寜 6 faces 鍐欏叆涓?atlas銆?- 鑳戒负棰濆鏂瑰悜鍏夌敓鎴愮嫭绔?second directional atlas锛屽苟鏀寔 cascades銆?- 鑳藉湪鐢熸垚鏉愯川鐨?Forward/OIT 璺緞涓鍙栬嚜瀹氫箟闃村奖琛板噺銆?- 鑳藉湪 feature 鍏抽棴銆佹棤鍏夋簮銆佹棤鎺ユ敹鑰呫€丼cene/Game View 鍒囨崲鏃舵竻鐞嗗叏灞€鐘舵€併€?- 鑳界敤 Debug 杈撳嚭瀹氫綅 atlas銆乻econd directional atlas銆乴ight/slice 鏁版嵁銆乺eceiver attenuation 鍜岃祫婧愮敓鍛藉懆鏈熼棶棰樸€?
## 鏃у疄鐜拌兘鍔涘熀绾?
鏃?`lilToon-URP-Extensions` 涓?HoShadowCast 鍙互浣滀负琛屼负鍙傝€冿紝浣嗕笉鑳戒綔涓?ABI 鐩存帴缁ф壙銆傛棫瀹炵幇涓嶆槸鍗曞厜鍘熷瀷锛屽畠宸茬粡鍏峰锛?
- `MaxSpotLights = 4`銆乣MaxPointLights = 4`锛岀偣鍏夋渶澶?24 涓?faces銆?- 涓?atlas 浣跨敤绠€鍗?row packer 鍒嗛厤 spot/point slices銆?- second directional atlas 鏀寔鏈€澶?4 涓澶栨柟鍚戝厜锛屾瘡鍏夋渶澶?4 cascades銆?- debug mode 鏀寔涓?atlas 涓?second directional atlas銆?- sampling include 涓凡鏈?punctual attenuation銆乻econd directional attenuation銆乵anual PCF/PCSS 鍒嗘敮銆?
绗崄涓夋搴斾繚鐣欒繖浜涜兘鍔涘眰绾э紝浣嗕笉淇濈暀鏃у悕瀛楁薄鏌擄細

- 鏃?RenderFeature銆丆ontroller銆丷esource銆丼ampling include 鍙綔涓烘暟鎹祦鍙傝€冦€?- 鏂板叕鍏辫祫婧愬悕銆乻hader property銆乨ebug id 蹇呴』褰掑叆 HoURP 鍛藉悕绌洪棿銆?- 涓嶇洿鎺ユ毚闇叉棫 `_HoShadowCast*` 鍚嶇О浣滀负鏂板绾︼紱纭渶鍏煎鏃跺彟寮€ legacy bridge 鏂囨。鍜屽紑鍏炽€?
## 鍒嗘瀹炴柦

### 13.a 杈圭晫涓庢棫瀹炵幇瀹℃煡

杈撳嚭锛?
- `Documentation~/rp閲嶆瀯绗崄涓夋/rp绗崄涓夐樁娈靛疄鐜拌竟鐣屽鏌?md`
- `Documentation~/rp閲嶆瀯绗崄涓夋/rpHoShadowCast鏃у疄鐜版暟鎹祦瀹℃煡.md`

宸ヤ綔锛?
- 褰掓。鏃?`Runtime/ShadowCast` 鐨?atlas銆乻pot銆乸oint銆乻econd directional銆乨ebug銆乻ampling 鏁版嵁娴併€?- 鏍囨敞鈥滀繚鐣欐蹇碘€濃€滄敼鍚嶉噸寤衡€濃€滃垎灞傝縼绉烩€濃€滄殏缂撯€濄€?- 鏄庣‘绗崄涓夋涓嶅鐞?CharacterSpecialization銆丳lanar Reflection銆佸畬鏁存潗璐?UI銆侀€忔槑绮剧‘鎶曞奖銆?
### 13.b ShadowCast 璧勬簮濂戠害涓庡鍏夋簮甯冨眬

杈撳嚭锛?
- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowCast璧勬簮涓庡绾﹁鍒?md`
- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowCastAtlasPack涓庡鍏夋簮鎵ц瑙勫垝.md`

宸ヤ綔锛?
- 鍦?`HoUrpRenderGraphResourceIds` 澧炲姞 ShadowCast 璧勬簮鏃忋€?- 寤虹珛 `HoUrpShadowCastResourceDeclaration`锛岃礋璐ｄ富 atlas銆乻econd directional atlas銆乴ight data銆乻lice data銆亀orld-to-shadow 鏁版嵁鐨勫０鏄庡拰 debug 鍛藉悕銆?- 瑙勫垝涓?atlas row packing銆乻pot 鍗?slice銆乸oint 鍏?faces銆?- 瑙勫垝 second directional atlas 鐨?grid/cascade 甯冨眬銆?- 鏄庣‘鏃犲厜婧愭垨 feature 鍏抽棴鏃跺彂甯?inactive 鐘舵€侊紝鑰屼笉鏄暀涓嬩笂涓€甯у叏灞€绾圭悊銆?
### 13.c RenderGraph 鎵ц璺緞

杈撳嚭锛?
- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowCastRenderGraph鎵ц璁″垝.md`

宸ヤ綔锛?
- 鏂板缓 `Runtime/ShadowCast/HoShadowCastRendererFeature.cs`銆?- 鏂板缓 settings/constants/resources/packer 鐩稿叧鏂囦欢銆?- Pass 椤哄簭寤鸿锛?  1. Reset/Inactive銆?  2. Build punctual frame data銆?  3. Allocate main atlas銆?  4. Draw spot/point caster slices銆?  5. Build second directional frame data銆?  6. Allocate second directional atlas銆?  7. Draw second directional cascade slices銆?  8. Publish receiver globals銆?  9. Debug atlas view銆?
绗竴涓増鏈笉鍐嶅彧鍋氬崟涓诲厜銆傚繀椤昏鐩栨棫瀹炵幇宸叉湁鐨勪笁绫昏緭鍏ワ細

- spot lights锛氭瘡鍏?1 slice锛岃繘鍏ヤ富 atlas銆?- point lights锛氭瘡鍏?6 slices锛岃繘鍏ヤ富 atlas銆?- second directional lights锛氭瘡鍏?N cascades锛岃繘鍏?second directional atlas銆?
鍚庣疆椤瑰寘鎷洿浼?packing銆佽法甯?atlas 缂撳瓨銆侀€忔槑 alpha/dither 绮剧‘鎶曞奖鍜岄珮绾ц蒋闃村奖璋冨弬銆?
### 13.d Shader Receiver ABI 涓庣敓鎴愭潗璐ㄦ帴鍏?
杈撳嚭锛?
- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowReceiverShaderABI涓庢潗璐ㄦ帴鍏ュ鏌?md`

宸ヤ綔锛?
- 鏂板缓 `Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl`銆?- 瑙勫畾鎺ユ敹鍑芥暟锛屼緥濡?`HoUrpSampleShadowCastAttenuation(positionWS, normalWS)`銆?- sampling 绗竴鐗堝簲鍖呭惈锛?  - punctual attenuation锛歴pot/point 褰卞搷鑼冨洿銆乻pot cone銆乸oint face selection銆?  - second directional attenuation锛氶澶栨柟鍚戝厜 cascade selection銆?  - PCF/PCSS锛氬厛杩佺Щ缁撴瀯鍜屽弬鏁颁綅锛岃川閲忎紭鍖栧彲鍚庣画璋冨弬銆?- 鏇存柊 debug/generated lit shader锛?  - `UniversalForward` 鎺ユ敹 HoShadowCast attenuation銆?  - `OIT` 鎺ユ敹鍚屼竴 attenuation锛岄伩鍏?OIT 寮€鍚悗鍏夌収/闃村奖閫€鍖栨垚绾壊銆?  - `ShadowCaster` pass 鐙珛瀛樺湪锛岀敤浜庝骇鐢熸姇褰便€?
### 13.e Debug 涓庣姸鎬佹竻鐞?
杈撳嚭锛?
- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowCastDebug涓庣姸鎬佹竻鐞嗗鏌?md`

宸ヤ綔锛?
- Debug mode 鑷冲皯瑕嗙洊 `Atlas` 涓?`SecondDirectionalAtlas`銆?- Receiver attenuation debug 鍙綔涓虹涓夐」锛岃嫢瀹炵幇鎴愭湰浣庡垯鍚岄樁娈靛畬鎴愩€?- 鎵€鏈夊叏灞€ shader id 闆嗕腑鍦?constants 鏂囦欢銆?- feature disable銆乧amera type mismatch銆佹棤鏈夋晥 light銆乤tlas allocation failure 閮藉繀椤?reset globals銆?- Debug 杈撳嚭涓嶈兘鎴愪负鏉愯川 ABI锛涘彧鐢ㄤ簬鎺掗敊銆?
### 13.f 涓?OIT/SSS/Post/AOV 椤哄簭鍥炲綊

杈撳嚭锛?
- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowCast涓嶰itSssPost椤哄簭鍥炲綊瀹℃煡.md`

宸ヤ綔锛?
- 纭 ShadowCast pass 涓嶈鍐?camera color銆?- ShadowCast 搴斿厛浜?transparent/OIT receiver 璺緞鍙戝竷 receiver globals銆?- 涓?atlas 涓?second directional atlas 涓嶅簲杩涘叆 post chain ping-pong銆?- SSS/AOV/Post 涓嶅簲渚濊禆 ShadowCast atlas 鐨?lifetime锛岄櫎闈炴樉寮忓０鏄庤祫婧愪緷璧栥€?- 鍏抽棴 ShadowCast 鍚庯紝OIT銆丼SS銆丼creenPost銆両magePost 浠嶅簲淇濇寔绗崄涓€銆佸崄浜屾琛屼负銆?
### 13.g 娴嬭瘯涓庨獙鏀?
杈撳嚭锛?
- `Documentation~/rp閲嶆瀯绗崄涓夋/rp绗崄涓夐樁娈垫祴璇曚笌楠屾敹娓呭崟.md`

宸ヤ綔锛?
- 澧炲姞璧勬簮濂戠害娴嬭瘯銆?- 澧炲姞 atlas packer 娴嬭瘯銆?- 澧炲姞 shader ABI 娴嬭瘯銆?- 澧炲姞 generated/debug shader pass 娴嬭瘯銆?- 澧炲姞 spot/point/second directional Unity 鎵嬪伐楠屾敹鍦烘櫙璇存槑銆?- 澧炲姞 RenderDoc 鍙€夐獙鏀堕」銆?
## 寤鸿鏂囦欢缁撴瀯

```text
Runtime/
  ShadowCast/
    HoShadowCastRendererFeature.cs
    HoShadowCastSettings.cs
    HoShadowCastShaderConstants.cs
    HoShadowCastResources.cs
    HoShadowCastAtlasPacker.cs
  RenderGraph/
    HoUrpShadowCastResourceDeclaration.cs
  Shaders/
    ShaderLibrary/
      HoUrpShadowCastSampling.hlsl
    Hidden/HoURP/ShadowCast/
      Debug.shader
```

娴嬭瘯寤鸿锛?
```text
Tests/
  Runtime/
    HoUrpShadowCastResourceContractTests.cs
    HoUrpShadowCastAtlasPackerTests.cs
    HoUrpShadowCastShaderAbiTests.cs
```

## 鏂板懡鍚嶅缓璁?
璧勬簮 id锛?
- `ShadowCast.Atlas`
- `ShadowCast.AtlasSize`
- `ShadowCast.LightData`
- `ShadowCast.LightAttenuation`
- `ShadowCast.LightColor`
- `ShadowCast.SliceData`
- `ShadowCast.WorldToShadow`
- `ShadowCast.SecondDirectionalAtlas`
- `ShadowCast.SecondDirectionalAtlasSize`
- `ShadowCast.SecondDirectionalLightData`
- `ShadowCast.SecondDirectionalSliceData`
- `ShadowCast.SecondDirectionalWorldToShadow`
- `ShadowCast.DebugAtlas`
- `ShadowCast.DebugSecondDirectionalAtlas`
- `ShadowCast.DebugAttenuation`

Shader property锛?
- `_HoUrpShadowCastAtlas`
- `_HoUrpShadowCastAtlasSize`
- `_HoUrpShadowCastActive`
- `_HoUrpShadowCastLightCount`
- `_HoUrpShadowCastSliceCount`
- `_HoUrpShadowCastWorldToShadow`
- `_HoUrpShadowCastLightData`
- `_HoUrpShadowCastLightAttenuation`
- `_HoUrpShadowCastLightColor`
- `_HoUrpShadowCastSliceData`
- `_HoUrpShadowCastPcssParams`
- `_HoUrpShadowCastPcssParams2`
- `_HoUrpShadowCastSecondDirectionalAtlas`
- `_HoUrpShadowCastSecondDirectionalParams`
- `_HoUrpShadowCastSecondDirectionalAtlasSize`
- `_HoUrpShadowCastSecondDirectionalWorldToShadow`
- `_HoUrpShadowCastSecondDirectionalLightData`
- `_HoUrpShadowCastSecondDirectionalSliceData`
- `_HoUrpShadowCastSecondDirectionalPcssParams`
- `_HoUrpShadowReceiverStrength`

## 瀹屾垚瀹氫箟

绗崄涓夋瀹屾垚鏃讹紝搴旀弧瓒筹細

- HoShadowCast feature 鍙紑鍏筹紝涓斾笉浼氭薄鏌撳叧闂悗鐨勫悗缁抚銆?- 涓?atlas 鑳?pack spot/point slices锛岀偣鍏?6 faces 鐨?slice 鏁版嵁鍙?debug銆?- second directional atlas 鑳芥樉绀洪澶栨柟鍚戝厜 cascades銆?- 鑷冲皯涓€涓?debug/generated lit 鏉愯川鏃㈣兘浜х敓 shadow caster depth锛屼篃鑳芥帴鏀?HoURP ShadowCast attenuation銆?- OIT 鎵撳紑鍚庯紝閫忔槑瀵硅薄浠嶈兘澶嶇敤鍚屼竴 receiver attenuation锛屼笉閫€鍥炵函鑹插悎鎴愩€?- AOV/SSS/Post 涓嶅洜 ShadowCast 璧勬簮寮曞叆浜х敓椤哄簭鎴栫敓鍛藉懆鏈熷洖褰掋€?- 娴嬭瘯鍜屾枃妗ｉ兘鑳借鏄庡綋鍓嶉€忔槑鎶曞奖闄愬埗锛屼互鍙婂悗缁?alpha/dither shadow 鐨勫叆鍙ｃ€?

## 13.h ShadowCast 宸ヤ綔妯″紡涓庣淮鎶ゅ簳绾?
杈撳嚭锛?- `Documentation~/rp閲嶆瀯绗崄涓夋/rpShadowCast宸ヤ綔妯″紡涓庝娇鐢ㄦ柟娉?md`

宸ヤ綔锛?- 璁板綍 ShadowCast 涓?URP 涓诲厜/澶╁厜鐨勮竟鐣岋細HoCast 鏄嫭绔?receiver term锛屼笉鍐欏叆 URP main light shadow銆?- 璁板綍 RendererFeature Inspector 浣跨敤鏂瑰紡锛氶粯璁よ嚜鍔ㄦ敹闆嗗彲瑙佺伅锛屾墜鍔ㄧ伅鍏夊垪琛ㄥ彧浣滀负楂樼骇琛ュ厖鍏ュ彛銆?- 璁板綍 atlas/debug 鏄剧ず绾﹀畾锛氫富 atlas 鏄剧ず鐪熷疄 slice锛宻econd directional atlas 鎸夊厜婧?block 鏄剧ず cascade銆?- 璁板綍鏉愯川娑堣垂鏂瑰紡锛氭寮忔潗璐ㄩ€氳繃 `HoUrpShadowCastSampling.hlsl` 璋冪敤 receiver sampling锛屼笉鐩存帴璇?atlas銆?- 璁板綍 AI/寮€鍙戣€呭悗缁慨鏀瑰簳绾匡紝閬垮厤鍥為€€鍒版棫 ABI銆佸亣缃戞牸銆侀殣寮忓弬涓庢垨蹇呴』鎷栫伅鐨勫伐浣滄祦銆?

