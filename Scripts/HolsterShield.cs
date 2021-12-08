using SnippetCode;
using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace HolsterShield
{
    /* ------------------------------------
     *            Meme's Notes
     * ------------------------------------
     * A lot of the variables below can be removed.
     * 
     * I'd highly suggest creating a 'Projectile.cs'
     * class which you can create generic projectile
     * bases and very easily compare them without
     * requiring X amount of harcoded methods, this
     * would allow you to easily expand the projectile
     * types and effects to anything without hardcoding 
     * every single type in this file.
     * 
     * I've performed a general clean, it's untested but it
     * will give you an idea how you can refactor further, there
     * is still a lot that can be done but generall this is the 
     * right steps to take which will make expanding this mod
     * or adding to this mod much much easier.
     * ------------------------------------
     */

    public class HolsterShield : MonoBehaviour, IDisposable
    {
        // Item cache.
        private Item itemHolsterShield;
        private Item projectileThrown;

        // Handle transforms.
        private Transform handleThrowTransform;
        private Transform fixedHandleTransform;

        // Effects.
        private EffectData plumbataFocusEffectData;
        private EffectInstance plumbataFocusEffectVFX;
        private EffectData plumbataChargeEffectData;
        private EffectInstance plumbataChargeEffectVFX;

        // Used to track despawning times.
        private float despawnTimeMax = 100.0f;
        private float despawnTimer;

        // Other.
        private Handle handleThrowPulled;
        private bool handledThrowPulledPulled = false;
        private bool itemThrown = false;
        private Vector3 holsterShieldDirectionForward;
        private bool projectileSpawned = false;
        private float distanceForce;
        private float lastVelocityForce;
        private RagdollHand ragdollHandOnHandle;

        /// <summary>
        /// All projectiles fired by this shield.
        /// </summary>
        public List<Item> Projectiles { get; } = new List<Item>();

        /// <summary>
        /// All vortex projectiles fired by this shield.
        /// </summary>
        public List<Item> VortexProjectiles { get; } = new List<Item>();

        /// <summary>
        /// this items Module.
        /// </summary>
        public HolsterShieldItemModule ItemModule { get; internal set; }

        /// <summary>
        /// This items rigidbody.
        /// </summary>
        public Rigidbody PhysicsBody { get; private set; }

        private void OnEnable()
        {
            // Each time this is unpooled it will recache and reinitialize.

            // Cache.
            itemHolsterShield = GetComponent<Item>();
            handleThrowPulled = itemHolsterShield.handles[1];
            itemHolsterShield.mainHandleLeft.Grabbed += OnShieldGrabbed;
            itemHolsterShield.mainHandleRight.Grabbed += OnShieldGrabbed;
            itemHolsterShield.mainHandleLeft.UnGrabbed += OnShieldUngrabbed;
            itemHolsterShield.mainHandleRight.UnGrabbed += OnShieldUngrabbed;
            handleThrowPulled.Grabbed += OnHandleThrowGrab;
            handleThrowPulled.UnGrabbed += OnHandleThrowUnGrab;
            handleThrowTransform = handleThrowPulled.gameObject.transform;
            PhysicsBody = GetComponent<Rigidbody>();
            PhysicsBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            fixedHandleTransform = gameObject.transform.GetChild(14).gameObject.transform;
            holsterShieldDirectionForward = handleThrowPulled.transform.forward;

            // Load effects.
            plumbataChargeEffectData = Catalog.GetData<EffectData>("VFXPlumbataCharge");
            plumbataFocusEffectData = Catalog.GetData<EffectData>("VFXPlumbataFocus");
        }

        private void OnDisable()
        {
            // When this gets pooled/destroyed, dispose of anything that may cause issues 
            // when it's next unpooled.
            Dispose();
        }

        public void Update()
        {
            // Ensure projectiles fired from this item are despawn tracked.
            if (despawnTimer <= 0)
            {
                // This runs every 1 minute.
                // By this time the projectiles should reach 60s+ and be ready to despawn.

                RegulateProjectile();
                despawnTimer = despawnTimeMax;
            }
            else
            {
                despawnTimer -= Time.deltaTime;
            }

            // Make the fired projectile attract creatures that are at a 1m radius for example
            // I doubt a coroutine here is the best plan here... Any pointers ??

            // Make sure player exists.
            if (Player.local.creature != null)
            {
                // If the handle isn't pulled.
                if (!handledThrowPulledPulled)
                {
                    // When then handled pulled is returning to its position, release the projectile. (and destroy the parent relation with the shield to allow the projectile to move)
                    if (!itemThrown && Math.Abs(Vector3.Distance(fixedHandleTransform.position, handleThrowTransform.position)) < 0.03f && projectileSpawned == true)
                    {
                        holsterShieldDirectionForward = handleThrowPulled.transform.forward;
                        projectileThrown.transform.SetParent(null);
                        ReleaseProjectile(projectileThrown, holsterShieldDirectionForward, distanceForce * 10f + GetMaxVelocityMagnitude(false) * 10f);
                        plumbataChargeEffectVFX?.Stop();
                        plumbataFocusEffectVFX?.Stop();
                        projectileSpawned = false;
                    }
                }
                else
                {
                    // While pulling, play the effects and move the projectile spawned on the shield.
                    plumbataChargeEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
                    plumbataChargeEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);
                    plumbataFocusEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
                    plumbataFocusEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);

                    // Has a projectile spawned?
                    if (projectileSpawned)
                    {
                        projectileThrown.transform.localPosition = fixedHandleTransform.position;
                        projectileThrown.transform.localRotation = fixedHandleTransform.rotation;
                    }
                }
            }
        }

        /// <summary>
        /// Dispose of this item.
        /// </summary>
        public void Dispose()
        {
            itemHolsterShield.mainHandleLeft.Grabbed -= OnShieldGrabbed;
            itemHolsterShield.mainHandleRight.Grabbed -= OnShieldGrabbed;
            itemHolsterShield.mainHandleLeft.UnGrabbed -= OnShieldUngrabbed;
            itemHolsterShield.mainHandleRight.UnGrabbed -= OnShieldUngrabbed;
            handleThrowPulled.Grabbed -= OnHandleThrowGrab;
            handleThrowPulled.UnGrabbed -= OnHandleThrowUnGrab;
        }

        /// <summary>
        /// When the projectile is released, get the spells equipped for different effects.
        /// </summary>
        private void ReleaseProjectile(Item projectile, Vector3 velocity, float factor)
        {
            // Switch case for spell effects.
            switch (ragdollHandOnHandle.caster?.spellInstance?.id)
            {
                case "Fire":
                    DefaultProjectile(projectile, velocity, factor);
                    break;
                case "Gravity":
                    VortexTrailProjectile(projectile, velocity, factor);
                    break;
                case "Lightning":
                    DefaultProjectile(projectile, velocity, factor);
                    break;

                default:
                    DefaultProjectile(projectile, velocity, factor);
                    break;
            }

            // Incase the switch case failed, try a null check on the current spells.
            if (ragdollHandOnHandle.caster?.spellInstance?.id == null)
            {
                // Spell was null, use the default projectile effects.
                DefaultProjectile(projectile, velocity, factor);
            }

            // Set flag
            itemThrown = true;
        }

        /// <summary>
        /// Return the maximum velocity between the current magnitude and last magnitude velocity.
        /// </summary>
        private float GetMaxVelocityMagnitude(bool testValue)
        {
            // If testValue, return 0, else return the highest velocity value between a/b and set lastVelocityForce equal to the result and return it.
            return testValue ? 0 : (lastVelocityForce = Mathf.Max(handleThrowPulled.rb.velocity.magnitude, lastVelocityForce));
        }

        /// <summary>
        /// Check for any active projectiles to despawn.
        /// </summary>
        private void RegulateProjectile()
        {
            // Loop through all active projctiles.
            for (int index = Item.allActive.Count - 1; index >= 0; --index)
            {
                // I uh, I'm not going to touch this. ~MemeMan.
                // Guessing it checks for despawning conditions.
                if ((Item.allActive[index].itemId == "PlumbataHolsterShield"
                    && !Item.allActive[index].IsHanded()
                    && !Item.allActive[index].isGripped
                    && Item.allActive[index].disallowDespawn
                    && !(bool)Item.allActive[index].holder
                    && (Time.time - (double)Item.allActive[index].spawnTime) > 60.0f))
                {
                    // Despawn an active projectile.
                    Item.allActive[index].Despawn();
                }
            }
        }

        /// <summary>
        /// Add a force and launch the projectile using gravity.
        /// </summary>
        private void DefaultProjectile(Item projectile, Vector3 velocity, float factor)
        {
            projectile.rb.useGravity = true;
            projectile.isThrowed = true;
            projectile.isFlying = true;
            projectile.Throw(flyDetection: Item.FlyDetection.Forced);
            projectile.rb.AddForce(velocity * factor, ForceMode.VelocityChange);
            Projectiles.Add(projectile);
        }

        /// <summary>
        /// Add a force and launch a slower projectile with no gravity.
        /// </summary>
        private void VortexTrailProjectile(Item projectile, Vector3 velocity, float factor)
        {
            projectile.rb.useGravity = false;
            projectile.isThrowed = true;
            projectile.isFlying = true;
            projectile.Throw(flyDetection: Item.FlyDetection.Forced);
            projectile.rb.AddForce((velocity * factor) / 3f, ForceMode.VelocityChange);
            Projectiles.Add(projectile);
            VortexProjectiles.Add(projectile);
        }

        /// <summary>
        /// When the shield is grabbed, activate the thrown handle.
        /// </summary>
        private void OnShieldGrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            // Is the event not OnEnd?
            if (eventTime != EventTime.OnEnd)
            {
                return;
            }

            // Set flag.
            handleThrowPulled.SetTouch(true);
        }

        /// <summary>
        /// When the shield is dropped, make the hand pulling drop and deactivate the thrown handle.
        /// </summary>
        private void OnShieldUngrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            // Is the event not OnEnd?
            if (eventTime != EventTime.OnEnd)
            {
                return;
            }

            // Is the hand currently holding the target handle?
            if (ragdollHand.otherHand.grabbedHandle == handleThrowPulled)
            {
                // Ungrab the handle.
                ragdollHand.otherHand.UnGrab(false);
            }

            // Set flag.
            handleThrowPulled.SetTouch(false);
        }

        /// <summary>
        /// When the throw handle is grabbed, spawn the projectile, play the VFX.
        /// </summary>
        private void OnHandleThrowGrab(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            // Is the event not OnEnd?
            if (eventTime != EventTime.OnEnd)
            {
                return;
            }

            // Set flags.
            handledThrowPulledPulled = true;
            itemThrown = false;

            // Spawn a new projectile.
            Catalog.GetData<ItemData>("PlumbataHolsterShield")?.SpawnAsync(projectile =>
            {
                // Configure projectile.
                projectile.disallowDespawn = true;
                projectile.transform.SetParent(fixedHandleTransform);
                projectile.transform.position = Vector3.zero;
                projectile.transform.localPosition = fixedHandleTransform.position;
                projectile.transform.localRotation = fixedHandleTransform.rotation;
                projectile.rb.useGravity = false;
                projectileThrown = projectile;
                projectileSpawned = true;
            });

            // Configure projectile properties.
            plumbataChargeEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
            plumbataChargeEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);
            plumbataFocusEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
            plumbataFocusEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);

            // Spawn projectile VFX and play the,
            plumbataChargeEffectVFX = plumbataChargeEffectData.Spawn(fixedHandleTransform);
            plumbataFocusEffectVFX = plumbataFocusEffectData.Spawn(fixedHandleTransform);
            plumbataChargeEffectVFX?.Play();
            plumbataFocusEffectVFX?.Play();

            // Set the target hand on the current projectile handle.
            ragdollHandOnHandle = ragdollHand;
        }

        // When the throw handle is dropped, get the distance and add a factor and stop the VFX and also grab the hand that is pulling
        private void OnHandleThrowUnGrab(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
        {
            // Is the event not OnEnd?
            if (eventTime != EventTime.OnEnd)
            {
                return;
            }

            // Set flag.
            handledThrowPulledPulled = false;

            // Stop VFX from playing.
            plumbataChargeEffectVFX?.Stop();
            plumbataFocusEffectVFX?.Stop();

            // Calculate the distance force.
            distanceForce = Math.Abs(Vector3.Distance(fixedHandleTransform.localPosition, handleThrowTransform.localPosition)) * 15f;

            // Set the new held hand.
            ragdollHandOnHandle = ragdollHand;
        }
    }
}