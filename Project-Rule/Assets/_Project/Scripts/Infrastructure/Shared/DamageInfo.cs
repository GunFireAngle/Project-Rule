namespace RuleGame.Infrastructure
{
    /// <summary>
    /// 一次伤害的完整信息。由施加方构造，传给承受方。
    /// </summary>
    /// <remarks>
    /// <para><b>Amount 已含全局伤害参数</b>（缩放 / 反转 / 失效，见 <c>IAttributeManager.Values</c>），
    /// 但<b>尚未扣目标自身抗性</b> —— 抗性写在承受方自己的
    /// <see cref="IDamageable.ApplyDamage"/> 里（守约束 C4）。</para>
    /// <para>Amount 为负表示<b>回血</b>（伤害反转规则生效时）。</para>
    /// </remarks>
    public struct DamageInfo
    {
        /// <summary>施加方。</summary>
        public object Attacker;

        /// <summary>承受方。</summary>
        public object Target;

        /// <summary>伤害数值（负数为回血）。</summary>
        public float Amount;

        /// <summary>击退数值。</summary>
        public float Knockback;
    }

    /// <summary>
    /// 可承受伤害的实体。玩家与敌人各自实现，接口一致。
    /// </summary>
    /// <remarks>
    /// <b>不暴露抗性值</b> —— 抗性逻辑写在各自的实现内部，
    /// 这样规则系统与战斗系统都不必知道任何实体的抗性（约束 C4）。
    /// </remarks>
    public interface IDamageable
    {
        void ApplyDamage(in DamageInfo damage);
    }
}
