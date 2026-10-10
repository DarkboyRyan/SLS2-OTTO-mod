"""补充061–086卡面和状态文本；保留此前卡牌的翻译与排序。"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / 'OTTO/localization'
# 每项依次为ID、中文名、英文名、中文效果、英文效果；升级差异通过引擎动态变量显示。
CARDS = [
('MO_MO_MI_NO','么么哒米诺','Mo Mo Mi No','若未处于古神形态，获得{Block:diff()}点格挡与{Ancient:diff()}点古神值；否则抽{Cards:diff()}张牌。','Outside Ancient Form, gain {Block:diff()} Block and {Ancient:diff()} Ancient Meter. Otherwise, draw {Cards:diff()} cards.'),
('AO_LI_AN_FEI','奥利安费','Ao Li An Fei','造成{Damage:diff()}点伤害。若未处于古神形态，获得{Ancient:diff()}点古神值；否则在攻击前施加2层易伤。','Deal {Damage:diff()} damage. Outside Ancient Form, gain {Ancient:diff()} Ancient Meter. In Ancient Form, apply 2 Vulnerable before attacking instead.'),
('GONG_GONG','公公又式式','Gong Gong Again','若炫狗未在场，召唤炫狗。指挥它对目标行动{Repeat:diff()}次。每次击杀额外行动1次，目标死亡后随机选择新目标。炫狗入狱时无法打出。','Summon Xuan Dog if absent. It acts {Repeat:diff()} times against the target. Each kill adds one action; choose a random new target after a kill. Unplayable while Xuan Dog is imprisoned.'),
('LAW_ABIDING','主播是不会干违法的事的','Law Abiding Streamer','召唤山泥若。本场战斗第一次将其送入监狱时，改为留在场上并立即行动3次。重复打出不刷新机会。','Summon Shan Ni Ruo. The first time it would be imprisoned this combat, prevent it and have it act 3 times. Playing this again does not reset the protection.'),
('KFC','肯德基','KFC','需要山泥若在场。直到你的下回合开始，它的治疗改为对所有敌人造成治疗量{Multiplier:diff()}倍的伤害。立即行动1次。满血仍按正常治疗量计算。','Requires Shan Ni Ruo. Until your next turn, its healing instead deals {Multiplier:diff()} times the normal healing amount to ALL enemies. It acts immediately. Works at full HP.'),
('BIG_BLADE','她的大刀我很怕！','Fear Her Great Blade','召唤炫狗。获得{Block:diff()}点格挡。直到你的下回合开始，炫狗在场时，每段消耗格挡且完全挡住的敌方攻击，受到炫狗伤害3倍的反击。','Summon Xuan Dog. Gain {Block:diff()} Block. Until your next turn, while it is present, each enemy attack hit fully blocked by spending Block triggers a counter for 3 times its damage.'),
('WAVE_CRASH','兵线进塔了','Wave Crashes the Tower','对所有敌人造成{Damage:diff()}点伤害。火星包在场时，每有1点正敏捷，额外攻击1次。','Deal {Damage:diff()} damage to ALL enemies. While Huo Xing Bao is present, repeat once for each point of positive Dexterity.'),
('I_AM_CHINESE','I am Chinese','I am Chinese','召唤火星包。选择它本场战斗的行动模式：获得双倍力量、不获得敏捷；或获得双倍敏捷、不获得力量。模式可切换。{IfUpgraded:show:随后立即行动1次。|}','Summon Huo Xing Bao. Choose its combat mode: double Strength and no Dexterity, or double Dexterity and no Strength. Can be switched.{IfUpgraded:show: It then acts immediately.|}'),
('MARS_STRENGTH_CHOICE','力量模式','Strength Mode','火星包只获得双倍正常数值的力量，不获得敏捷。','Huo Xing Bao grants twice its normal Strength and no Dexterity.'),
('MARS_DEXTERITY_CHOICE','敏捷模式','Dexterity Mode','火星包只获得双倍正常数值的敏捷，不获得力量。','Huo Xing Bao grants twice its normal Dexterity and no Strength.'),
('KOREAN_HAND','韩国的虾米诺手啊','Korean Hand','造成{Damage:diff()}点伤害。若击杀目标，获得50点古神值，将本牌返回手牌，本回合费用为0。','Deal {Damage:diff()} damage. If this kills the target, gain 50 Ancient Meter and return this card to your hand. It costs 0 this turn.'),
('OLD_REASON','说的老理','Old Reason','若未处于古神形态，当前古神值翻倍；否则获得3点能量并抽3张牌。','Outside Ancient Form, double your current Ancient Meter. Otherwise, gain 3 Energy and draw 3 cards.'),
('GIRL_BAND_FAN','少女乐队痴','Girl Band Fan','将6位少女乐队成员的牌各1张加入手牌{IfUpgraded:show:并升级|}。本回合它们的费用为0。','Add one card for each of the six Girl Band members to your hand{IfUpgraded:show: and Upgrade them|}. They cost 0 this turn.'),
('ALL_IN','all in！all in!','all in! all in!','按顺序免费自动打出抽牌堆中的全部古神牌，目标随机。不包含同名牌。','Automatically play all Ancient Form cards in your draw pile in order for free, choosing random targets. Excludes all copies of this card.'),
('AMINOS','阿米诺斯','Aminos','本场战斗，古神形态额外持续1轮。若已在形态中，立即延长1轮。可叠加。','Ancient Form lasts 1 additional round this combat. If already in the form, extend it by 1 round immediately. Stacks.'),
('LAUGHTER_CARD','电棍笑传','OTTO Laughter','每次手动从手牌边缘触发易拉罐时，免费额外打出该牌1次。额外打出不触发易拉罐。不可叠加。','Whenever a manual play from a hand edge triggers Can, play an extra copy for free. The extra play does not trigger Can. Does not stack.'),
('SUONA_POWER_CARD','唢呐','Suona','每个你的回合，第3张手动打出的牌额外打出1次。自动出牌不计数。不可叠加。','Each turn, the third card you manually play is played one additional time. Auto-plays do not count. Does not stack.'),
('LAST_XUAN_CARD','要我告诉你多少遍，你的青春由我last炫来无限复活','Last Xuan, Endless Youth','召唤炫狗。受到致命伤害时，若炫狗在场，使其入狱并将生命恢复至最大生命的50%（向上取整）。救回炫狗后可再次触发。无法阻止直接击杀。','Summon Xuan Dog. On lethal damage while it is present, imprison it and restore HP to 50% of Max HP, rounded up. Can trigger again after rescuing it. Does not prevent direct kills.'),
('ATOMIC_BOMB','原子弹','Atomic Bomb','需要山泥若在场。对所有敌人造成其正常治疗量{Multiplier:diff()}倍的伤害，然后使其入狱。满血仍按正常治疗量计算。','Requires Shan Ni Ruo. Deal {Multiplier:diff()} times its normal healing amount as damage to ALL enemies, then imprison it. Works at full HP.'),
('NANAMI','七海娜娜米','Nanami','本场战斗，进入古神形态所需的古神值固定为100。进入仍清空古神值，形态期间不积攒。','The Ancient Meter required to enter Ancient Form is fixed at 100 this combat. Entering still clears the meter; no meter is gained during the form.'),
('LOST_PARADISE_CARD','消逝の天堂，参上！','Lost Paradise Arrives','召唤火星包。每次打出攻击牌后，它立即行动1次。回合结束时仍正常行动。不可叠加。','Summon Huo Xing Bao. After each Attack is played, it acts immediately. It still acts at turn end. Does not stack.'),
('CCB','CCB','CCB','移除自身所有虚弱、易伤、脆弱和中毒，并将移除层数的2倍分别施加给所有敌人。抽2张牌。','Remove all your Weak, Vulnerable, Frail and Poison. Apply twice the removed stacks of each to ALL enemies. Draw 2 cards.'),
('AN_NAI','安奈师傅','Master An Nai','从抽牌堆选择最多2张非X费牌，升级并加入手牌。本回合它们的费用为0。','Choose up to 2 non-X-cost cards in your draw pile. Upgrade them and put them into your hand. They cost 0 this turn.'),
('DA_SI_MA_CARD','芜湖大司马','Wuhu Da Si Ma','回合结束时记录本回合最后手动打出的非X费攻击或技能。下回合抽牌后免费额外打出它1次。原牌所在牌堆不变。不可叠加。','At turn end, record the last non-X Attack or Skill you manually played this turn. After drawing next turn, play an extra copy for free. The original stays in its pile. Does not stack.'),
('JI_BAI','鸡掰！','Ji Bai!','造成{Damage:diff()}点伤害。古神形态每剩余1轮，额外攻击1次，然后退出古神形态。','Deal {Damage:diff()} damage. Repeat once for each remaining round of Ancient Form, then exit the form.'),
('SOYO','长崎素世-official','Nagasaki Soyo-official','消耗任意数量的其他手牌。每消耗1张，获得{Block:diff()}点格挡并抽1张牌。','Exhaust any number of other cards in your hand. For each card Exhausted, gain {Block:diff()} Block and draw 1 card.'),
('TOMORI','高松灯-official','Takamatsu Tomori-official','从消耗牌堆选择最多{Cards:diff()}张非同名牌加入手牌，本回合费用为0。每选中1张少女乐队牌，额外获得其0费消耗复制牌。复制牌保留升级，0费仅限本回合。','Return up to {Cards:diff()} cards other than Takamatsu Tomori from your Exhaust pile to your hand. They cost 0 this turn. For each Girl Band card chosen, also add a copy with Exhaust that costs 0 this turn and keeps its upgrades.'),
('V5','v5-otto','v5-otto','每个你的回合结束时，从所有角色牌池中随机免费打出3张{IfUpgraded:show:升级过的|}带有格挡效果的牌，允许重复。直接打出，完整结算效果。','At the end of your turn, directly play 3 random {IfUpgraded:show:Upgraded |}cards with Block effects from all character pools for free. Repeats are allowed. Resolve their full effects.'),
]
POWERS = [
('ANCIENT_THRESHOLD','下次变身门槛','Next Ancient Threshold','下次进入古神形态需要{Amount}点古神值。每次重新进入后增加50；形态内刷新不增加。','The next Ancient Form requires {Amount} Ancient Meter. Each new entry increases this by 50; refreshing an active form does not.'),
('LAW_ABIDING_POWER','守法保护','Law Abiding','本场战斗首次山泥若入狱时，防止入狱并立即行动3次。','Prevent Shan Ni Ruo\'s first imprisonment this combat and have it act 3 times.'),
('KFC_POWER','肯德基','KFC','直到你的下回合开始，山泥若的治疗改为对所有敌人造成正常治疗量{Amount}倍的伤害。','Until your next turn, Shan Ni Ruo replaces healing with damage to ALL enemies equal to {Amount} times its normal healing.'),
('MARS_MODE','火星包模式','Huo Xing Bao Mode','力量模式：行动时获得双倍正常力量，不获得敏捷。','Strength Mode: actions grant double normal Strength and no Dexterity.'),
('DOG_COUNTER','大刀反击','Great Blade Counter','直到你的下回合开始，炫狗在场时，每段消耗格挡并完全挡住的敌方攻击触发3倍炫狗伤害的反击。','Until your next turn, while Xuan Dog is present, each enemy attack hit fully blocked by spending Block triggers a counter for 3 times its damage.'),
('LAST_XUAN','青春由我复活','Endless Youth','受到致命伤害时，若炫狗在场，使其入狱并恢复至50%最大生命（向上取整）。不阻止直接击杀。','On lethal damage, imprison Xuan Dog if present and restore to 50% Max HP, rounded up. Does not prevent direct kills.'),
('LOST_PARADISE','消逝的天堂','Lost Paradise','每次打出攻击牌后，若火星包在场，使其立即行动1次。','After each Attack is played, Huo Xing Bao acts immediately if present.'),
('LAUGHTER','电棍笑传','OTTO Laughter','手动从手牌边缘触发易拉罐后，免费额外打出该牌1次。追加不触发易拉罐。','After a manual edge play triggers Can, play an extra copy for free. Extra plays do not trigger Can.'),
('DA_SI_MA','芜湖大司马','Wuhu Da Si Ma','记录回合最后手动打出的非X费攻击或技能，下回合抽牌后免费额外打出1次。','Record the last manually played non-X Attack or Skill each turn; play an extra copy for free after drawing next turn.'),
('V5_POWER','v5-otto','v5-otto','你的回合结束时，直接免费打出{Amount}张随机全职业格挡效果牌，其中{Upgraded}张为升级版。','At the end of your turn, directly play {Amount} random cards with Block effects from all character pools for free; {Upgraded} are Upgraded.'),
('AMINOS_POWER','阿米诺斯','Aminos','本场战斗，古神形态额外持续{Amount}轮。','Ancient Form lasts {Amount} additional rounds this combat.'),
('SUONA_POWER','唢呐','Suona','每个你的回合，第3张手动打出的牌额外打出1次。自动出牌不计数。','Each turn, play your third manually played card one additional time. Auto-plays do not count.'),
('NANAMI_POWER','七海娜娜米','Nanami','本场战斗，进入古神形态的门槛固定为100。进入清空古神值，形态期间不积攒。','The Ancient Form threshold is fixed at 100 this combat. Entering clears the meter; no meter is gained in the form.'),
]
for lang, title, effect in [('zhs',1,3), ('eng',2,4)]:
    for name, entries in [('cards',CARDS),('powers',POWERS)]:
        path = ROOT / lang / (name+'.json')
        data = json.loads(path.read_text())
        for row in entries:
            data['OTTO_'+row[0]+'.title'] = row[title]
            data['OTTO_'+row[0]+'.description'] = row[effect]
        if name == 'cards':
            data['OTTO_AN_NAI.selectionScreenPrompt'] = '选择最多2张非X费牌。' if lang=='zhs' else 'Choose up to 2 non-X-cost cards.'
            data['OTTO_TOMORI.selectionScreenPrompt'] = '选择要回收的牌（不能选择高松灯）。' if lang=='zhs' else 'Choose cards to return (except Takamatsu Tomori).'
        else:
            data['OTTO_ANCIENT_METER.description'] = '积攒到{Threshold}点时进入古神形态，清空全部古神值。形态期间不再积攒。' if lang=='zhs' else 'At {Threshold} Ancient Meter, enter Ancient Form and clear all meter. No meter is gained during the form.'
            data['OTTO_ANCIENT_METER.rulesDescription'] = '本场战斗初始门槛为100，每次重新进入古神形态后，下次门槛增加50。进入清空全部古神值，形态期间不积攒。' if lang=='zhs' else 'The initial threshold is 100 each combat. Each new entry increases the next threshold by 50. Entering clears all meter; no meter is gained during the form.'
            data['OTTO_LAW_ABIDING_POWER.usedDescription'] = '本场战斗的入狱保护已使用，重复打出不会刷新。' if lang=='zhs' else 'Imprisonment protection has been used this combat. Playing this again does not reset it.'
            data['OTTO_MARS_MODE.dexterityDescription'] = '敏捷模式：行动时获得双倍正常敏捷，不获得力量。' if lang=='zhs' else 'Dexterity Mode: actions grant double normal Dexterity and no Strength.'
            data['OTTO_HUO_XING_BAO.strengthDescription'] = '你的回合结束时，获得{Value}点力量，不获得敏捷。' if lang=='zhs' else 'At the end of your turn, gain {Value} Strength and no Dexterity.'
            data['OTTO_HUO_XING_BAO.dexterityDescription'] = '你的回合结束时，获得{Value}点敏捷，不获得力量。' if lang=='zhs' else 'At the end of your turn, gain {Value} Dexterity and no Strength.'
        path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
