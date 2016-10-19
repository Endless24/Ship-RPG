using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// Unit.cs

//
// Defines a Unit, which is an entity in the game world capable of receiving orders and affecting the game state.
//

public abstract class Unit : MonoBehaviour {
    public static ReadOnlyCollection<Unit> Units {
        get { return s_units.AsReadOnly(); }
    }

    public ReadOnlyCollection<Weapon> Weapons {
        get { return m_weapons.AsReadOnly(); }
    }

    // The maximum amount of health this Unit can have.
    public float MaximumHealth {
        get { return m_maximumHealth; }
    }

    // The maximum amount of mana this Unit can have.
    public float MaximumMana {
        get { return m_maximumMana; }
    }

    // Health is a measure of the remaining durability of a Unit. If a Unit's health reaches 0, the unit is destroyed.
    public float Health {
        get { return m_health; }
    }

    // Mana is a regenerating resource which is spent to activate certain special Abilities.
    public float Mana {
        get { return m_mana; }
    }

    // Whether the unit currently has the ability to move.
    public bool CanMove {
        get { return m_maximumVelocity > 0 && m_acceleration > 0; }
    }

    // The current speed and direction, in units per second, at which this Unit is traveling.
    public Vector3 Velocity {
        get { return m_velocity; }
    }

    // Maximum magnitude of velocity, in units per second, which this Unit can travel.
    public float MaximumVelocity {
        get { return m_maximumVelocity; }
    }

    // Acceleration, in units per second squared, of this Unit.
    public float Acceleration {
        get { return m_acceleration; }
    }

    // Whether this Unit is currently selected by the local player.
    public bool Selected {
        get { return m_selected; }
    }

    // Returns the greatest extent of the Unit's mesh in any of the three axes.
    public float GreatestExtent {
        get {
            Vector3 extents = m_mesh.bounds.extents;
            List<float> axes = new List<float>();
            axes.Add(extents.x);
            axes.Add(extents.y);
            axes.Add(extents.z);

            float largest = 0;
            foreach(float axis in axes) {
                if (axis > largest)
                    largest = axis;
            }
            return largest;
        }
    }

    public Vector3 Position { get { return gameObject.transform.position; } }

    private static List<Unit> s_units = new List<Unit>();

    protected float m_maximumHealth = 100;
    protected float m_maximumMana = 25;
    protected float m_maximumVelocity = 2.0f; // In units per second
    protected float m_maximumAngularVelocity = 15; // In degrees per second
    protected float m_acceleration = 0.25f; // In units per second squared
    protected float m_angularAcceleration = 2.5f; // In degrees per second squared
    // Used in course correction calculations. Lower values will cause the ship to make less of an attempt
    // to correct its course; higher values will do the opposite.
    protected float m_proportionalityConstant = 0.25f; 

    protected float m_health;
    protected float m_mana;

    protected Order m_currentOrder;
    // If this Unit is capable of executing the Order specified here, it will replace the
    // current Order on the next update.
    protected Order m_newOrder; 
    protected List<Weapon> m_weapons = new List<Weapon>();
    private PercentageBar m_healthbar;
    private PercentageBar m_manabar;
    private Vector3 m_velocity;
    private float m_angularVelocity;
    private bool m_isRespondingToMoveOrder;
    private Player m_owner;
    private Mesh m_mesh;
    private bool m_selected;

    void Start() {
        s_units.Add(this);
        m_mesh = GetComponent<MeshFilter>().mesh;
        Init();
        m_health = m_maximumHealth;
        m_mana = m_maximumMana;

        m_healthbar = gameObject.AddComponent<PercentageBar>();
        m_healthbar.Init(m_maximumHealth, m_health, drawAbove: false, offset: new Vector2(0, 0));
        m_manabar = gameObject.AddComponent<PercentageBar>();
        m_manabar.Init(m_healthbar, m_maximumMana, m_mana, drawAbove: false, emptyColor: Color.gray, filledColor: Color.blue, offset: new Vector2(0, 1));
        SetOwnership(null);
    }

