# 其人之道双敌人与复合意图修复

2026-09-23，OTTO v0.3.2，游戏 v0.107.1，BaseLib 3.4.7。

## 原因与修正

旧版只为七个招式提供强化/格挡数值。目标筛选遇到未适配的 BuffIntent 就拒绝整个目标，即使该敌人还带有攻击意图。不是按怪物数量限制；用户的双敌人情形需要逐个目标判定。

- 保持单敌人指向，只复制选中的敌人，不要求其他敌人的意图也被支持。
- 补齐本机原版常规攻击＋力量、攻击＋活力、攻击＋格挡，以及常见纯强化、格挡＋强化。
- 力量、活力、仪式、荆棘、无实体分别结算；不能把所有强化都当成力量。
- 用户确认：攻击附带的无法转换的专属强化忽略，攻击仍执行。瀑布巨人的蒸汽爆炸不会挂到玩家身上；感染棱体的活性火花跳过，攻击及格挡保留。
- 先快照被选招式的基础伤害、段数和增益，再按 OTTO 自身属性反打，最后将格挡/增益给自己。不调用原版敌人行动，不推进敌人状态机。
- 含召唤、减益、治疗等其他意图仍不可选；未适配的纯强化或纯格挡仍不可选，避免无效果地消耗卡牌。其他 Mod 的未知攻击＋强化按同一降级规则仅复制攻击。
- 中英文卡面说明与本地化生成脚本已同步；代码保留中文解释。

## 真实引擎验证

`OTTO_COPY_FIX_OK: 593 assertions passed`

65 个真实原版招式在双敌人隔离战斗中通过正式目标判定、扣费和 `CardModel.OnPlayWrapper` 出牌验证。

- 两只怪分别显示攻击和强化，两者可分别选择；选攻击怪只打它，选强化怪只给 OTTO 增益。
- 另一只怪显示召唤意图，不阻止选择支持的目标；若所有目标均不支持则不可打出。
- 多段伤害次数和伤害归属正确，未选敌人生命/状态不变。
- 反打使用 OTTO 原有力量，新复制力量在攻击后取得，不复制敌人已有力量。
- 活力与力量区分，普通版消耗2费、升级版1费，完成后进入弃牌堆。
- 敌人不获得复制的强化、格挡，其 NextMove 和状态机日志不被推进。
- 首领专属强化未挂到玩家，不触发玩家死亡时强转怪物模型或战斗无法结束的问题。

验收是实际游戏引擎中的内存战斗，不进入地图、不保存用户进度；不声称完成 GUI 整局或真实多人联机验证。蟹钳的掉血钩子依赖首领背景，隔离测试使用格挡承伤并核对实际格挡损失与 OTTO 获得的18格挡，不替换原版方法。

正式Mod与验收插件均编译成功，无编译错误。构建环境获取 NuGet 安全审计数据失败产生 NU1900 警告，不影响编译和这次引擎验证。无界面测试缺少屏幕摇晃目标及退出时资源清理提示与断言无关。

验收源码：`tools/intent_copy_probe.cs.txt`，不编入 Mod 或资源包。临时插件在测试进程退出后已自动移除。

## 通过验证的原版招式

- `GremlinMerc/HEHE_MOVE`
- `CeremonialBeast/PLOW_MOVE`
- `CeremonialBeast/CRUSH_MOVE`
- `CubexConstruct/REPEATER_BLAST_MOVE`
- `CubexConstruct/REPEATER_BLAST_MOVE_2`
- `DecimillipedeSegmentFront/BULK_MOVE`
- `Fogmog/SWIPE_MOVE`
- `Fogmog/SWIPE_RANDOM_MOVE`
- `GlobeHead/GALVANIC_BURST`
- `Myte/SUCK_MOVE`
- `SkulkingColony/INERTIA_MOVE`
- `SludgeSpinner/RAGE_MOVE`
- `SnappingJaxfruit/ENERGY_ORB_MOVE`
- `TerrorEel/THRASH_MOVE`
- `TheAdversaryMkOne/BARRAGE_MOVE`
- `TheAdversaryMkTwo/BARRAGE_MOVE`
- `TheAdversaryMkThree/BARRAGE_MOVE`
- `LivingShield/SMASH_MOVE`
- `SlumberingBeetle/ROLL_OUT_MOVE`
- `WaterfallGiant/RAM_MOVE`
- `WaterfallGiant/PRESSURE_GUN_MOVE`
- `WaterfallGiant/PRESSURE_UP_MOVE`
- `InfestedPrism/PULSATE_MOVE`
- `Aeonglass/EBB_MOVE`
- `AxeRubyRaider/SWING_1`
- `AxeRubyRaider/SWING_2`
- `BowlbugEgg/BITE_MOVE`
- `Crusher/GUARDED_STRIKE_MOVE`
- `InfestedPrism/RADIATE_MOVE`
- `LagavulinMatriarch/SLASH2_MOVE`
- `MagiKnight/POWER_SHIELD_MOVE`
- `Nibbit/SLICE_MOVE`
- `SlitheringStrangler/THWACK`
- `TheObscura/HARDENING_STRIKE_MOVE`
- `BowlbugNectar/BUFF_MOVE`
- `BruteRubyRaider/ROAR_MOVE`
- `BygoneEffigy/WAKE_MOVE`
- `Crusher/ADAPT_MOVE`
- `CubexConstruct/CHARGE_UP_MOVE`
- `Exoskeleton/ENRAGE_MOVE`
- `FakeMerchantMonster/ENRAGE_MOVE`
- `Queen/ENRAGE_MOVE`
- `FlailKnight/WAR_CHANT`
- `FrogKnight/FOR_THE_QUEEN`
- `FuzzyWurmCrawler/INHALE`
- `KinFollower/POWER_DANCE_MOVE`
- `KinPriest/RITUAL_MOVE`
- `LouseProgenitor/CURL_AND_GROW_MOVE`
- `MechaKnight/WINDUP_MOVE`
- `Nibbit/HISS_MOVE`
- `Ovicopter/NUTRITIONAL_PASTE_MOVE`
- `PhantasmalGardener/ENLARGE_MOVE`
- `Rocket/CHARGE_UP_MOVE`
- `ScrollOfBiting/MORE_TEETH`
- `Seapunk/BUBBLE_BURP_MOVE`
- `SewerClam/PRESSURIZE_MOVE`
- `SoulFysh/FADE_MOVE`
- `SpinyToad/PROTRUDING_SPIKES_MOVE`
- `TheInsatiable/SALIVATE_MOVE`
- `TheObscura/SAIL_MOVE`
- `Toadpole/SPIKEN_MOVE`
- `TurretOperator/RELOAD_MOVE`
- `Vantom/PREPARE_MOVE`
- `CalcifiedCultist/INCANTATION_MOVE`
- `DampCultist/INCANTATION_MOVE`
