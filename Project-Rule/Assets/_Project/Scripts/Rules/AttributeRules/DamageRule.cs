using RuleGame.Infrastructure.Rules;

namespace RuleGame.Core.Rules
{
    /// <summary>
    /// 伤害 / 生命属性规则。
    /// </summary>
    /// <remarks>
    /// <para>伤害<b>没有引擎全局载体</b>，所以写进 <see cref="IAttributeManager"/>（见 §4.5）——
    /// 战斗系统再从那里读，玩家与敌人共用同一份值。</para>
    /// <para>只改伤害相关的三个字段，<b>不动摩擦 / 质量</b>：否则会误伤同时生效的其它规则。</para>
    /// </remarks>
    public class DamageRule : AttributeRuleBase
    {
        private const float EnhancedScale = 2f;
        private const float WeakenedScale = 0.5f;

        /// <summary>反转：攻击变回血。</summary>
        protected override bool OnRevert()
        {
            ResetDamage();
            Attributes.SetDamageInverted(true);
            return true;
        }

        /// <summary>失效：攻击与回血都失效。</summary>
        protected override bool OnNullify()
        {
            ResetDamage();
            Attributes.SetDamageDisabled(true);
            return true;
        }

        /// <summary>恒定：伤害参数恢复正常。</summary>
        /// <remarks>
        /// ⚠️ 规则矩阵里「伤害/生命 × 恒定」仍是 undecided（原文「攻击和回血都变成定值？」）。
        /// 这里暂按其它属性的「恒定 = 恢复正常」语义实现，<b>待策划定稿</b>。
        /// </remarks>
        protected override bool OnConstant()
        {
            ResetDamage();
            return true;
        }

        /// <summary>增强：攻防数值放大。</summary>
        protected override bool OnEnhance()
        {
            ResetDamage();
            Attributes.SetDamageScale(EnhancedScale);
            return true;
        }

        /// <summary>削弱：攻防数值缩小。</summary>
        protected override bool OnWeaken()
        {
            ResetDamage();
            Attributes.SetDamageScale(WeakenedScale);
            return true;
        }

        /// <summary>到期 / 被覆盖 / 重开关卡时恢复默认伤害参数。</summary>
        public override void Clear() => ResetDamage();

        /// <summary>只重置伤害三个字段，不碰摩擦 / 质量。</summary>
        private void ResetDamage()
        {
            Attributes.SetDamageScale(1f);
            Attributes.SetDamageInverted(false);
            Attributes.SetDamageDisabled(false);
        }
    }
}
