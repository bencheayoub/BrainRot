using UnityEngine;

// Brings the character to life with code only (no animation files needed):
// tilt while moving, falling wobble, reactions to fruits, and a funny idle per character.
// Put this on the Player object. It moves the MODEL (a child), never the Player itself,
// so movement and collisions are not affected.
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to find it automatically")]
    [SerializeField] private CharacterSpawner spawner;

    [Header("Tilt while moving")]
    [Tooltip("Copy the Move Speed of PlayerController here")]
    [SerializeField] private float referenceSpeed = 6f;
    [SerializeField] private float tiltAngle = 25f;
    [SerializeField] private float tiltSmooth = 12f;

    [Header("Falling wobble")]
    [SerializeField] private float wobbleAngle = 6f;
    [SerializeField] private float wobbleSpeed = 2.5f;

    [Header("Reactions")]
    [SerializeField] private float hitDuration = 0.4f;
    [SerializeField] private float hitShake = 0.15f;
    [SerializeField] private float hitSquash = 0.25f;
    [SerializeField] private float catchDuration = 0.3f;
    [SerializeField] private float catchPop = 0.2f;
    [SerializeField] private float goldenSpinDuration = 0.6f;

    [Header("Funny idle (optional)")]
    [Tooltip("Tung Tung Tung Sahur trembles like a drum, the potatoes wobble side to side, the ballerina does a pirouette")]
    [SerializeField] private bool funnyIdle = true;
    [Tooltip("Ballerina spin speed in degrees per second")]
    [SerializeField] private float pirouetteSpeed = 150f;

    private Transform model;
    private Vector3 baseScale;
    private Vector3 basePos;
    private Quaternion baseRot;

    private GameManager gm;
    private float lastX;
    private float roll;
    private float hitTimer;
    private float catchTimer;
    private float spinTimer;
    private bool dead;
    private float deadSpin;

    private void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<CharacterSpawner>();

        if (spawner == null || spawner.CurrentModel == null)
        {
            Debug.LogWarning("PlayerAnimator: no character model found. Check the CharacterSpawner prefabs.");
            enabled = false;
            return;
        }

        model = spawner.CurrentModel.transform;
        baseScale = model.localScale;
        basePos = model.localPosition;
        baseRot = model.localRotation;
        lastX = transform.position.x;

        gm = GameManager.Instance;
        if (gm != null)
        {
            gm.FruitCaught += OnFruitCaught;
            gm.GameOver += OnGameOver;
        }
    }

    private void OnDestroy()
    {
        if (gm != null)
        {
            gm.FruitCaught -= OnFruitCaught;
            gm.GameOver -= OnGameOver;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        // Horizontal speed, measured from the Player's own movement
        float x = transform.position.x;
        float vel = (x - lastX) / dt;
        lastX = x;

        // Lean into the direction of travel
        float targetRoll = -Mathf.Clamp(vel / referenceSpeed, -1f, 1f) * tiltAngle;
        roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-tiltSmooth * dt));

        // Gentle wobble so the character always looks like it is falling
        float pitch = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle;
        float extraRoll = Mathf.Sin(Time.time * wobbleSpeed * 0.7f + 1f) * wobbleAngle * 0.5f;
        float yaw = 0f;

        // Funny idle, different for each character
        if (funnyIdle)
        {
            switch (CharacterSelection.Index)
            {
                case 0: // Tung Tung Tung Sahur: drum shake
                    extraRoll += Mathf.Sin(Time.time * 28f) * 3f;
                    break;
                case 3: // Ballerina Cappuccina: pirouette
                    yaw += (Time.time * pirouetteSpeed) % 360f;
                    break;
                default: // Potatoes: wobble side to side
                    yaw += Mathf.Sin(Time.time * 3f) * 25f;
                    break;
            }
        }

        // Golden fruit: one full spin
        if (spinTimer > 0f)
        {
            spinTimer = Mathf.Max(0f, spinTimer - dt);
            yaw += (1f - spinTimer / goldenSpinDuration) * 360f;
        }

        // Game over: dizzy spin
        if (dead)
        {
            deadSpin += 720f * dt;
            yaw += deadSpin;
        }

        hitTimer = Mathf.Max(0f, hitTimer - dt);
        catchTimer = Mathf.Max(0f, catchTimer - dt);

        // Scale: squash when hit, pop when a good fruit is caught
        Vector3 mult = Vector3.one;
        Vector3 offset = Vector3.zero;

        if (hitTimer > 0f)
        {
            float t = hitTimer / hitDuration; // 1 -> 0
            mult = new Vector3(1f + hitSquash * t, 1f - hitSquash * t, 1f + hitSquash * t);
            offset = Random.insideUnitSphere * hitShake * t;
            offset.z = 0f;
        }
        else if (catchTimer > 0f)
        {
            float pop = Mathf.Sin((1f - catchTimer / catchDuration) * Mathf.PI) * catchPop;
            mult = Vector3.one * (1f + pop);
        }

        // Rotations are applied in the Player's space, so they look right for any model orientation
        Quaternion q = Quaternion.AngleAxis(roll + extraRoll, Vector3.forward)
                     * Quaternion.AngleAxis(pitch, Vector3.right)
                     * Quaternion.AngleAxis(yaw, Vector3.up)
                     * baseRot;

        model.localRotation = q;
        model.localScale = Vector3.Scale(baseScale, mult);
        model.localPosition = basePos + offset;
    }

    // ---------------------------------------------------------------- events

    private void OnFruitCaught(Fruit fruit)
    {
        if (fruit.heartDamage > 0 || fruit.points < 0)
        {
            hitTimer = hitDuration;
        }
        else if (fruit.points > 0)
        {
            catchTimer = catchDuration;
            if (fruit.type == FruitType.Golden) spinTimer = goldenSpinDuration;
        }
    }

    private void OnGameOver()
    {
        dead = true;
    }
}