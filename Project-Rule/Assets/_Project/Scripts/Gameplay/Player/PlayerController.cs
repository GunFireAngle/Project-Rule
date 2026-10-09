using UnityEngine;
using UnityEngine.InputSystem;
using RuleGame.Core.Rules;
using RuleGame.Gameplay.Combat;

namespace RuleGame.Gameplay.Player
{
    /// <summary>
    /// 玩家移动控制。用 Dynamic 刚体 + <c>gravityScale = 0</c>。
    /// </summary>
    /// <remarks>
    /// <para><b>为什么不用 Kinematic</b>：官方文档确认 Kinematic 与静态地形之间<b>没有碰撞响应</b>（会穿透），
    /// 会失去地面 / 墙体阻挡。改用 Dynamic 后碰撞响应免费，同时把 <c>gravityScale</c> 置 0、
    /// 自己读 <c>Physics2D.gravity</c> 施加重力，<b>移动仍完全由代码控制</b>（原 D1 的核心诉求）。</para>
    /// <para>好处：重力规则改的是 <c>Physics2D.gravity</c> 这个全局值，<b>本类无需改一行</b>就会跟着变——
    /// 「规则作用于世界本身」就是这样落地的。</para>
    /// <para>摩擦同理：自己读 <c>IAttributeManager.Values.FrictionScale</c> 应用（不是碰撞体材质摩擦）。</para>
    /// <para>物理运算写在 <see cref="FixedUpdate"/>（Unity 2D 物理要求）。</para>
    /// </remarks>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // ==================================================================
        //  序列化字段
        // ==================================================================

        [Header("输入")]
        [Tooltip("拖入 Assets/_Project/Settings/InputSystem_Actions.inputactions")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("移动")]
        [SerializeField] private float _moveSpeed    = 8f;
        [SerializeField] private float _jumpSpeed    = 14f;
        [SerializeField] private float _gravityScale = 1f;

        [Tooltip("无输入时的水平减速度；会被 FrictionScale 缩放（摩擦失效 ≈ 冰面）。")]
        [SerializeField] private float _frictionDecel = 40f;

        [Header("地面检测")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float     _groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask _groundLayer;

        [Header("攻击")]
        [SerializeField] private CombatSystem _combat;
        [SerializeField] private float     _attackDamage    = 10f;
        [SerializeField] private float     _attackKnockback = 6f;
        [SerializeField] private Vector2   _attackBoxSize   = new Vector2(1.2f, 1f);
        [SerializeField] private float     _attackOffset    = 0.8f;
        [SerializeField] private LayerMask _attackMask;

        // ==================================================================
        //  运行时状态
        // ==================================================================

        private Rigidbody2D _rb;
        private Vector2 _velocity;
        private Vector2 _moveInput;
        private bool    _jumpQueued;
        private bool    _grounded;
        private float   _facing = 1f;

        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;

        // ==================================================================
        //  生命周期
        // ==================================================================

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            // Dynamic + gravityScale=0：碰撞响应交给引擎，重力自己算（见 FixedUpdate）
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 0f;
            _rb.linearDamping = 0f;                                            // 摩擦自己算，别叠引擎阻尼
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;          // 平台角色不翻滚
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 防高速穿透薄地形

            if (_inputActions != null)
            {
                _moveAction   = _inputActions.FindAction("Player/Move",   false);
                _jumpAction   = _inputActions.FindAction("Player/Jump",   false);
                _attackAction = _inputActions.FindAction("Player/Attack", false);
            }

            if (_moveAction == null)
                Debug.LogWarning("[PlayerController] 未找到输入动作，请把 InputSystem_Actions 拖到 _inputActions。", this);
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _jumpAction?.Enable();
            _attackAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
            _attackAction?.Disable();
        }

        // ==================================================================
        //  输入（视觉帧）
        // ==================================================================

        private void Update()
        {
            _moveInput = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

            if (_jumpAction != null && _jumpAction.WasPressedThisFrame())
                _jumpQueued = true;

            if (_attackAction != null && _attackAction.WasPressedThisFrame())
                DoAttack();
        }

        // ==================================================================
        //  物理（固定帧）
        // ==================================================================

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            // ① 重力：自己读全局值（gravityScale=0，引擎不会自动施加）
            _velocity += Physics2D.gravity * _gravityScale * dt;

            // ② 水平：有输入直接给速度，无输入按摩擦力减速
            if (Mathf.Abs(_moveInput.x) > 0.01f)
            {
                _velocity.x = _moveInput.x * _moveSpeed;
                _facing = Mathf.Sign(_moveInput.x);
            }
            else
            {
                float friction = Mathf.Max(0.01f, FrictionScale);   // 0 = 无摩擦
                _velocity.x = Mathf.MoveTowards(_velocity.x, 0f, _frictionDecel * friction * dt);
            }

            // ③ 地面检测 + 跳跃
            _grounded = _groundCheck != null &&
                        Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);

            if (_grounded && _velocity.y < 0f)
                _velocity.y = 0f;                    // 贴地：抵消重力累积

            if (_jumpQueued)
            {
                if (_grounded) _velocity.y = _jumpSpeed;
                _jumpQueued = false;
            }

            // ④ 写回刚体
            _rb.linearVelocity = _velocity;
        }

        // ==================================================================
        //  攻击
        // ==================================================================

        private void DoAttack()
        {
            if (_combat == null) return;

            Vector2 center = (Vector2)transform.position + new Vector2(_facing * _attackOffset, 0f);
            _combat.PerformAttack(this, center, _attackBoxSize, _attackMask, _attackDamage, _attackKnockback);
        }

        // ==================================================================
        //  属性读取
        // ==================================================================

        /// <summary>当前摩擦缩放（1.0 = 正常）。规则系统未就绪时退化为 1。</summary>
        private static float FrictionScale
        {
            get
            {
                var attrs = RuleSystem.Instance?.Attributes;
                return attrs?.Values.FrictionScale ?? 1f;
            }
        }
    }
}
