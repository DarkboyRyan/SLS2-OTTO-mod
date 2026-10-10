using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace OttoMod;

/// <summary>同时整理实际手牌顺序与本地显示；不能只移动图片，否则易拉罐判定仍读取旧位置。</summary>
public static class OttoHandOrder
{
    public static void MoveLeft(CardModel card, AbstractModel? selectionSource = null)
    {
        if (card.Pile is not { Type: PileType.Hand } hand) return;
        hand.MoveToTopInternal(card);
        hand.InvokeContentsChanged();
        // 不移出/重进牌堆，避免错误触发抽牌、入场、复制效果，也不受满手限制影响。
        SyncLocalView(card);
        if (selectionSource is not null)
        {
            // 原版选择器把选中牌暂放在另一个容器，直到源卡完成效果才送回手牌最右侧。
            // 在原版的归还回调之后再同步一次，避免模型已在最左、画面却仍在最右。
            selectionSource.ExecutionFinished += AfterSelectionReturned;
            void AfterSelectionReturned(AbstractModel source)
            {
                source.ExecutionFinished -= AfterSelectionReturned;
                SyncLocalView(card);
            }
        }
    }

    private static void SyncLocalView(CardModel card)
    {
        if (card.Pile?.Type != PileType.Hand) return;
        if (!LocalContext.IsMe(card.Owner) || NPlayerHand.Instance is not { } view) return;
        SyncView(card, view);
    }

    internal static void SyncView(CardModel card, NPlayerHand view)
    {
        if (view.GetCardHolder(card) is { } holder && holder.GetParent() == view.CardHolderContainer)
        {
            view.CardHolderContainer.MoveChild(holder, 0);
            view.ForceRefreshCardIndices();
        }
    }
}
