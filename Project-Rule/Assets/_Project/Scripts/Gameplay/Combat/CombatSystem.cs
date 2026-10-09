using System;
using System.Collections.Generic;
using UnityEngine;
using RuleGame.Infrastructure;
using RuleGame.Infrastructure.Rules;
using RuleGame.Core.Rules;

namespace RuleGame.Gameplay.Combat
{
    /// <summary>
    /// 战斗系统实现：伤害结算 + 命中判定。
    /// </summary>
    /// <remarks>
    /// <para><b>只读规则，不写规则</b>（架构约束）—— 伤害参数从 <see cref="IAttributeManager"/> 读。</para>
    /// <para>伤害结算对玩家与敌人使用<b>同一函数</b>（§5.3）。</para>
    /// </remarks>
    public class CombatSystem : MonoBehaviour, ICombatSystem
    {
        /// <summary>单次命中判定的上限，避免每帧分配。</summary>
        private const int MaxHits = 16;

        private readonly Collider2D[]   _hits       = new Collider2D[MaxHits];
        private readonly List<IDamageable> _hitTargets = new List<IDamageable>();

        public event Action<DamageInfo> OnDamageApplied;
        public event Action<object>     OnEntityDied;

        // ==================================================================
        //  伤害结算
        // ==================================================================

        /// <summary>
        /// 把基础伤害换算为实际伤害：<b>缩放 → 反转 → 失效</b>。
        /// </summary>
        /// <remarks>返回值已含全局伤害参数，<b>尚未扣目标抗性</b>（那是承受方的事）。</remarks>
        public float ResolveAttackAmount(float baseDamage)
        {
            var attrs = RuleSystem.Instance?.Attributes;
            if (attrs == null) return baseDamage;        // 规则系统未就绪时退化为原始值

            var v = attrs.Values;
            if (v.DamageDisabled) return 0f;             // 失效：攻击与回血都无效

            float amount = baseDamage * v.DamageScale;
            return v.DamageInverted ? -amount : amount;  // 反转：正伤害转为回血
        }

        // ==================================================================
        //  命中判定
        // ==================================================================

        /// <summary>
        /// 在指定矩形范围内做一次攻击判定，对命中的每个 <see cref="IDamageable"/> 施加伤害。
        /// </summary>
        /// <returns>实际被施加伤害的目标数。</returns>
        public int PerformAttack(object attacker, Vector2 center, Vector2 size, LayerMask mask,
                                 float baseDamage, float knockback)
        {
            float amount = ResolveAttackAmount(baseDamage);

            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(mask);

            int count = Physics2D.OverlapBox(center, size, 0f, filter, _hits);

            _hitTargets.Clear();
            for (int i = 0; i < count; i++)
            {
                // 一个目标可能有多个碰撞体：用 GetComponentInParent 找，并按目标去重
                var damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || _hitTargets.Contains(damageable)) continue;

                _hitTargets.Add(damageable);
                ApplyDamage(attacker, damageable, amount, knockback);
            }
            return _hitTargets.Count;
        }

        /// <summary>对单个已知目标施加伤害。也供「已知目标」的场合直接调用。</summary>
        public void ApplyDamage(object attacker, IDamageable target, float amount, float knockback)
        {
            var info = new DamageInfo
            {
                Attacker  = attacker,
                Target    = target,
                Amount    = amount,
                Knockback = knockback,
            };

            target.ApplyDamage(info);
            OnDamageApplied?.Invoke(info);
        }

        /// <summary>
        /// 由实体自身在血量归零时调用（其 <see cref="IDamageable"/> 实现内部判断死亡）。
        /// </summary>
        public void NotifyDied(object entity) => OnEntityDied?.Invoke(entity);
    }
}
