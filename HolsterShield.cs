using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using SnippetCode;
using Random = UnityEngine.Random;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq.Expressions;

namespace HolsterShield
{
    public class HolsterShieldItemModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            if (Level.current.data.id == "CharacterSelection")
                return;
            var holstershield = item.gameObject.AddComponent<HolsterShield>();
            holstershield.module = this;
        }

        /* Here's what I would like to do, I don't know if it's possible or what I should do but here's the main stuff :
            - Create a Plumbata class for the projectile that are created
            - Add a spell that allow to modify some property of the shield and for example, make the shield levitate next to the player with a spell class
            - Create a controller that coordinates all of this ??
        
        
        Any advice or at least beginning of classes and how they can interact together is welcome
        I'm still lacking on how classes can work together and mostly where is the entry point of the code


        I took inspiration of Shatterblade to make the item and monobehavior, but it's a bit confusing on how exactly it works
        */

        public class HolsterShield : MonoBehaviour
        {
            public HolsterShieldItemModule module;
            public Item itemHolsterShield;
            public Handle handleThrowPulled;
            public bool isDespawned;
            public Rigidbody rb { get; protected set; }
            private string itemId = "PlumbataHolsterShield";
            private string thrownItemId;
            public bool handledThrowPulledPulled = false;
            private bool itemThrown = false;
            private static List<Item> listProjectile = new List<Item>();
            private static List<Item> listProjectileVortexTrail = new List<Item>();
            private List<Creature> creatures = new List<Creature>();
            private EffectData plumbataFocusEffectData;
            private EffectInstance plumbataFocusEffectVFX;
            private EffectData plumbataChargeEffectData;
            private EffectInstance plumbataChargeEffectVFX;
            private Vector3 holsterShieldDirectionForward;
            private Item projectileThrown;
            private bool projectileSpawned = false;
            private float distanceForce;
            private float velocityForce;
            private Transform handleThrowTransform;
            private Transform fixedHandleTransform;
            private RagdollHand ragdollHandOnHandle;

            // Create stuff like grab event on handles and VFX
            public void Init()
            {
                if (!gameObject)
                    return;
                itemHolsterShield = GetComponent<Item>();
                handleThrowPulled = itemHolsterShield.handles[1];
                itemHolsterShield.mainHandleLeft.Grabbed += new Handle.GrabEvent(OnShieldGrabbed);
                itemHolsterShield.mainHandleRight.Grabbed += new Handle.GrabEvent(OnShieldGrabbed);
                itemHolsterShield.mainHandleLeft.UnGrabbed += new Handle.GrabEvent(OnShieldUngrabbed);
                itemHolsterShield.mainHandleRight.UnGrabbed += new Handle.GrabEvent(OnShieldUngrabbed);
                handleThrowPulled.Grabbed += new Handle.GrabEvent(OnHandleThrowGrab);
                handleThrowPulled.UnGrabbed += new Handle.GrabEvent(OnHandleThrowUnGrab);
                handleThrowTransform = handleThrowPulled.gameObject.transform;
                rb = GetComponent<Rigidbody>();
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                // Plumbata Charge
                plumbataChargeEffectData = Catalog.GetData<EffectData>("VFXPlumbataCharge");
                // Plumbata Focus
                plumbataFocusEffectData = Catalog.GetData<EffectData>("VFXPlumbataFocus");
                fixedHandleTransform = gameObject.transform.GetChild(14).gameObject.transform;
                holsterShieldDirectionForward = handleThrowPulled.transform.forward;
            }

            public void Start()
            {
                Init();
            }
            public void Update()
            {
                if (itemHolsterShield == null)
                {
                    Init();
                    if (itemHolsterShield == null)
                        return;
                }
                //Coroutine to make despawn the plumbata when 1 minute have passed
                GameManager.local.StartCoroutine(regulateNbProjectile());
                
                // Make the fired projectile attract creatures that are at a 1m radius for example
                // I doubt a coroutine here is the best plan here... Any pointers ??

                //GameManager.local.StartCoroutine(vortexTrailRoutine());
                //Make sure player exist
                if (Player.local.creature != null)
                {
                    // If the handle isn't pulled
                    if (!handledThrowPulledPulled)
                    {
                        // If the plumbata have spawned and 
                        // Nevermind, it doesn't work
                        if(projectileSpawned && handledThrowPulledPulled)
                        {
                            GetMaxVelocityMagnitude(true);
                        }
                        // When then handled pulled is returning to his position : release the projectile ! (and destroy the parent relation with the shield to allow the projectile to move)
                        if (!itemThrown && Math.Abs(Vector3.Distance(fixedHandleTransform.position, handleThrowTransform.position)) < 0.03f && projectileSpawned == true)
                        {
                            holsterShieldDirectionForward = handleThrowPulled.transform.forward;
                            projectileThrown.transform.SetParent(null);
                            ReleaseProjectile(projectileThrown, holsterShieldDirectionForward, distanceForce * 10f + velocityForce * 10f);
                            plumbataChargeEffectVFX?.Stop();
                            plumbataFocusEffectVFX?.Stop();
                            GetMaxVelocityMagnitude(false);
                            projectileSpawned = false;
                        }
                    }
                    // While pulling, play the effects and move the projectile spawned on the shield
                    else
                    {
                        plumbataChargeEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
                        plumbataChargeEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);
                        plumbataFocusEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
                        plumbataFocusEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);
                        if (projectileSpawned == true)
                        {
                            projectileThrown.transform.localPosition = fixedHandleTransform.position;
                            projectileThrown.transform.localRotation = fixedHandleTransform.rotation;
                        }
                    }
                }
            }
            // When the shield is grabbed, activate the thrown handle
            private void OnShieldGrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
            {
                if (eventTime != EventTime.OnEnd)
                    return;
                handleThrowPulled.SetTouch(true);
            }

            // When the shield is dropped, make the hand pulling drop and deactivate the thrown handle
            private void OnShieldUngrabbed(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
            {
                if (eventTime != EventTime.OnEnd)
                    return;
                if (ragdollHand.otherHand.grabbedHandle == true && ragdollHand.otherHand.grabbedHandle == handleThrowPulled)
                    ragdollHand.otherHand.UnGrab(false);
                handleThrowPulled.SetTouch(false);
            }
            // When the throw handle is grabbed, spawn the projectile, play the VFX
            private void OnHandleThrowGrab(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
            {
                if (eventTime != EventTime.OnEnd)
                    return;
                handledThrowPulledPulled = true;
                itemThrown = false;
                thrownItemId = itemId.ToString();
                Catalog.GetData<ItemData>(thrownItemId)?.SpawnAsync(projectile =>
                {
                    projectile.disallowDespawn = true;
                    projectile.transform.SetParent(fixedHandleTransform);
                    projectile.transform.position = SnippetCode.SnippetCode.zero;
                    projectile.transform.localPosition = fixedHandleTransform.position;
                    projectile.transform.localRotation = fixedHandleTransform.rotation;
                    projectile.rb.useGravity = false;
                    projectileThrown = projectile;
                    projectileSpawned = true;
                });
                plumbataChargeEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
                plumbataChargeEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);
                plumbataFocusEffectVFX?.SetVFXProperty("PositionSet_position", fixedHandleTransform.position);
                plumbataFocusEffectVFX?.SetVFXProperty("PositionHandle_position", handleThrowPulled.transform.position);
                plumbataChargeEffectVFX = plumbataChargeEffectData.Spawn(fixedHandleTransform);
                plumbataFocusEffectVFX = plumbataFocusEffectData.Spawn(fixedHandleTransform);
                plumbataChargeEffectVFX?.Play();
                plumbataFocusEffectVFX?.Play();
                ragdollHandOnHandle = ragdollHand;
            }

            // When the throw handle is dropped, get the distance and add a factor and stop the VFX and also grab the hand that is pulling
            private void OnHandleThrowUnGrab(RagdollHand ragdollHand, Handle handle, EventTime eventTime)
            {
                if (eventTime != EventTime.OnEnd)
                    return;
                handledThrowPulledPulled = false;
                plumbataChargeEffectVFX?.Stop();
                plumbataFocusEffectVFX?.Stop();
                distanceForce = Math.Abs(Vector3.Distance(fixedHandleTransform.localPosition, handleThrowTransform.localPosition)) * 15f;
                ragdollHandOnHandle = ragdollHand;
            }
            // When the projectile is released, get the spells equipped for different effects
            private void ReleaseProjectile(Item projectile, Vector3 velocity, float factor)
            {
                switch(ragdollHandOnHandle.caster?.spellInstance?.id)
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
                if (ragdollHandOnHandle.caster?.spellInstance?.id == null)
                {
                    //case "Default Mode"
                    DefaultProjectile(projectile, velocity, factor);
                }
                itemThrown = true;
            }

            private void GetMaxVelocityMagnitude(bool testValue)
            {
                if (testValue)
                {
                    if (velocityForce <= Math.Abs(handleThrowPulled.rb.velocity.magnitude))
                        velocityForce = Math.Abs(handleThrowPulled.rb.velocity.magnitude);
                }
                else
                {
                    velocityForce = 0;
                }
            }

            private IEnumerator regulateNbProjectile()
            {
                for (int index = Item.allActive.Count - 1; index >= 0; --index)
                {
                    if ((Item.allActive[index].itemId == "PlumbataHolsterShield"
                        && !Item.allActive[index].IsHanded()
                        && !Item.allActive[index].isGripped
                        && Item.allActive[index].disallowDespawn
                        && !(bool)Item.allActive[index].holder
                        && (Time.time - (double)Item.allActive[index].spawnTime) > 60.0f))
                    {
                        Item.allActive[index].Despawn();
                    }
                }
                    yield return null;
            }
            // Dose not work pls help !
            private IEnumerator vortexTrailRoutine()
            {
                if (listProjectileVortexTrail.Count != 0)
                {
                    foreach (Item projectile in listProjectileVortexTrail)
                    {
                        foreach (Collider colliders in Physics.OverlapSphere(projectile.transform.position, 1f, -5, QueryTriggerInteraction.Ignore))
                        {
                            creatures.Add(colliders.attachedRigidbody?.GetComponent<CollisionHandler>()?.ragdollPart?.ragdoll.creature);
                        }
                        if (creatures.Count != 0)
                        {
                            foreach (Creature creature in creatures)
                            {
                                if (!creature.isPlayer)
                                {
                                    Debug.Log("Push creatures !");
                                    creature?.TryPush(Creature.PushType.Hit, (itemHolsterShield.transform.position - creature.transform.position).normalized / Vector3.Distance(itemHolsterShield.transform.position, creature.transform.position) * 20f, 4);
                                    //creature?.ragdoll.headPart.rb.AddForce((itemHolsterShield.transform.position - creature.transform.position).normalized / Vector3.Distance(itemHolsterShield.transform.position, creature.transform.position) * 20f, ForceMode.VelocityChange);
                                }
                            }
                        }
                    }
                }
                yield return null;
            }
            // Add a force and launch the projectile using gravity
            private void DefaultProjectile(Item projectile, Vector3 velocity, float factor)
            {
                projectile.rb.useGravity = true;
                projectile.isThrowed = true;
                projectile.isFlying = true;
                projectile.Throw(flyDetection: Item.FlyDetection.Forced);
                projectile.rb.AddForce(velocity * factor, ForceMode.VelocityChange);
                listProjectile.Add(projectile);
            }
            // Add a force and launch a slower projectile with no gravity
            private void VortexTrailProjectile(Item projectile, Vector3 velocity, float factor)
            {
                projectile.rb.useGravity = false;
                projectile.isThrowed = true;
                projectile.isFlying = true;
                projectile.Throw(flyDetection: Item.FlyDetection.Forced);
                projectile.rb.AddForce((velocity * factor) / 3f, ForceMode.VelocityChange);
                listProjectile.Add(projectile);
                listProjectileVortexTrail.Add(projectile);
            }
        }
    }
}
