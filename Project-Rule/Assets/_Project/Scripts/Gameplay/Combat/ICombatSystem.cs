using System;
using RuleGame.Infrastructure;

namespace RuleGame.Gameplay.Combat
{
    /// <summary>
    /// 战斗系统的事件宿主（接口文档 §5.2）。
    /// </summary>
    /// <remarks>
    /// <para><b>它住在规则系统下游：只读规则，不写规则。</b></para>
    /// <para>具体的伤害结算与命中判定入口在实现类 <c>CombatSystem</c> 上
    /// （文档 §5.2 只列了事件；攻击 API 属于实现层，待并入冻结文档）。</para>
    /// </remarks>
    public interface ICombatSystem
    {
        /// <summary>一次伤害施加后触发。</summary>
        event Action<DamageInfo> OnDamageApplied;

        /// <summary>某个实体死亡时触发。</summary>
        event Action<object> OnEntityDied;
    }
}