    void Update() {
        if (m_health <= 0) {
            Die();
            return;
        }
        m_healthbar.Maximum = m_maximumHealth;
        m_healthbar.Value = m_health;
        if (m_owner != null) {
            m_healthbar.FilledColor = m_owner.IsMe ? Color.green : Color.red;
        }
        else {
            m_healthbar.FilledColor = Color.red;
        }
        if (m_selected)
            m_healthbar.FilledColor = Color.yellow;
        m_manabar.Maximum = m_maximumMana;
        m_manabar.Value = m_mana;
        m_isRespondingToMoveOrder = false;
        OnUpdate();
        foreach (Weapon weapon in m_weapons) {
            weapon.OnUpdate();
        }
        ExecuteOrder();
        Move();
    }

    protected void Die() {
        enabled = false;
        s_units.Remove(this);
        Destroy(gameObject);
        Destroy(this);
    }

    protected virtual void Init() { }

    protected virtual void OnUpdate() { }

    public void SetOwnership(Player newOwner) {
        m_owner = newOwner;
    }

    // Sets the current Order to the passed Order. Returns an OrderResult which represents
    // whether the unit is capable of carrying out the new Order. If the Unit is capable of
    // carrying out the Order, the current Order will be replaced by the new Order on the next
    // update. If not, the new Order will be canceled on the next update.
    public Order.OrderResult IssueOrder(Order newOrder) {
        Order.OrderResult result = newOrder.CanExecute(this);
        m_newOrder = newOrder;
        return result;
    }

    protected void ExecuteOrder() {
        if(m_newOrder != null) {
            if (m_newOrder.CanExecute(this) == Order.OrderResult.OK)
                m_currentOrder = m_newOrder;
            m_newOrder = null;
        }
        if (m_currentOrder == null)
            AttackClosestEnemyUnit();
        else
            m_currentOrder.Execute(this);
    }

    protected void ExecuteOrder(Order overrideOrder) {
        overrideOrder.Execute(this);
    }

    protected virtual void AttackClosestEnemyUnit() {
        Unit nearest = GetClosestEnemyUnit(this);
        if (!nearest)
            return;
        foreach (Weapon weapon in m_weapons) {
            if (weapon.IsInRange(nearest))
                IssueOrder(new AttackOrder(nearest));
        }
    }

    // Returns true if the Unit can find a way of getting within (distance) units of (position).
    public virtual bool CanReach(Vector3 position, float distance) {
        if (!CanMove && Vector3.Distance(position, Position) > distance)
            return false;
        return true;
    }

    Vector3 VertexToScreenPoint(Vector3 vertex) {
        return Camera.main.WorldToScreenPoint(gameObject.transform.TransformPoint(vertex));
    }

    // Returns true if the Unit possesses at least one vertex within the passed rectangle.
    public bool VertexWithinRectangle(Rect rectangle) {
        float minX = rectangle.x;
        float minY = rectangle.y;
        float maxX = rectangle.x + rectangle.width;
        float maxY = rectangle.y + rectangle.height;
        if (minX > maxX)
            Utility.Swap(ref minX, ref maxX);
        if (minY > maxY)
            Utility.Swap(ref minY, ref maxY);

        foreach (Vector3 vertex in m_mesh.vertices) {
            Vector3 screenpos = VertexToScreenPoint(vertex);

            if (screenpos.x > minX && screenpos.x < maxX &&
                screenpos.y > minY && screenpos.y < maxY) 
            {
                return true;
            }
        }
        return false;
    }

    // Causes the Unit to move toward the target point. By default, a Unit does so by pointing itself at the
    // target and accelerating.
    public virtual void MoveToward(Vector3 targetPoint) {
        m_isRespondingToMoveOrder = true;
        targetPoint.y = gameObject.transform.position.y;
        TurnToward(targetPoint);
        Debug.DrawRay(gameObject.transform.position, targetPoint - gameObject.transform.position);
    }

