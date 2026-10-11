using UnityEngine;

namespace RuleGame.Gameplay.Combat
{
    /// <summary>
    /// 能被击退推动的实体（玩家与敌人各自实现）。
    /// </summary>
    /// <remarks>
    /// <para>把冲量交给<b>移动的持有者</b>，而不是直接写 <c>Rigidbody2D.linearVelocity</c>：
    /// Kinematic 角色自己维护速度、每个物理帧整体覆盖，直接写进去的冲量活不过一帧。
    /// 由持有者把它并入自己的位移，衰减节奏也归它管。</para>
    /// </remarks>
    public interface IKnockbackReceiver
    {
        /// <summary>施加一次外部冲量（单位：米/秒）。</summary>
        void AddImpulse(Vector2 impulse);
    }
}
