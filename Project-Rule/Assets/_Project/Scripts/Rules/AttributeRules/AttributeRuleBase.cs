using RuleGame.Infrastructure.Rules;

namespace RuleGame.Core.Rules
{
    /// <summary>
    /// 属性规则类基类。6 个属性各实现一个。
    /// </summary>
    /// <remarks>
    /// <para>基类**不设 AttributeId 属性** —— 类名本身就是标识。</para>
    /// <para>基类**不接收 RuleDef** —— 只接收关系枚举。</para>
    /// <para>基类**不持有计时器** —— 剩余时间由规则维护表持有。</para>
    /// <para>基类**持有 <see cref="IAttributeManager"/>** —— 没有引擎全局载体的属性
    /// （摩擦 / 质量 / 伤害）必须通过它写值。有全局载体的（重力 / 时间）拿到也不用。</para>
    /// </remarks>
    public abstract class AttributeRuleBase
    {
        /// <summary>属性管理器。规则类通过它写属性值。</summary>
        protected IAttributeManager Attributes { get; }

        /// <param name="attributes">由 <c>RuleSystem</c> 注入，与它自己用的是同一个实例。</param>
        protected AttributeRuleBase(IAttributeManager attributes)
        {
            Attributes = attributes ?? throw new System.ArgumentNullException(nameof(attributes));
        }

        // ------------------------------------------------------------------
        //  五种关系算法
        //
        //  返回 false = 「这个格位存在，但算法跑不通」→ RuleRequestStatus.NoResource。
        //
        //  ⚠️ 「格位在矩阵里不存在」是**另一回事**，由 IRuleCatalog.Get 返回 null 决定，
        //  RuleSystem 在那一步就会返回 Undefined，根本走不到规则类。
        // ------------------------------------------------------------------

        protected abstract bool OnRevert();
        protected abstract bool OnNullify();
        protected abstract bool OnConstant();
        protected abstract bool OnEnhance();
        protected abstract bool OnWeaken();

        /// <summary>
        /// 统一入口：路由到对应的算法。
        /// </summary>
        /// <returns>算法是否成功。失败时调用方（RuleSystem）会回滚槽位。</returns>
        public RuleRequestResult Request(RelationId rel)
        {
            bool ok = rel switch
            {
                RelationId.Revert   => OnRevert(),
                RelationId.Nullify  => OnNullify(),
                RelationId.Constant => OnConstant(),
                RelationId.Enhance  => OnEnhance(),
                RelationId.Weaken   => OnWeaken(),
                _                   => false,
            };

            return ok
                ? RuleRequestResult.Ok()
                : RuleRequestResult.Fail(RuleRequestStatus.NoResource);
        }

        /// <summary>每帧维护。行为型规则（时间恒定 / 反转）覆写它。</summary>
        public virtual void Tick(float deltaTime) { }

        /// <summary>
        /// 规则到期 / 被覆盖 / 重开关卡时调用。
        /// </summary>
        /// <remarks>⚠️ 必须把世界恢复原样。这是最容易漏的一步。</remarks>
        public virtual void Clear() { }
    }
}
