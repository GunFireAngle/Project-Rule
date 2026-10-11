namespace RuleGame.Infrastructure.Rules
{
    /// <summary>
    /// 属性管理器的值。
    /// </summary>
    /// <remarks>
    /// **只保存「没有引擎全局载体」的属性**：
    /// 重力直接写 <c>Physics2D.gravity</c>、时间速率直接写 <c>Time.timeScale</c>，
    /// 都不在这里。
    /// </remarks>
    public struct AttributeValues
    {
        /// <summary>摩擦力缩放。1.0 = 正常。</summary>
        public float FrictionScale;

        /// <summary>
        /// 摩擦力方向反转。
        /// </summary>
        /// <remarks>
        /// <para>与 <see cref="FrictionScale"/> 是**两件事**：缩放改的是摩擦力大小，
        /// 这个标志表达的是「摩擦力的方向反过来」。所以反转生效时
        /// <see cref="FrictionScale"/> 应该保持 1.0，而不是变成负数——
        /// Unity 会把 <c>PhysicsMaterial2D.friction</c> 的负值夹到 0，
        /// 用负数表达反转会退化成「失效」。</para>
        /// <para>具体怎么响应由**读这个值的实体自己决定**，不在规则系统的职责内。</para>
        /// </remarks>
        public bool FrictionInverted;

        /// <summary>质量缩放。1.0 = 正常。</summary>
        public float MassScale;

        /// <summary>
        /// 质量反转：重的变轻、轻的变重。
        /// </summary>
        /// <remarks>
        /// ⚠️ **不能用单一的 <see cref="MassScale"/> 表达** —— 反转是**逐实体**的，
        /// 而缩放是全局倍率。所以这里用标志位，由实体自己按参考质量取倒数。
        /// 与 <see cref="DamageInverted"/> 同一个套路。
        /// </remarks>
        public bool MassInverted;

        /// <summary>伤害缩放。1.0 = 正常。</summary>
        public float DamageScale;

        /// <summary>攻防反转：攻击变回血。</summary>
        public bool DamageInverted;

        /// <summary>伤害失效。</summary>
        public bool DamageDisabled;

        /// <summary>默认值（全部正常）。</summary>
        public static AttributeValues Default => new AttributeValues
        {
            FrictionScale    = 1f,
            FrictionInverted = false,
            MassScale        = 1f,
            MassInverted  = false,
            DamageScale   = 1f,
        };
    }
}
