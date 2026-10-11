namespace RuleGame.Infrastructure.Rules
{
    /// <summary>
    /// 规则系统的属性（已砍掉「空间」和「方向」，「关系」也已从策划案删除）。
    /// </summary>
    /// <remarks>
    /// ⚠️ 枚举顺序即规则维护表的槽位下标。**不要调整顺序**，
    /// 也不要插入新值 —— 那会让已存档的规则对不上槽位。
    /// 删除同样不行：「关系」被砍掉后仍<b>保留枚举位</b>，只是不再有代码使用它。
    /// </remarks>
    public enum AttributeId
    {
        Gravity,    // 重力
        Time,       // 时间
        Friction,   // 摩擦
        Mass,       // 质量
        Relation,   // 关系 —— 已从策划案删除，保留占位以免下标漂移
        Damage,     // 伤害/生命
        Causality,  // 因果
    }
}
