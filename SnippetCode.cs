using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;
using SnippetCode;
using System.Collections;

namespace SnippetCode
{
    static class SnippetCode
    {
        /// <summary>
        /// Vector pointing away from the palm
        /// </summary>
        public static Vector3 PalmDir(this RagdollHand hand) => -hand.transform.forward;

        /// <summary>
        /// Vector pointing in the direction of the thumb
        /// </summary>
        public static Vector3 ThumbDir(this RagdollHand hand) => (hand.side == Side.Right) ? hand.transform.up : -hand.transform.up;

        /// <summary>
        /// Vector pointing away in the direction of the fingers
        /// </summary>
        public static Vector3 PointDir(this RagdollHand hand) => -hand.transform.right;

        /// <summary>
        /// Get a point above the player's hand
        /// </summary>
        public static Vector3 PosAboveBackOfHand(this RagdollHand hand) => hand.transform.position - hand.transform.right * 0.1f + hand.transform.forward * 0.2f;

        public static void SetVFXProperty<T>(this EffectInstance effect, string name, T data)
        {
            if (effect == null)
                return;
            if (data is Vector3 v)
            {
                foreach (EffectVfx effectVfx in effect.effects.Where<Effect>((Func<Effect, bool>)(fx => fx is EffectVfx effectVfx17 && effectVfx17.vfx.HasVector3(name))))
                    effectVfx.vfx.SetVector3(name, v);
            }
            else if (data is float f2)
            {
                foreach (EffectVfx effectVfx2 in effect.effects.Where<Effect>((Func<Effect, bool>)(fx => fx is EffectVfx effectVfx18 && effectVfx18.vfx.HasFloat(name))))
                    effectVfx2.vfx.SetFloat(name, f2);
            }
            else if (data is int i3)
            {
                foreach (EffectVfx effectVfx2 in effect.effects.Where<Effect>((Func<Effect, bool>)(fx => fx is EffectVfx effectVfx19 && effectVfx19.vfx.HasInt(name))))
                    effectVfx2.vfx.SetInt(name, i3);
            }
            else if (data is bool b4)
            {
                foreach (EffectVfx effectVfx2 in effect.effects.Where<Effect>((Func<Effect, bool>)(fx => fx is EffectVfx effectVfx20 && effectVfx20.vfx.HasBool(name))))
                    effectVfx2.vfx.SetBool(name, b4);
            }
            else
            {
                if (!(data is Texture t5))
                    return;
                foreach (EffectVfx effectVfx2 in effect.effects.Where<Effect>((Func<Effect, bool>)(fx => fx is EffectVfx effectVfx21 && effectVfx21.vfx.HasTexture(name))))
                    effectVfx2.vfx.SetTexture(name, t5);
            }
        }
        public static object GetVFXProperty(this EffectInstance effect, string name)
        {
            foreach (Effect effect1 in effect.effects)
            {
                if (effect1 is EffectVfx effectVfx1)
                {
                    if (effectVfx1.vfx.HasFloat(name))
                        return effectVfx1.vfx.GetFloat(name);
                    if (effectVfx1.vfx.HasVector3(name))
                        return effectVfx1.vfx.GetVector3(name);
                    if (effectVfx1.vfx.HasBool(name))
                        return effectVfx1.vfx.GetBool(name);
                    if (effectVfx1.vfx.HasInt(name))
                        return effectVfx1.vfx.GetInt(name);
                }
            }
            return null;
        }
        public static Vector3 zero = Vector3.zero;
        public static Vector3 one = Vector3.one;
        public static Vector3 forward = Vector3.forward;
        public static Vector3 right = Vector3.right;
        public static Vector3 up = Vector3.up;
        public static Vector3 back = Vector3.back;
        public static Vector3 left = Vector3.left;
        public static Vector3 down = Vector3.down;
    }
}
