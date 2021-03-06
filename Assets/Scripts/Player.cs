﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Player : MonoBehaviour
{

    public static Player Instance;

	public Vector3 positionOffset = new Vector3 (0.0f, 0.4f, 0.0f); // position offset while calculating the biome effect to the player
    public float speed; // speed of player
    public float acceleration; // acceleration of player;
    [HideInInspector]
    public float defaultSpeed = 6.0f; // default speed of player
    [HideInInspector]
    public float defaultAcceleration = 10.0f; // default acceleration of player
    public int baseDamage; // base damage
	public float knockbackMultiplier; // knockback multiplier
    public int maxHealth; // max hit points
    public int health; // current hit points
    public int score; // player score
    public int maxCharges; // max charges
    public int charges; // current charges
    public int icePower; // ice power
    public int firePower; // fire power
    public int prevCharges; // charges of saved state
    public int prevIcePower; // ice power of saved state
    public int prevFirePower; // fire power of saved state
	public int prevScore; // score of saved state
    public float fireResistance; // current fire resistance
    public float maxFireResistance; // max fire resistance
    public float fireResistanceCooldown; // cooldown for fire resistance meter to regenerate if the player was on lava
    public float maxFireResistanceCooldown; // max cooldown time
    public float fireDamageCooldown; // fire damagee cooldown
    public float maxFireDamageCooldown; // max fire damage cooldown time
    public bool onFire; // whether the player is on fine or not
    public int[] weaponShards = new int[4]; //(0, default) (1, hammer) (2, whip) (3, dagger) 
    public int boxes; // number of boxes pushed to correct places
    public int maxBoxes; // number of boxes in the puzzle
    public int levers; // number of switches left to switch
    public int maxLevers; // number of switches in the puzzle
	public bool puzzleCompletedOnce;

    public GameObject gem;
    public AudioClip playerHammerAttackSound;
    public AudioClip playerDaggerAttackSound;
    public AudioClip playerWhipAttackSound;
    public AudioClip playerSwordAttackSound;
    public AudioClip playerHitSound1;
    public AudioClip playerHitSound2;
    public AudioClip playerHitSound3;
    public AudioClip exitFoundSound;
    public AudioClip treasureFoundSound;

    //for attack speed
    public float attackCooldown = 0.5F;

    // attack collider
    public GameObject attackCollider;

	Vector3 defaultWeaponColliderLocalScale;

    public int currentWeapon = 0;
    public int nextWeapon;

    [HideInInspector] public int sword = 0;
    [HideInInspector] public int hammer = 1;
	[HideInInspector] public int spear = 2;
	[HideInInspector] public int dagger = 3;

	public float swordCooldown = 0.6f;
	public float hammerCooldown = 1.0f;
	public float spearCooldown = 0.4f;
	public float daggerCooldown = 0.2f;

	public int swordDamageMultiplier = 70;
	public int hammerDamageMultiplier = 100;
	public int spearDamageMultiplier = 100;
	public int daggerDamageMultiplier = 100;

	public float swordKnockbackMultiplier = 3.0f;
	public float hammerKnockbackMultiplier = 10.0f;
	public float spearKnockbackMultiplier = 2.0f;
	public float daggerKnockbackMultiplier = 0.2f;

    [HideInInspector]
    public Rigidbody2D rb; // rigid body of playersprite
    [HideInInspector]
    public Animator anim;

    public RuntimeAnimatorController Spear_RAC;
    public RuntimeAnimatorController Hammer_RAC;
    public RuntimeAnimatorController Sword_RAC;
    public RuntimeAnimatorController Dagger_RAC;

    public GameObject shard;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        anim = GetComponent<Animator>();

        //anim.runtimeAnimatorController = Resources.Load ("Assets/Animations/Player/SwordAC") as RuntimeAnimatorController;


        //anim.runtimeAnimatorController = Sword_RAC; 

        rb = GetComponent<Rigidbody2D>();
        rb.mass = 1.0f; // mass of player
        rb.drag = 0.0f; // drag of player
        speed = defaultSpeed;
        acceleration = defaultAcceleration;

        maxHealth = 50;
		score = 0;
        maxCharges = 10;
        maxFireResistance = 1.0f;
        maxFireResistanceCooldown = 0.5f;
        maxFireDamageCooldown = 0.5f;
        health = maxHealth;
        fireResistance = maxFireResistance;
        fireDamageCooldown = maxFireDamageCooldown;
        onFire = false;
		puzzleCompletedOnce = false;

		defaultWeaponColliderLocalScale = transform.FindChild("WeaponCollider").localScale;

        currentWeapon = 0;

        prevIcePower = 0;
        prevFirePower = 0;
        prevCharges = 0;

        transform.FindChild("WeaponCollider").gameObject.SetActive(false);
    }

	public void RestartPenalty()
	{
		score = Mathf.FloorToInt (score * 0.8f); // 20% reduction to score;
		prevScore = score;
	}

    // load the current state of the player in the beginning of the room
    public void LoadState()
    {
		score = prevScore;
        icePower = prevIcePower;
        firePower = prevFirePower;
        charges = prevCharges;
        health = maxHealth;
        fireResistance = 1.0f;
        fireDamageCooldown = maxFireDamageCooldown;
		puzzleCompletedOnce = false;
        ResetWeapon();
        PlayerUI.Instance.transform.FindChild("Weapon Timer").FindChild("Real Value").GetComponent<Text>().text = 0.0f.ToString(); // reset weapon durability timer
        PlayerUI.Instance.nextWeaponPanelCountdown = 0.0f; // reset next weapon panel cooldown timer
        PlayerUI.Instance.transform.FindChild("Next Weapon Panel").gameObject.SetActive(false); // hide next weapon panel
		transform.Find ("Saved Velocity").gameObject.GetComponent<Text> ().text = "0,0"; // reset player velocity
		rb.velocity = new Vector2 (0, 0); // reset player velocity
    }

    // save the current state of the player to the beginning of the room 
    public void SaveState()
    {
		prevScore = score;
        prevIcePower = icePower;
        prevFirePower = firePower;
        prevCharges = charges;
    }

    void FixedUpdate()
    {
        float moveHorizontal = 0.0f;
        float moveVertictal = 0.0f;

        bool menusActive = false; // is there any menus active in the scene

        try
        {
            if (DungeonUI.Instance != null)
            {
                foreach (Transform child in DungeonUI.Instance.transform)
                {
                    menusActive = menusActive || child.gameObject.activeSelf;
                }
            }
        }
        catch (NullReferenceException) { }


        if (!menusActive)
        { // if no menus are active
            moveHorizontal = Input.GetAxisRaw("Horizontal");
            if(moveHorizontal < 0)
                transform.localScale = new Vector3(-1, 1, 1);
            else if(moveHorizontal > 0)
                transform.localScale = new Vector3(1, 1, 1);
            moveVertictal = Input.GetAxisRaw("Vertical");
        }

        Vector2 targetVelocity = new Vector3(moveHorizontal, moveVertictal).normalized * speed; // target velocity of player

        Vector2 velocityDifference = (targetVelocity - rb.velocity) * acceleration;
        rb.AddForce(velocityDifference);

        HandleMovement();

		bool PauseMenuActive = false; // is the pause menus active in the scene

		try
		{
			PauseMenuActive = DungeonUI.Instance.transform.Find("Pause Menu").gameObject.activeSelf;
		}
		catch (NullReferenceException) { }

		if (Input.GetKey(KeyCode.Space) && attackCooldown <= 0.0F && !PauseMenuActive)
        {
            //can add switch case for different weapons

            StopAllCoroutines();
            StartCoroutine(MeleeAttack());

            //SoundController.Instance.RandomizeSfxLarge (playerHammerAttackSound);
            switch (currentWeapon)
            {
				case 0:
	                    //sword
					SoundController.Instance.RandomizeSfxLarge (playerSwordAttackSound);
					attackCooldown = swordCooldown;
					baseDamage = Mathf.FloorToInt (swordDamageMultiplier * attackCooldown);
					knockbackMultiplier = swordKnockbackMultiplier;
	                break;
                case 1:
                    //hammer
                    SoundController.Instance.RandomizeSfxLarge(playerHammerAttackSound);
                    attackCooldown = hammerCooldown;
					baseDamage = Mathf.FloorToInt(hammerDamageMultiplier * attackCooldown);
					knockbackMultiplier = hammerKnockbackMultiplier;
                    break;
                case 2:
                    //spear
                    SoundController.Instance.RandomizeSfxLarge(playerWhipAttackSound);
                    attackCooldown = spearCooldown;
					baseDamage = Mathf.FloorToInt(spearDamageMultiplier * attackCooldown);
					knockbackMultiplier = spearKnockbackMultiplier;
                    break;
                case 3:
                    //dagger
                    SoundController.Instance.RandomizeSfxLarge(playerDaggerAttackSound);
                    attackCooldown = daggerCooldown;
					baseDamage = Mathf.FloorToInt(daggerDamageMultiplier * attackCooldown);
					knockbackMultiplier = daggerKnockbackMultiplier;
                    break;
            }
        }
        
		//for testing only
		if (Input.GetKeyDown ("1")) {
			currentWeapon = 0;
			anim.runtimeAnimatorController = Sword_RAC;
			attackCooldown = swordCooldown;
			baseDamage = Mathf.FloorToInt(swordDamageMultiplier * attackCooldown);
			knockbackMultiplier = swordKnockbackMultiplier;
		}
		if (Input.GetKeyDown ("2")) {
			currentWeapon = 1;
			anim.runtimeAnimatorController = Hammer_RAC;
			attackCooldown = hammerCooldown;
			baseDamage = Mathf.FloorToInt(hammerDamageMultiplier * attackCooldown);
			knockbackMultiplier = hammerKnockbackMultiplier;
		}
		if (Input.GetKeyDown ("3")) {
			currentWeapon = 2;
			anim.runtimeAnimatorController = Spear_RAC;
			attackCooldown = spearCooldown;
			baseDamage = Mathf.FloorToInt(spearDamageMultiplier * attackCooldown);
			knockbackMultiplier = spearKnockbackMultiplier;
		}
		if (Input.GetKeyDown ("4")) {
			currentWeapon = 3;
			anim.runtimeAnimatorController = Dagger_RAC;
			attackCooldown = daggerCooldown;
			baseDamage = Mathf.FloorToInt(daggerDamageMultiplier * attackCooldown);
			knockbackMultiplier = daggerKnockbackMultiplier;
		}


    }

    public void UpdateAnimator()
    {
        switch (currentWeapon)
        {
            case 0:
                anim.runtimeAnimatorController = Sword_RAC;
                attackCooldown = swordCooldown;
				baseDamage = Mathf.FloorToInt(swordDamageMultiplier * attackCooldown);
				knockbackMultiplier = swordKnockbackMultiplier;
                break;
            case 1:
                anim.runtimeAnimatorController = Hammer_RAC;
                attackCooldown = hammerCooldown;
				baseDamage = Mathf.FloorToInt(hammerDamageMultiplier * attackCooldown);
				knockbackMultiplier = hammerKnockbackMultiplier;
                break;
            case 2:
                anim.runtimeAnimatorController = Spear_RAC;
                attackCooldown = spearCooldown;
				baseDamage = Mathf.FloorToInt(spearDamageMultiplier * attackCooldown);
				knockbackMultiplier = spearKnockbackMultiplier;
                break;
            case 3:
                anim.runtimeAnimatorController = Dagger_RAC;
                attackCooldown = daggerCooldown;
				baseDamage = Mathf.FloorToInt(daggerDamageMultiplier * attackCooldown);
				knockbackMultiplier = daggerKnockbackMultiplier;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // update camera position
        Camera.main.transform.position = new Vector3(transform.position[0], transform.position[1] + Mathf.Tan(Mathf.Deg2Rad * -20.0f) * 20.0f, Camera.main.transform.position[2]);

		if (health == 0) {
			gameObject.SetActive (false);
		} else {
			gameObject.SetActive (true);
		}

        attackCooldown = Mathf.Max(attackCooldown - Time.deltaTime, 0);
    }

    // reset weapon to default
    public void ResetWeapon()
    {
        currentWeapon = sword;
        nextWeapon = sword;
    }

    void OnTriggerEnter2D(Collider2D coll)
    {
        if (coll.gameObject.tag == "Loot")
        {
            // show health bar of loot box
            coll.transform.FindChild("Health Bar").gameObject.SetActive(true);

            coll.gameObject.GetComponent<LootBoxController>().health -= 1;

            if (coll.gameObject.GetComponent<LootBoxController>().health <= 0)
            {
                // spawn gems
                Vector3 boxPosition = coll.gameObject.transform.position;
                bool firstGemOnLeft = Random.Range(0.0f, 1.0f) > 0.5f;
                bool firstGemIsFireGem = Dungeon.Instance.currentRoomClimate > 0.2f; // determine element of first gem
                bool secondGemIsFireGem = Dungeon.Instance.currentRoomClimate > -0.2f; // determine element of second gem
                GameObject firstGem = Instantiate(gem, new Vector3(boxPosition.x + (firstGemOnLeft ? -0.55f : 0.55f), boxPosition.y, -1.1f), Quaternion.identity); // spawn first gem
                firstGem.GetComponent<GemController>().firePower = firstGemIsFireGem ? 1 : 0;
                firstGem.GetComponent<GemController>().icePower = firstGemIsFireGem ? 0 : 1;
                firstGem.transform.SetParent(Dungeon.Instance.dungeonVisual.transform);
                GameObject secondGem = Instantiate(gem, new Vector3(boxPosition.x + (firstGemOnLeft ? 0.55f : -0.55f), boxPosition.y, -1.1f), Quaternion.identity); // spawn second gem
                secondGem.GetComponent<GemController>().firePower = secondGemIsFireGem ? 1 : 0;
                secondGem.GetComponent<GemController>().icePower = secondGemIsFireGem ? 0 : 1;
                secondGem.transform.SetParent(Dungeon.Instance.dungeonVisual.transform);

                // destroy loot box
                Destroy(coll.gameObject);

                AstarPath.active.Scan();
            }
        }

    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Exit")
        {
            if (boxes == maxBoxes && levers == maxLevers) { // if all puzzles are being solved
				// show next level menu if the player touches the exit for the first time
				if (DungeonUI.Instance.nextLevelMenuActive == false) {
					
					SoundController.Instance.PlaySingle(exitFoundSound);

					Player.Instance.score += 5 * Player.Instance.health;
					SaveState(); // save state of player
				}
                DungeonUI.Instance.showNextLevelMenu();
            }
        }
		else if (coll.gameObject.CompareTag("Small Monster") || coll.gameObject.CompareTag("Large Monster")|| coll.gameObject.CompareTag("Boss"))
        {

			if (health > 0) {
				health -= 1;
			}
            Vector3 enemyPosition = coll.transform.position;

            rb.AddForce((transform.position - coll.transform.position).normalized * coll.gameObject.GetComponent<Rigidbody2D>().mass * 1.0f, ForceMode2D.Impulse);
            SoundController.Instance.RandomizeSfxLarge(playerHitSound1, playerHitSound2, playerHitSound3);

        }


    }

    void HandleMovementAnimations()
    {
        if ((rb.velocity.x > -0.1 && rb.velocity.x < 0.1) && (rb.velocity.y > -0.1 && rb.velocity.y < 0.1))
            anim.SetBool("Moving", false);
        else
            anim.SetBool("Moving", true);
        anim.SetInteger("VerticalMovement", (int)Input.GetAxisRaw("Vertical"));
        anim.SetInteger("HorizontalMovement", (int)Input.GetAxisRaw("Horizontal"));
    }

    void HandleMovement()
    {

        float input_x = Input.GetAxisRaw("Horizontal");
        float input_y = Input.GetAxisRaw("Vertical");

        bool isWalking = (Mathf.Abs(input_x) + Mathf.Abs(input_y)) > 0.01f;

        anim.SetBool("isWalking", isWalking);

        if (isWalking)
        {
            anim.SetFloat("x", input_x);
            anim.SetFloat("y", input_y);
        }

    }

    IEnumerator MeleeAttack()
    {
        //change collider scale depends on weapon range
        switch (currentWeapon)
        {
            case 0:
                //sword
				transform.FindChild("WeaponCollider").localScale = new Vector3 (defaultWeaponColliderLocalScale.x, defaultWeaponColliderLocalScale.y, defaultWeaponColliderLocalScale.z);
                break;
            case 1:
                //hammer
				transform.FindChild("WeaponCollider").localScale = new Vector3 (defaultWeaponColliderLocalScale.x * 1.5f, defaultWeaponColliderLocalScale.y * 1.5f, defaultWeaponColliderLocalScale.z);
                break;
            case 2:
                //spear
				transform.FindChild("WeaponCollider").localScale = new Vector3 (defaultWeaponColliderLocalScale.x * 2.5f, defaultWeaponColliderLocalScale.y, defaultWeaponColliderLocalScale.z);
                break;
            case 3:
                //dagger
				transform.FindChild("WeaponCollider").localScale = new Vector3 (defaultWeaponColliderLocalScale.x, defaultWeaponColliderLocalScale.y * 2.25f, defaultWeaponColliderLocalScale.z);
                break;
        }


        //Get player orientation
        float x = anim.GetFloat("x");

        float y = anim.GetFloat("y");

        if (x == 1)
        {
            transform.FindChild("WeaponCollider").localEulerAngles = new Vector3(0, 0, 0);
        }
        else if (x == -1)
        {
            transform.FindChild("WeaponCollider").localEulerAngles = new Vector3(0, 0, -0);
        }
        else if (y == 1)
        {
            transform.FindChild("WeaponCollider").localEulerAngles = new Vector3(0, 0, 90);
        }
        else {
            transform.FindChild("WeaponCollider").localEulerAngles = new Vector3(0, 0, -90);
        }

        //trigger attack anim and collider
        anim.SetTrigger("meleeAttack");

        transform.FindChild("WeaponCollider").gameObject.SetActive(true);

        yield return new WaitForSeconds(0.1f);
        transform.FindChild("WeaponCollider").gameObject.SetActive(false);
        //charges -= 1;

        //restore collider scale
        transform.FindChild("WeaponCollider").localScale = defaultWeaponColliderLocalScale;
    }
}