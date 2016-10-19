using UnityEngine;
using System.Collections;

// Weapon.cs

//
// Defines a Weapon, which is used by a Unit to attack other Units.
//

public abstract class Weapon {
    public float m_maximumCooldown { get; protected set; } // The time, in seconds, which must pass between consecutive shots from this Weapon.
    public float m_range { get; protected set; } // The default maximum distance to which this Weapon can fire.

    public float m_cooldown { get; protected set; } // The time remaining, in seconds, which must pass before this weapon can fire.
    public Unit m_owner { get; protected set; } // The Unit to which this Weapon is attached.

    public Weapon(Unit owner) {
        m_owner = owner;
    }

    public virtual void OnUpdate() {
        m_cooldown -= Time.deltaTime;
    }

    public void TryAttack(Unit target) {
        if (CanAttackImmediately(target)) {
            Attack(target);
            ApplyCooldown();
        }
    }
    // Attempt to attack the target unit.
    protected abstract void Attack(Unit target);

    // Returns whether the Weapon is capable of attacking the target unit, 
    // discounting situational factors such as distance to the target.
    public virtual bool CanAttack(Unit target) {
        return true;
    }

    // Returns whether the Weapon is capable of attacking the target unit at this very moment.
    public virtual bool CanAttackImmediately(Unit target) {
        if (!CanAttack(target) ||
            !IsCooldownExpired() ||
            !IsInRange(target))
        {
            return false;
        }
        return true;
    }

    public float GetDistanceToTarget(Unit target) {
        return Vector2.Distance(m_owner.Position, target.Position);
    }

    public float GetDistanceToTarget(Vector2 target) {
        return Vector2.Distance(m_owner.Position, target);
    }

    public bool IsInRange(Unit target) {
        return GetDistanceToTarget(target) < GetRange();
    }

    public bool IsInRange(Vector2 target) {
        return GetDistanceToTarget(target) < GetRange();
    }

    public bool IsCooldownExpired() {
        return m_cooldown <= 0;
    }

    protected void ApplyCooldown() {
        float cooldown = m_maximumCooldown;
        // apply cooldown modifiers here
        m_cooldown = cooldown;
    }

    // Returns the actual range of the weapon after modifiers.
    public virtual float GetRange() {
        float range = m_range;
        // apply modifiers here
        return range;
    }
}
