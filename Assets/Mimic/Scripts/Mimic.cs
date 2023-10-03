using SpaceGame.Audio;
using SpaceGame.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MimicSpace
{
    public class Mimic : MonoBehaviour
    {
        [Header("Animation")]
        public GameObject legPrefab;

        [Range(2, 20)]
        public int numberOfLegs = 5;
        [Tooltip("The number of splines per leg")]
        [Range(1, 10)]
        public int partsPerLeg = 4;
        private int maxLegs;

        public int legCount;
        public int deployedLegs;
        [Range(0, 19)]
        public int minimumAnchoredLegs = 2;
        public int minimumAnchoredParts;

        [Tooltip("Minimum duration before leg is replaced")]
        public float minLegLifetime = 5;
        [Tooltip("Maximum duration before leg is replaced")]
        public float maxLegLifetime = 15;

        public Vector3 legPlacerOrigin = Vector3.zero;
        [Tooltip("Leg placement radius offset")]
        public float newLegRadius = 3;

        public float minLegDistance = 4.5f;
        public float maxLegDistance = 6.3f;

        [Range(2, 50)]
        [Tooltip("Number of spline samples per legpart")]
        public int legResolution = 40;

        [Tooltip("Minimum lerp coeficient for leg growth smoothing")]
        public float minGrowCoef = 4.5f;
        [Tooltip("MAximum lerp coeficient for leg growth smoothing")]
        public float maxGrowCoef = 6.5f;

        [Tooltip("Minimum duration before a new leg can be placed")]
        public float newLegCooldown = 0.3f;
        private bool canCreateLeg = true;
        private List<GameObject> availableLegPool = new();

        [Tooltip("This must be updates as the Mimin moves to assure great leg placement")]
        public Vector3 velocity;
        internal event Action<float> OnHealthChanged;
        internal event Action OnEnemyDestroyed;
        internal event Action OnDestroyed;

        [SerializeField] private AudioClip _destroyAudio;
        [SerializeField] private GameObject _explosion;

        public Guid Guid { get; private set; }

        public float CurrentHealth { get; private set; }

        private void Start()
        {

            ResetMimic();
        }

        private void OnValidate()
        {
            ResetMimic();
        }

        private void ResetMimic()
        {

            legCount = 0;
            deployedLegs = 0;

            maxLegs = numberOfLegs * partsPerLeg;

            _ = 360f / maxLegs;
            var randV = Random.insideUnitCircle;
            velocity = new Vector3(randV.x, 0, randV.y);
            minimumAnchoredParts = minimumAnchoredLegs * partsPerLeg;
            maxLegDistance = newLegRadius * 2.1f;

        }

        private IEnumerator NewLegCooldown()
        {
            canCreateLeg = false;
            yield return new WaitForSeconds(newLegCooldown);
            canCreateLeg = true;
        }

        protected virtual void OnUpdate() { }
        // Update is called once per frame
        private void Update()
        {

            OnUpdate();
            if (!canCreateLeg)
                return;

            // New leg origin is placed in front of the mimic
            legPlacerOrigin = transform.position + (velocity.normalized * newLegRadius);

            if (legCount <= maxLegs - partsPerLeg)
            {
                // Offset The leg origin by a random vector
                var offset = Random.insideUnitCircle * newLegRadius;
                var newLegPosition = legPlacerOrigin + new Vector3(offset.x, 0, offset.y);

                // If the mimic is moving and the new leg position is behind it, mirror it to make
                // it reach in front of the mimic.
                if (velocity.magnitude > 1f)
                {
                    var newLegAngle = Vector3.Angle(velocity, newLegPosition - transform.position);

                    if (Mathf.Abs(newLegAngle) > 90)
                    {
                        newLegPosition = transform.position - (newLegPosition - transform.position);
                    }
                }

                if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(legPlacerOrigin.x, 0, legPlacerOrigin.z)) < minLegDistance)
                    newLegPosition = ((newLegPosition - transform.position).normalized * minLegDistance) + transform.position;

                // if the angle is too big, adjust the new leg position towards the velocity vector
                if (Vector3.Angle(velocity, newLegPosition - transform.position) > 45)
                    newLegPosition = transform.position + ((newLegPosition - transform.position + (velocity.normalized * (newLegPosition - transform.position).magnitude)) / 2f);

                _ = Physics.Raycast(newLegPosition + (Vector3.up * 10f), -Vector3.up, out var hit);
                var myHit = hit.point;
                if (Physics.Linecast(transform.position, hit.point, out hit))
                    myHit = hit.point;

                var lifeTime = Random.Range(minLegLifetime, maxLegLifetime);

                _ = StartCoroutine(NewLegCooldown());
                for (var i = 0; i < partsPerLeg; i++)
                {
                    RequestLeg(myHit, legResolution, maxLegDistance, Random.Range(minGrowCoef, maxGrowCoef), this, lifeTime);
                    if (legCount >= maxLegs)
                        return;
                }
            }
        }

        // object pooling to limit leg instantiation
        private void RequestLeg(Vector3 footPosition, int legResolution, float maxLegDistance, float growCoef, Mimic myMimic, float lifeTime)
        {
            GameObject newLeg;
            if (availableLegPool.Count > 0)
            {
                newLeg = availableLegPool[^1];
                availableLegPool.RemoveAt(availableLegPool.Count - 1);
            }
            else
            {
                newLeg = Instantiate(legPrefab, transform.position, Quaternion.identity);
            }

            newLeg.SetActive(true);
            newLeg.GetComponent<Leg>().Initialize(footPosition, legResolution, maxLegDistance, growCoef, myMimic, lifeTime);
            newLeg.transform.SetParent(myMimic.transform);
        }

        public void RecycleLeg(GameObject leg)
        {
            availableLegPool.Add(leg);
            leg.SetActive(false);
        }

        public void SetHealth(float health)
        {
            CurrentHealth = health;
        }

        public void SetId(Guid id)
        {
            Guid = id;
        }

        public void Damage(float damage)
        {
            CurrentHealth -= damage;
            transform.localScale = new Vector3(transform.localScale.x - (damage * 0.1f), transform.localScale.y - (damage * 0.1f), transform.localScale.z - (damage * 0.1f));
            OnHealthChanged?.Invoke(CurrentHealth);

            var mimicData = GameContext.CurrentGameData.PlayersData.FirstOrDefault(playerData => playerData.Id == Guid)
                ?? GameContext.CurrentGameData.EnemiesData.First(enemyData => enemyData.Id == Guid);
            mimicData.Health = CurrentHealth;

            if (IsLife())
                return;

            OnDestroyed?.Invoke();
            var explosion = Instantiate(_explosion, gameObject.transform.position, Quaternion.identity);
            Destroy(explosion, 2f);
            Destroy(gameObject);
        }

        protected bool IsLife()
        {
            return CurrentHealth > 0;
        }

        private void OnTriggerStay(Collider other)
        {
            if (gameObject.CompareTag(other.tag))
                return;

            var mimic = other.gameObject.GetComponent<Mimic>();
            if (mimic == null)
                return;

            mimic.Damage(transform.localScale.x * Time.deltaTime * 0.1f);

        }

        private void OnDestroy()
        {
            AudioController.Play(_destroyAudio);
            OnHealthChanged = null;
        }
    }
}