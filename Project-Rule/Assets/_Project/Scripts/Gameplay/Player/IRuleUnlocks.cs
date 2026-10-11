using RuleGame.Infrastructure.Rules;

namespace RuleGame.Gameplay.Player
{
    /// <summary>
    /// 词条解锁查询。规则释放前的第一道门。
    /// </summary>
    /// <remarks>
    /// <para>玩家侧：读存档里「已解锁的属性 / 关系」。（<c>ISaveSystem</c> 落地前可先用一个内存实现。）</para>
    /// <para>敌人侧：传 <see cref="AllUnlockedRuleUnlocks.Instance"/> —— 敌人不受玩家解锁进度限制。</para>
    /// </remarks>
    public interface IRuleUnlocks
    {
        bool IsUnlocked(AttributeId attr, RelationId rel);
    }

    /// <summary>全解锁实现。供敌人使用。</summary>
    public sealed class AllUnlockedRuleUnlocks : IRuleUnlocks
    {
        public static readonly AllUnlockedRuleUnlocks Instance = new AllUnlockedRuleUnlocks();

        public bool IsUnlocked(AttributeId attr, RelationId rel) => true;
    }
}
