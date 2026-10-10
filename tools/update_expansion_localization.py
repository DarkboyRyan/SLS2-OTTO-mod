"""补入2026-09-23新增卡牌文本；不覆盖前一批卡牌的名称与数值。"""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[1]

# 元组依次为：稳定ID、中文名、中文说明、英文名、英文说明。
CARDS = [
    ('WAWO', '哇沃！', '抽 {Cards:diff()} 张牌。它们在本场战斗中获得保留，但本回合无法打出。', 'Wawo!', 'Draw {Cards:diff()} cards. They gain Retain this combat, but cannot be played this turn.'),
    ('ONE_HAND', '偶内的手', '获得 {CalculatedBlock:diff()} 点格挡。\n基础为 {CalculationBase:diff()}；本回合打出过阿米诺手则基础翻倍。', 'One Hand', 'Gain {CalculatedBlock:diff()} Block.\nBase: {CalculationBase:diff()}, doubled if you played Amino Hand this turn.'),
    ('AMINO_HAND', '阿米诺手', '造成 {CalculatedDamage:diff()} 点伤害。\n基础为 {CalculationBase:diff()}；本回合打出过偶内的手则基础翻倍。', 'Amino Hand', 'Deal {CalculatedDamage:diff()} damage.\nBase: {CalculationBase:diff()}, doubled if you played One Hand this turn.'),
    ('SUONA', '吹唢呐，嘟嘟嘟', '获得 {Energy:diff()} 点能量。\n你的下个回合无法打出攻击牌。', 'Suona, Doot Doot', 'Gain {Energy:diff()} Energy.\nYou cannot play Attacks during your next turn.'),
    ('BULL_DEMON', '牛魔', '获得 {Block:diff()} 点格挡。\n本场战斗每打出一张易拉罐，所有牛魔费用降低1。打出任一牛魔后重置计数。', 'Bull Demon', 'Gain {Block:diff()} Block.\nEach Can played this combat reduces the cost of all Bull Demons by 1. Playing any Bull Demon resets this count.'),
    ('BACK_ELEPHANT', '踩背大象', '造成 {Damage:diff()} 点伤害。', 'Back-Stomping Elephant', 'Deal {Damage:diff()} damage.'),
    ('SILVER', '白银就是癌症晚期', '本回合无法再打出任何牌。\n下个回合额外获得3点能量。', 'Terminal Silver', 'You cannot play any more cards this turn.\nGain 3 additional Energy next turn.'),
    ('VOID_SNORLAX', '虚空卡比兽', '获得 {Block:diff()} 点格挡。\n直到你的下回合开始，每次受到的伤害降低 {Reduction:diff()} 点。', 'Void Snorlax', 'Gain {Block:diff()} Block.\nUntil your next turn starts, reduce each instance of damage taken by {Reduction:diff()}.'),
    ('SPONGE_EKKO', '海绵宝宝艾克儿', '造成 {Damage:diff()} 点伤害。\n本场战斗中，你下次打出的易拉罐牌费用为0。', 'Sponge Ekko', 'Deal {Damage:diff()} damage.\nThe next Can you play this combat costs 0 Energy.'),
    ('VTUBER_ESSAY', '皮套小作文', '抽8张牌，丢弃其中所有非易拉罐牌。', 'Vtuber Essay', 'Draw 8 cards. Discard the drawn cards that are not Cans.'),
    ('HALILU_TORNADO', '哈利路大旋风', '从所有角色的常规攻击牌池中随机获得X张{IfUpgraded:show:升级过的|}攻击牌。本回合它们的费用为0。', 'Halilu Tornado', 'Add X random {IfUpgraded:show:upgraded |}Attacks from all character pools to your Hand. They cost 0 this turn.'),
    ('NONEXISTENT', '不存在之物', '触发你持有的棍亩或老棍和棍亩 {Repeat:diff()} 次。\n每次分别获得1或2层无实体。', 'What Does Not Exist', 'Trigger your Gun Mu or Lao Gun and Gun Mu {Repeat:diff()} times, gaining 1 or 2 Intangible per trigger respectively.'),
    ('ZOO_BOND_CARD', '动物园的羁绊', '召唤全部三种动物园伙伴。\n本场战斗允许三种伙伴同时存在，每种最多一只。', 'Zoo Bond', 'Summon all three Zoo companions.\nFor this combat, different companions coexist. You may have at most one of each.'),
    ('SECOND_STREAM_GOAT', '二路GOAT', '召唤炫狗并强化它。\n本场战斗，炫狗回合末效果变为造成10点伤害并获得10点格挡。', 'Second-Stream GOAT', 'Summon and train Xuan Dog.\nFor this combat, its end-of-turn effect deals 10 damage and grants 10 Block.'),
    ('COMEBACK_DIRECTOR', '逆风局局长', '召唤火星包并强化它。\n本场战斗，火星包回合末效果变为获得3点力量和3点敏捷。', 'Comeback Director', 'Summon and train Huo Xing Bao.\nFor this combat, its end-of-turn effect grants 3 Strength and 3 Dexterity.'),
    ('ETERNAL_GOD', '永远的神', '召唤山泥若并强化它。\n本场战斗，山泥若回合末效果变为回复4点生命，并对所有敌人施加4层中毒。', 'Eternal God', 'Summon and train Shan Ni Ruo.\nFor this combat, its end-of-turn effect heals 4 HP and applies 4 Poison to all enemies.'),
    ('JOHNNIE_WALKER', '尊尼获加', '本场战斗中，当前所有手牌获得易拉罐。', 'Johnnie Walker', 'For this combat, all cards currently in your Hand gain Can.'),
    ('THEIR_OWN_WAY', '其人之道', '复制一个敌人的下次攻击、格挡和可转换的强化。\n攻击反打该目标，格挡和强化给予自己，按自身属性结算。\n攻击附带的专属强化若无法转换则忽略；含其他意图不可选。', 'Their Own Way', "Copy one enemy's next Attack, Block and transferable buffs, using your own stats. Attacks hit that enemy; Block and buffs go to you. Ignore unconvertible buffs accompanying attacks. Other intents cannot be selected."),
]

