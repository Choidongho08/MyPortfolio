using System.Collections.Generic;
using UnityEngine;

namespace Assets._01.Member.CDH.Code.Synergies.Banana
{
    public class BananaSynergyEffect : SynergyEffectComponent
    {
        [SerializeField] private GameObject bananaPeel;
        [SerializeField] private float minDuration;
        [SerializeField] private float maxDuration;

        private List<Transform> wayPoints;
        private bool isSynergyActive;
        private float timer;
        private float duration;

        protected override void Awake()
        {
            base.Awake();
            wayPoints = WayPointManager.Instance.GetWaypoints();
            isSynergyActive = false;
        }

        private void Update()
        {
            if (!isSynergyActive)
                return;

            timer += Time.deltaTime;
            if(timer > duration)
            {
                timer = 0f;
                SetDuration();

                int randValue = Random.Range(0, wayPoints.Count);
                Vector3 randPos = wayPoints[randValue].position;
                Instantiate(bananaPeel, randPos, Quaternion.identity);
                Destroy(bananaPeel, 20.0f);
            }
        }

        protected override void SynergyActiveMethod(Synergy synergy)
        {
            isSynergyActive = true;
            SetDuration();
        }

        private void SetDuration()
        {
            duration = Random.Range(minDuration, maxDuration);
        }
    }
}