    // This function returns the angle, in degrees, between two unit vectors as you would view them looking down from above.
    // Aside from only considering the XZ plane, this function differs from Vector3.Angle(Vector3, Vector3) in that it returns
    // a signed angle; Vector3.Angle() returns the absolute value of the angle.
    float AngleBetween2DVectors(Vector3 a, Vector3 b) {
        float angle = Mathf.Rad2Deg * (Mathf.Atan2(b.z, b.x) - Mathf.Atan2(a.z, a.x));
        if (Mathf.Abs(angle) > 180)
            return (360 - Mathf.Abs(angle)) * (angle < 0 ? 1 : -1);
        return angle;
    }

    // Causes the Unit to turn toward the specified point. By default, the Unit's turn rate depends on its current velocity.
    // As such, a Unit that is not moving cannot turn. When turning toward a target point, a Unit will automatically turn
    // beyond the target point as necessary to correct its course. Pass (adjustCourse = false) to disable this behavior.
    public virtual void TurnToward(Vector3 targetPoint, bool adjustCourse = true) {
        Vector3 forward = gameObject.transform.forward;
        Vector3 directionOfTravel = m_velocity.normalized;
        Vector3 target = (targetPoint - gameObject.transform.position).normalized;
        float angularDifference;
        if (adjustCourse) {
            // Get the difference between our current direction of travel and our intended direction of travel.
            angularDifference = AngleBetween2DVectors(directionOfTravel, target);
            // Adjust beyond the target vector to bring our momentum in line with our intended path.
            angularDifference += angularDifference * m_proportionalityConstant;
            // Get the difference between our current facing and that new vector and then proceed as normal.
            angularDifference = angularDifference - AngleBetween2DVectors(directionOfTravel, forward);
        }
        else {
            angularDifference = AngleBetween2DVectors(forward, target);
        }
        int sign = angularDifference < 0 ? 1 : -1;
        // Distance formula: d = (1/2)at^2 + vt, applied to angular velocity here
        float timeToStop = Mathf.Abs(m_angularVelocity / m_angularAcceleration);
        float degreesRotatedWhileStopping = ((0.5f) * (-m_angularAcceleration * Mathf.Pow(timeToStop, 2))) + (Mathf.Abs(m_angularVelocity) * timeToStop);
        if(Mathf.Abs(angularDifference) <= degreesRotatedWhileStopping) {
            m_angularVelocity = Mathf.Max(Mathf.Abs(m_angularVelocity) - m_angularAcceleration, 0) * sign; 
        }
        else {
            m_angularVelocity = m_angularVelocity + (m_angularAcceleration * sign);
            if (m_angularVelocity > m_maximumAngularVelocity)
                m_angularVelocity = m_maximumAngularVelocity * (m_angularVelocity > 0 ? 1 : -1);
        }

        // Apply our angular velocity to our facing.
        gameObject.transform.Rotate(new Vector3(0, m_angularVelocity * Time.deltaTime, 0));
    }

    // Applies the Unit's velocity to its position.
    private void Move() {
        Accelerate();
        gameObject.transform.position += m_velocity * Time.deltaTime;
    }

    // Returns true if, given that the Unit starts slowing down immediately, the Unit would stop at or beyond the target point.
    public virtual bool IsWithinStoppingDistanceOfPoint(Vector3 point) {
        float distance = Vector3.Distance(gameObject.transform.position, point);
        float timeToStop = m_velocity.magnitude / m_acceleration;
        // Distance formula: d = (1/2)at^2 + vt, noting that acceleration in this situation is negative.
        float distanceTraveledWhileStopping = ((-m_acceleration * (Mathf.Pow(timeToStop, 2))) * 0.5f) + (m_velocity.magnitude * timeToStop);
        if (distanceTraveledWhileStopping > distance)
            return true;
        return false;
    }

