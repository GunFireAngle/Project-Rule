using RuleGame.Infrastructure.Rules;

namespace RuleGame.Core.Rules
{
    /// <summary>
    /// 属性规则类基类。7 个属性各实现一个。
    /// </summary>
    /// <remarks>
    /// <para>基类**不设 AttributeId 属性** —— 类名本身就是标识。</para>
    /// <para>基类**不接收 RuleDef** —— 只接收关系枚举。</para>
    /// <para>基类**不持有计时器** —— 剩余时间由规则维护表持有。</para>
    /// </remarks>
    public abstract class AttributeRuleBase
    {
        // ------------------------------------------------------------------
        //  依赖：属性管理器
        // ------------------------------------------------------------------

        private IAttributeManager _attributes;

        /// <summary>
        /// 属性管理器：没有引擎全局载体的属性（摩擦 / 质量 / 伤害 / 关系 / 因果）写到这里。
        /// </summary>
        /// <remarks>
        /// ⚠️ 接口文档 v3.0 §2.3 的基类没有这个入口，但规则类要落地「写属性管理器」（见 §4.5）
        /// 就必须拿到它。采用「由 <c>RuleSystem</c> 创建后注入」而非让规则类自己取单例，
        /// 是为了保持规则类可单独测试。<b>待团队确认后并入冻结文档。</b>
        /// </remarks>
        protected IAttributeManager Attributes => _attributes;

        /// <summary>由 <c>RuleSystem</c> 在创建规则类后注入依赖。子类无需覆写。</summary>
        internal void Bind(IAttributeManager attributes) => _attributes = attributes;

        // ------------------------------------------------------------------
        //  五种关系算法
        //  未定义的格位直接 return false（会被 RuleSystem 记为 Undefined）
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
