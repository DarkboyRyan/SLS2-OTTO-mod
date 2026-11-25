using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace OttoMod;

/// <summary>统一正式版与 beta 的攻击来源接口；beta 必须传入本次出牌，才能正确结算重放和伤害钩子。</summary>
public static class OttoAttackCompatibility
{
    public static AttackCommand FromOttoCard(this AttackCommand command, CardModel card, CardPlay play)
    {
#if STS2_BETA
        return command.FromCard(card, play);
#else
        return command.FromCard(card);
#endif
    }
}