    // Returns whether the Unit has any Weapon capable of attacking the target Unit. Note that this is not a query of whether this
    // Unit can attack the target at this very moment - only whether it is capable of executing the order at all, under any circumstances.
    public virtual bool CanAttack(Unit targetUnit) {
        foreach (Weapon weapon in m_weapons) {
            if (weapon.CanAttack(targetUnit)) {
                return true;
            }
        }

        return false;
    }

    public virtual void Attack(Unit targetUnit) {
        if (!targetUnit.IsAlive()) // If our target is dead, don't continue trying to attack it.
            ClearCurrentOrder();

        // Cycle through our Weapons, attempting to fire each one at our target.
        foreach (Weapon weapon in m_weapons) {
            weapon.TryAttack(targetUnit);
        }
    }

    public Ability.AbilityResult UseUntargetedAbility(UntargetedAbility ability) {
        return ability.Activate(this);
    }

    public Ability.AbilityResult UsePointTargetedAbility(PointTargetedAbility ability, Vector3 target) {
        return ability.Activate(this, target);
    }

    public Ability.AbilityResult UseUnitTargetedAbility(UnitTargetedAbility ability, Unit target) {
        return ability.Activate(this, target);
    }

    public void ClearCurrentOrder() {
        m_currentOrder = null;
    }

    // Call this when changes are made to the local Player's selection.
    public void SelectionChanged() {
        m_selected = PlayerManager.Manager.Me.SelectedUnits.Contains(this);
    }

    // Reduces the health of the Unit, subject to damage modifiers. Pass (trueDamage = true) to ignore damage modifiers.
    // Returns the damage actually taken after modifiers.
    public float TakeDamage(float amount, bool trueDamage = false) {
        if (!trueDamage) {
            // modify damage here
        }

        m_health -= amount;
        return amount;
    }

    // Restores health to the Unit, subject to healing modifiers, up to the Unit's maximum health. Pass (trueHealing = true) to ignore healing modifiers.
    // Pass (allowOverhealing = true) to allow increasing the Unit's health above its maximum.
    // Returns the amount actually healed after modifiers.
    public float TakeHealing(float amount, bool trueHealing = false, bool allowOverhealing = false) {
        if (!trueHealing) {
            // modify healing here
        }

        if (!allowOverhealing) {
            float healthLost = m_health - m_maximumHealth;
            amount = Mathf.Min(amount, healthLost);
        }

        m_health += amount;
        return amount;
    }

    // Consumes the Unit's mana, subject to mana consumption modifiers. Pass (trueConsumption = true) to ignore mana consumption modifiers.
    // Pass (allowNegativeMana = true) to allow consuming more mana than the Unit has.
    // Returns the amount of mana actually consumed after modifiers.
    public float ConsumeMana(float amount, bool trueConsumption = false, bool allowNegativeMana = false) {
        if (!trueConsumption) {
            // modify consumption here
        }

        if (!allowNegativeMana) {
            amount = Mathf.Max(m_mana, amount);
        }

        m_mana -= amount;
        return amount;
    }

    // Modifies and returns the passed vector such that the returned vector has magnitude newMagnitude without
    // changing the ratio of the three axes.
    private Vector3 SetMagnitude(Vector3 vec, float newMagnitude) {
        vec.Normalize();
        return vec * newMagnitude;
    }

