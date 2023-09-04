using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Visuals")]
    [SerializeField] public GameObject model;
    internal PlayerNetwork network;
    [SerializeField] public float screenShakeIntensity, screenShakeDuration;

    [Header("Weapon Aiming")]
    [SerializeField] public bool canAim = true;
    [SerializeField] public Vector3 aimPosition, aimRotation;

    [Header("Weapon Parameters")]
    [SerializeField] public int maxAmmo;
    [SerializeField] public int currentAmmo, damage;
    [SerializeField] public float reloadTime, fireRate;

    public abstract void Shoot(Vector3 origin, Vector3 dir);
}

public class PlayerWeaponManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject recoilObject;
    [SerializeField] private TMPro.TMP_Text top, bottom;
    private PlayerMovement p;

    [Header("Weapon")]
    [SerializeField] private WeaponBase weapon;
    [SerializeField] private AnimationCurve walk;
    private float previousTimeFired;

    [Header("Weapon Smoothing")]
    [SerializeField] private Vector3 recoil;
    [SerializeField] private float snapping, returnSpeed;
    private static readonly float smoothTime = 0.09f;
    private Vector3 currentPosition, desiredPosition, desiredRotation;
    private Vector3 currentRotation, targetRotation;
    private float walkTime;
    private Camera c;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        p = GetComponent<PlayerMovement>();
        weapon.network = p.p;

        c = GetComponentInChildren<Camera>();
        UpdateAmmoUI();
    }

    private void Update()
    {
        bool aiming = Input.GetMouseButton(1);

        weapon.transform.localPosition = Vector3.SmoothDamp(weapon.transform.localPosition, desiredPosition, ref currentPosition, smoothTime);
        weapon.transform.localRotation = Quaternion.Lerp(weapon.transform.localRotation, Quaternion.Euler(desiredRotation), Time.deltaTime * 19);
        c.fieldOfView = Mathf.MoveTowards(c.fieldOfView, aiming && weapon.canAim ? 40 : 75, Time.deltaTime * 290);
        
        desiredPosition = Vector3.zero;
        desiredRotation = Vector3.zero;

        Vector2 inputs = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (inputs != new Vector2(0, 0))
        {
            walkTime += Time.deltaTime;
            desiredPosition.y += walk.Evaluate(walkTime) / (aiming ? 2 : 1);
            desiredPosition.x -= walk.Evaluate(walkTime) / (aiming ? 2 : 1) * 0.1f;
        }
        else walkTime = 0;

        if (aiming && weapon.canAim) {
            desiredPosition = weapon.aimPosition;
            desiredRotation = weapon.aimRotation;
        } 

        if (Input.GetMouseButtonDown(0) && Time.time >= previousTimeFired + weapon.fireRate) {
            previousTimeFired = Time.time;
            weapon.Shoot(c.transform.position, p.mainCamera.forward);
            weapon.transform.localPosition += new Vector3(0, 0.05f, Random.Range(-0.05f, 0.05f));
            weapon.transform.localRotation *= Quaternion.Euler(new Vector3(2, 0, 2));
            Recoil();
            UpdateAmmoUI();
        }

        desiredPosition.y += p.rb.velocity.y / 120f;
        desiredPosition.x += inputs.x * (aiming ? 0.04f : 0.09f);
        desiredPosition.x += Input.GetAxis("Mouse X") * (aiming ? 0.04f : 0.09f);

        desiredRotation.x += inputs.y * (4 / (aiming ? 2 : 1));
        desiredRotation.z -= Input.GetAxis("Mouse X") * (4 / (aiming ? 2 : 1));
        desiredRotation.z -= inputs.x * (5 / (aiming ? 2 : 1));

        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snapping * Time.fixedDeltaTime);
        recoilObject.transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void Recoil() => targetRotation += new Vector3(Random.Range(-recoil.x, recoil.x),  recoil.y, Random.Range(-recoil.z, recoil.z));
    private void UpdateAmmoUI() => top.text = bottom.text = weapon.currentAmmo.ToString() + " / " + weapon.maxAmmo.ToString();
}
