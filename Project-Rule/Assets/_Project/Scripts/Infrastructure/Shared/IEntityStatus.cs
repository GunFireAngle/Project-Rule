using System;
using RuleGame.Infrastructure.Rules;

namespace RuleGame.Infrastructure
{
    /// <summary>
    /// 实体状态组件。玩家与敌人各挂一个，行为可不同，接口一致。
    /// </summary>
    /// <remarks>
    /// <para>承载<b>能量、生命、持有物、冷却</b>（接口文档 §4.1）。</para>
    /// <para><b>它不认识规则系统。</b>—— 规则的合法性判定在 <c>RuleReleaseService</c>，
    /// 本接口只回答「还够不够、好没好」。</para>
    /// <para>位置放在 <c>Infrastructure/Shared/</c>，使 UI（HUD）与敌人都能引用，
    /// 而不必反向依赖 Gameplay。</para>
    /// </remarks>
    public interface IEntityStatus
    {
        // ==================================================================
        //  能量
        // ==================================================================

        float Energy    { get; }
        float EnergyMax { get; }

        /// <summary>扣除能量。<b>不足则返回 false 且不扣除。</b></summary>
        bool TryConsumeEnergy(float amount);

        void RestoreEnergy(float amount);

        // ==================================================================
        //  生命
        // ==================================================================

        float Health    { get; }
        float HealthMax { get; }

        // ==================================================================
        //  持有物
        // ==================================================================

        object HeldItem       { get; }
        int    ThrowableCount { get; }

        // ==================================================================
        //  冷却（按 属性 × 关系 索引）
        // ==================================================================

        bool  IsOnCooldown(AttributeId attr, RelationId rel);
        void  BeginCooldown(AttributeId attr, RelationId rel, float seconds);
        float GetCooldownRemaining(AttributeId attr, RelationId rel);

        // ==================================================================
        //  快照 / 存档桥接
        // ==================================================================

        /// <summary>导出为 <see cref="EntityStatusData"/>，供存档与快照系统使用。</summary>
        /// <remarks>
        /// ⚠️ <b>v3.0 §4.1 未列出下面这两个方法</b>，但快照系统（§3.2 的 <c>EntityState.Status</c>）
        /// 与存档（<c>SaveData.PlayerStatus</c>）都需要读写实体状态的同一份数据。
        /// 放在接口上、而不是让调用方 <c>GetComponent</c> 摸内部字段，是为了避免两份实现漂移。
        /// <b>待团队确认后并入冻结文档。</b>
        /// </remarks>
        EntityStatusData ToData();

        /// <summary>用 <see cref="EntityStatusData"/> 覆盖当前状态。</summary>
        void ApplyData(EntityStatusData data);

        // ==================================================================
        //  事件
        // ==================================================================

        event Action<float, float> OnEnergyChanged;
        event Action<float, float> OnHealthChanged;
        event Action<AttributeId, RelationId, float> OnCooldownStarted;
    }
}
