using UnityEngine;
using System.Collections;
using System;

public class TestWeapon : Weapon {
    public TestWeapon(Unit owner) : base(owner) {
        m_maximumCooldown = 1;
        m_range = 5;
    }

    protected override void Attack(Unit target) {
        target.TakeDamage(5);

    }
}