    // Increases the Unit's velocity by its acceleration in the direction it is currently facing.
    private void Accelerate() {
        // Apply friction to the extent that we're not moving in the direction that we're facing.
        float angularDifference = Vector3.Angle(gameObject.transform.forward, m_velocity);
        float percentNotMovingInTheFacedDirection = angularDifference / 180;
        Vector3 friction = -m_velocity;
        friction = SetMagnitude(friction, m_acceleration * percentNotMovingInTheFacedDirection * 0.5f);
        m_velocity += friction * Time.deltaTime;

        Vector3 addedVelocity = gameObject.transform.TransformVector(new Vector3(0, 0, m_acceleration)) * Time.deltaTime;
        if (m_isRespondingToMoveOrder) {
            if (m_velocity.magnitude < m_maximumVelocity) {
                m_velocity += addedVelocity;
                if (m_velocity.magnitude > m_maximumVelocity) {
                    m_velocity = SetMagnitude(m_velocity, m_maximumVelocity);
                }
            }
            else {
                m_velocity -= addedVelocity;
            }
        }
        else {
            m_velocity = SetMagnitude(m_velocity, Mathf.Max(m_velocity.magnitude - (m_acceleration * Time.deltaTime), 0));
        }
    }

    // Increases the Unit's velocity in the directions and amounts specified in the velocityDelta parameter.
    // If doing so would increase the magnitude of the Unit's velocity above its maximum, the amount of velocity
    // in excess of the maximum is applied logarithmically to ensure that the Unit cannot drastically exceed
    // its normal maximum velocity. Pass (forceLinear = true) to disable this behavior.
    public void ApplyVelocity(Vector3 velocityDelta, bool forceLinear) {
        m_velocity += velocityDelta;
        if (!forceLinear && m_velocity.magnitude > m_maximumVelocity) {
            float excess = m_velocity.magnitude - m_maximumVelocity;
            float excessPercentage = excess / m_maximumVelocity;
            float scaledExcessPercentage = Mathf.Log10(excessPercentage + 1);
            m_velocity *= scaledExcessPercentage;
        }
    }

    // Restores mana to the Unit, subject to mana restoration modifiers. Pass (trueRestoration = true) to ignore mana restoration modifiers.
    // Pass (allowManaOverMaximum = true) to allow increasing the Unit's health above its maximum.
    public float RestoreMana(float amount, bool trueRestoration = false, bool allowManaOverMaximum = false) {
        if (!trueRestoration) {
            // modify restoration here
        }

        if (!allowManaOverMaximum) {
            float manaLost = m_mana - m_maximumMana;
            amount = Mathf.Min(amount, manaLost);
        }

        m_mana += amount;
        return amount;
    }

    public bool IsAlive() {
        return m_health > 0;
    }

    public void AddWeapon(Weapon weapon) {
        m_weapons.Add(weapon);
    }

    public void RemoveWeapon(int index) {
        m_weapons.RemoveAt(index);
    }

    public void RemoveWeapon(Weapon weapon) {
        m_weapons.Remove(weapon);
    }

    // Get the distance from this Unit to the passed Unit.
    public float GetDistance(Unit unit) {
        return GetDistance(unit.Position);
    }

    // Get the distance from this Unit to the passed point.
    public float GetDistance(Vector3 toThisPoint) {
        return Vector3.Distance(Position, toThisPoint);
    }

    // Gets the closest enemy Unit to the passed Unit.
    public static Unit GetClosestEnemyUnit(Unit unit) {
        return GetClosestUnit(unit.Position, filter: x => x.m_owner != unit.m_owner);
    }

    // Gets the closest Unit to the passed Unit, not counting the unit itself.
    public static Unit GetClosestUnit(Unit unit) {
        return GetClosestUnit(unit.Position, x => x != unit);
    }

    // Gets the closest Unit to the passed point which passes the filter.
    public static Unit GetClosestUnit(Vector3 toThisPoint, System.Predicate<Unit> filter = null) {
        Unit closest = null;
        float currentDistance = 0;
        foreach (Unit unit in s_units) {
            // Ignore any unit which does not match the user-provided predicate.
            if (filter != null && !filter(unit))
                continue;
            float dist = unit.GetDistance(toThisPoint);
            if (closest == null || dist < currentDistance) {
                closest = unit;
                currentDistance = dist;
            }
        }
        return closest;
    }
}