POWERS = [
    ('CARD_RULES', '出牌记录', '记录本场战斗的易拉罐次数和本回合的手连招。', 'Card Records', 'Tracks Cans played this combat and Hands played this turn.'),
    ('FREE_CAN', '下一罐免费', '本场战斗中，下次打出的易拉罐牌能量和星星费用为0。', 'Next Can Free', 'The next Can you play this combat costs 0 Energy.'),
    ('NO_ATTACK_NEXT_TURN', '吹唢呐的代价', '下个自己的回合无法打出攻击牌。生效后，该回合末移除。', 'Suona Aftermath', 'You cannot play Attacks on your next turn. Expires at the end of that turn.'),
    ('NO_MORE_CARDS', '白银时刻', '本回合无法再打出任何牌。', 'Silver Moment', 'You cannot play any more cards this turn.'),
    ('FLAT_GUARD', '虚空防护', '每次受到的伤害降低 {Amount} 点，最少为0。你的下回合开始时移除。', 'Void Protection', 'Reduce each instance of damage taken by {Amount}, to a minimum of 0. Expires at the start of your next turn.'),
    ('ZOO_BOND', '动物园的羁绊', '不同动物园伙伴可以共存，每种最多一只。', 'Zoo Bond', 'Different Zoo companions coexist, at most one of each.'),
    ('DOG_TRAINING', '二路GOAT', '炫狗强化为回合末10伤害、10格挡。换走后再召回仍有效，重复强化不叠加。', 'Trained Xuan Dog', 'Xuan Dog deals 10 damage and grants 10 Block at turn end. Persists through replacement. Does not stack.'),
    ('MARS_TRAINING', '逆风局局长', '火星包强化为回合末3力量、3敏捷。换走后再召回仍有效，重复强化不叠加。', 'Trained Huo Xing Bao', 'Huo Xing Bao grants 3 Strength and 3 Dexterity at turn end. Persists through replacement. Does not stack.'),
    ('PIG_TRAINING', '永远的神', '山泥若强化为回合末回复4生命、对全体敌人施加4中毒。换走后再召回仍有效，重复强化不叠加。', 'Trained Shan Ni Ruo', 'Shan Ni Ruo heals 4 HP and applies 4 Poison to all enemies at turn end. Persists through replacement. Does not stack.'),
]

