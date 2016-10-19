using UnityEngine;
using System.Collections;
using System;

// Order.cs

//
// Defines an Order, which is an in-game action that can be carried out by a Unit. The interpretation of an Order
// is left up to the Unit itself.
//

public abstract class Order {
    abstract public OrderResult Execute(Unit executor);
    virtual public OrderResult CanExecute(Unit executor) {
        if (!executor.IsAlive())
            return OrderResult.NotCapable;
        return OrderResult.OK;
    }

    // To be called when the executor of this order was rendered incapable of complying with this order.
    protected void Fail(Unit executor, OrderResult reason) {
        executor.ClearCurrentOrder();
    }

    public enum OrderResult {
        OK,                 // Order accepted; Unit will attempt to comply
        NotOwned,           // Unit is not owned by the Player issuing the order
        NotCapable,         // Unit is physically incapable of executing the Order
        NoPath,             // Unit cannot reach the target point
        InsufficientMana,   // Unit does not have enough mana to use an Ability required by the Order
        OutOfRange,         // Unit is out of range of the target
        AbilityOnCooldown,  // The Ability required by the Order is still on cooldown
        Unknown             // The status of the order is unknown
    };

    public OrderResult AbilityResultToOrderResult(Ability.AbilityResult result) {
        switch (result) {
            case Ability.AbilityResult.OK:                  return OrderResult.OK;
            case Ability.AbilityResult.OutOfRange:          return OrderResult.OutOfRange;
            case Ability.AbilityResult.InsufficientMana:    return OrderResult.InsufficientMana;
            case Ability.AbilityResult.AbilityOnCooldown:   return OrderResult.AbilityOnCooldown;
            default:                                        return OrderResult.Unknown;
        }
    }

    public OrderResult EvaluateAbilityOrder(Unit executor, Ability ability) {
        if (executor.Mana < ability.GetManaCost())
            return OrderResult.InsufficientMana;
        return OrderResult.OK;
    }

    protected void ResolveAbilityFailure(Unit executor, Ability failingAbility, Ability.AbilityResult failureCondition, Vector3 locationOfTarget) {
        if (failureCondition == Ability.AbilityResult.OK)
            return; // The ability activated successfully; nothing to be done.

        // If we failed to activate the Ability because we're out of range but we're capable
        // of moving closer to the target, move toward the target and retry.
        if (failureCondition == Ability.AbilityResult.OutOfRange) {
            if (executor.CanReach(locationOfTarget, failingAbility.GetRange()))
                executor.MoveToward(locationOfTarget);
            else
                Fail(executor, AbilityResultToOrderResult(failureCondition)); // If we can't move closer, give up.
        }
        else { // Ability failed for some reason other than range; nothing we can do about that.
            Fail(executor, AbilityResultToOrderResult(failureCondition));
        }
    }
};

// Move to a target point.
public class MoveOrder : Order {
    public MoveOrder(Vector3 targetPosition) {
        m_targetPosition = targetPosition;
    }
    Vector3 m_targetPosition;

    public override OrderResult Execute(Unit executor) {
        if (!executor.CanMove) {
            Fail(executor, OrderResult.NotCapable);
            return OrderResult.NotCapable;
        }
        if (executor.IsWithinStoppingDistanceOfPoint(m_targetPosition)) {
            // We've arrived; time to stop.
            executor.ClearCurrentOrder();
            return OrderResult.OK;
        }
        executor.MoveToward(m_targetPosition);
        return OrderResult.OK;
    }

    public override OrderResult CanExecute(Unit executor) {
        if (!executor.CanMove)
            return OrderResult.NotCapable;
        if (!executor.CanReach(m_targetPosition, 5.0f))
            return OrderResult.NoPath;
        return base.CanExecute(executor);
    }
};

// Clear the current order.
public class StopOrder : Order {
    public override OrderResult Execute(Unit executor) {
        executor.ClearCurrentOrder();
        return OrderResult.OK;
    }

    public override OrderResult CanExecute(Unit executor) {
        return OrderResult.OK; // This Order is always executable and cannot fail.
    }
};

// Attack another Unit.
public class AttackOrder : Order {
    public AttackOrder(Unit target) {
        m_target = target;
    }
    Unit m_target;

    public override OrderResult Execute(Unit executor) {
        if (!executor.CanAttack(m_target)) {
            Fail(executor, OrderResult.NotCapable);
            return OrderResult.NotCapable;
        }
        executor.Attack(m_target);
        return OrderResult.OK;
    }

    public override OrderResult CanExecute(Unit executor) {
        if (!executor.CanAttack(m_target) || m_target == executor)
            return OrderResult.NotCapable;
        return base.CanExecute(executor);
    }
};

// Use one of the unit's special abilities.
public class UseUntargetedAbilityOrder : Order {
    public UseUntargetedAbilityOrder(UntargetedAbility ability) {
        m_ability = ability;
    }
    UntargetedAbility m_ability;

    public override OrderResult Execute(Unit executor) {            
        Ability.AbilityResult result = executor.UseUntargetedAbility(m_ability);
        // We'll clear the Order regardless of whether it failed or succeeded because, with
        // untargeted Abilities, there is nothing the Unit can do by itself to fix the failure condition.
        Fail(executor, AbilityResultToOrderResult(result));
        return AbilityResultToOrderResult(result);
    }

    public override OrderResult CanExecute(Unit executor) {
        if (executor.Mana < m_ability.GetManaCost())
            return OrderResult.InsufficientMana;
        return base.CanExecute(executor);
    }
};

public class UsePointTargetedAbilityOrder : Order {
    public UsePointTargetedAbilityOrder(PointTargetedAbility ability, Vector3 targetPoint) {
        m_ability = ability;
        m_targetPoint = targetPoint;
    }
    PointTargetedAbility m_ability;
    Vector3 m_targetPoint;

    public override OrderResult Execute(Unit executor) {
        Ability.AbilityResult result = executor.UsePointTargetedAbility(m_ability, m_targetPoint);
        if (result != Ability.AbilityResult.OK) {
            ResolveAbilityFailure(executor, m_ability, result, m_targetPoint);
        }
        return AbilityResultToOrderResult(result);
    }

    public override OrderResult CanExecute(Unit executor) {
        OrderResult abilityCheck = EvaluateAbilityOrder(executor, m_ability);
        if (abilityCheck != OrderResult.OK)
            return abilityCheck;
        if (!executor.CanReach(m_targetPoint, m_ability.GetRange()))
            return OrderResult.NoPath;
        return base.CanExecute(executor);
    }
}

public class UseUnitTargetedAbilityOrder : Order {
    public UseUnitTargetedAbilityOrder(UnitTargetedAbility ability, Unit target) {
        m_ability = ability;
        m_targetUnit = target;
    }
    UnitTargetedAbility m_ability;
    Unit m_targetUnit;

    public override OrderResult Execute(Unit executor) {
        Ability.AbilityResult result = executor.UseUnitTargetedAbility(m_ability, m_targetUnit);
        if (result != Ability.AbilityResult.OK) {
            ResolveAbilityFailure(executor, m_ability, result, m_targetUnit.Position);
        }
        return AbilityResultToOrderResult(result);
    }

    public override OrderResult CanExecute(Unit executor) {
        OrderResult abilityCheck = EvaluateAbilityOrder(executor, m_ability);
        if (abilityCheck != OrderResult.OK)
            return abilityCheck;
        if (!executor.CanReach(m_targetUnit.Position, m_ability.GetRange()))
            return OrderResult.NoPath;
        return base.CanExecute(executor);
    }
}