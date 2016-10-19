using UnityEngine;
using System.Collections;

public class TestShip : Unit {
    protected override void Init() {
        AddWeapon(new TestWeapon(this));
        m_maximumHealth = 100;
        m_maximumMana = 25;

        m_maximumVelocity = 5.0f;
        m_maximumAngularVelocity = 90.0f;
        m_acceleration = 1.25f;
        m_angularAcceleration = 30f;
    }
}
