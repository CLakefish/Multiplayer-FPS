using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pistol : WeaponBase
{
    public override void Shoot(Vector3 origin, Vector3 dir)
    {
        currentAmmo--;

        if (currentAmmo <= 0) currentAmmo = maxAmmo;

        if (!Physics.Raycast(new Ray(origin, dir), out RaycastHit player, 1000)) return;

        if (player.collider.TryGetComponent<PlayerNetwork>(out PlayerNetwork damage))
        {
            damage.DamageServerRPC(1);
            damage.CheckDeathServerRPC();
        }
    }
}
