using UnityEngine;
using System.Collections;

// Ability.cs

//
// Defines an Ability, which is a special action unique to a certain type of Unit or certain types of Units.
//

public abstract class Ability {
    public const float s_maximumCooldown = 0; // The time, in seconds, which a Unit must wait between consecutive uses of this Ability.
    public const float s_manaCost = 0; // The amount of mana which is required to use this Ability and which is consumed upon using this Ability.
    public const float s_range = 0; // The maximum distance at which this Ability can be used.

    public float m_cooldown { get; protected set; } // The time remaining, in seconds, which a Unit must wait before it can use this Ability.

    // Returns the range of the Ability after modifiers.
    public virtual float GetRange() {
        float range = s_range;
        // apply modifiers here
        return range;
    }

    public virtual float GetManaCost() {
        float manaCost = s_manaCost;
        // apply modifiers here
        return manaCost;
    }

    public enum AbilityResult {
        OK,                 // Ability was successfully activated.
        OutOfRange,         // Ability could not be activated because the activator was out of range of the target.
        InsufficientMana,   // Activator did not have enough mana to activate the Ability.
        AbilityOnCooldown   // The Ability's cooldown has not yet expired.
    }
};

// An UntargetedAbility is an Ability which does not require a target.
public abstract class UntargetedAbility : Ability {
    public abstract AbilityResult Activate(Unit activator);
}

// A PointTargetedAbility is an Ability which requires that the Player using the Ability select a point in the game world as a target.
public abstract class PointTargetedAbility : Ability {
    public abstract AbilityResult Activate(Unit activator, Vector3 target);
};

// A UnitTargetedAbility is an Ability which requires that the Player using the Ability select a Unit as a target.
// Some UnitTargetedAbility may only target certain types of Units or Units possessing a certain attribute
// such as being hostile.
public abstract class UnitTargetedAbility : Ability {
    public abstract AbilityResult Activate(Unit activator, Unit target);
}