using Assets._04.Core;

using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Yggdrasils
{
    public delegate void OnYggdrasilHealthChanged(float health, float maxHealth);

    public class Yggdrasil : Singleton<Yggdrasil>
    {
        private float maxHealth;
        private float health;

        public OnYggdrasilHealthChanged OnYggdrasilHealthChanged;

        public void Initialize(float maxHealth)
        {
            health = this.maxHealth = maxHealth;
        }

        public void Hit(float damage)
        {
            health -= damage;
            health = Mathf.Clamp(health, 0f, maxHealth);
            OnYggdrasilHealthChanged?.Invoke(health, maxHealth);
        }
    }
}