KEYWORDS = [
    ('CAN', '易拉罐', '从手牌最左或最右位置手动打出时，结算后抽1张牌。', 'Can', 'When manually played from the leftmost or rightmost position in your Hand, draw 1 card after resolving.'),
    ('HAND', '手', '两种不同名字的手互相触发本回合连招。同名手不会触发彼此。', 'Hand', 'The two differently named Hands activate each other for this turn. Identically named Hands do not.'),
    ('DELAYED', '下回合可用', '本回合无法打出，直到你的下个回合开始。', 'Delayed', 'Cannot be played until the start of your next turn.'),
]

for language, name_index, text_index in [('zhs', 1, 2), ('eng', 3, 4)]:
    directory = ROOT / 'OTTO' / 'localization' / language
    for filename, rows, prefix in [('cards', CARDS, 'OTTO_'), ('powers', POWERS, 'OTTO_'), ('card_keywords', KEYWORDS, 'OTTOMOD-OTTO_')]:
        path = directory / (filename + '.json')
        data = json.loads(path.read_text()) if path.exists() else {}
        for row in rows:
            data[prefix + row[0] + '.title'] = row[name_index]
            data[prefix + row[0] + '.description'] = row[text_index]
        if filename == 'cards':
            data['OTTO_CALL_FRIENDS.description'] = ('随机召唤一位动物园伙伴，替换原伙伴。\n有动物园的羁绊时，不同种类可以共存。伙伴在你的回合结束时行动。' if language == 'zhs' else 'Summon a random Zoo companion, replacing your current companion. With Zoo Bond, different companions coexist. Companions act at the end of your turn.')
        if filename == 'powers':
            # 伙伴强化与唢呐生效后显示对应的实际状态，而不是固定写最初的数值/时机。
            descriptions = {
                'OTTO_XUAN_DOG.trainedDescription': ('强化动物园伙伴。你的回合结束时，获得10点格挡，并对一名随机敌人造成10点伤害。', 'Trained Zoo companion. At turn end, gain 10 Block and deal 10 damage to a random enemy.'),
                'OTTO_SHAN_NI_RUO.trainedDescription': ('强化动物园伙伴。你的回合结束时，回复4点生命，并对所有敌人施加4层中毒。', 'Trained Zoo companion. At turn end, heal 4 HP and apply 4 Poison to all enemies.'),
                'OTTO_HUO_XING_BAO.trainedDescription': ('强化动物园伙伴。你的回合结束时，获得3点力量与3点敏捷。', 'Trained Zoo companion. At turn end, gain 3 Strength and 3 Dexterity.'),
                'OTTO_NO_ATTACK_NEXT_TURN.activeDescription': ('本回合无法打出攻击牌。回合结束后移除。', 'You cannot play Attacks this turn. Expires at turn end.'),
                'OTTO_NO_ATTACK_NEXT_TURN.activeAndPendingDescription': ('本回合及你的下个回合都无法打出攻击牌。', 'You cannot play Attacks this turn or your next turn.'),
            }
            for key, texts in descriptions.items():
                data[key] = texts[0 if language == 'zhs' else 1]
            for key in ['OTTO_XUAN_DOG.description', 'OTTO_SHAN_NI_RUO.description', 'OTTO_HUO_XING_BAO.description']:
                # 去掉已不适用于羁绊的绝对单伙伴说明，并保留原始效果说明。
                data[key] = data[key].replace('动物园伙伴（同时仅一位）。', '动物园伙伴。').replace('Only one Zoo companion can be active. ', '').replace('Zoo companion (only one at a time).', 'Zoo companion.')
        path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')

print('已更新18张新卡、9种状态和3个关键词的中英文说明。')
