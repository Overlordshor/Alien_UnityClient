using MimicSpace;
using SpaceGame.Alien;
using SpaceGame.SaveSystem;
using SpaceGame.SaveSystem.Dto;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpaceGame.Game
{
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] private MimicEnemy _enemyView;
        [SerializeField] private float _minPositionX;
        [SerializeField] private float _maxPositionX;
        [SerializeField] private float _positionY;
        [SerializeField] private float _spawnDelay = 10f;

        [SerializeField] private EnemyRepository _enemyRepository;

        private WaitForSeconds _wait;
        private Mimic[] players;
        private float _maxHealth;

        private void Start()
        {
            _wait = new WaitForSeconds(_spawnDelay);
        }

        public void SetUp(Mimic[] playerViews, float maxHealth)
        {
            players = playerViews;
            _maxHealth = maxHealth;
        }

        public void StartSpawnEnemies()
        {
            _ = StartCoroutine(SpawnEnemyCoroutine());
        }

        private bool HasAlivePlayer()
        {
            return players
                .Any(player => player != null
                && player.CurrentHealth > 0);
        }

        private IEnumerator SpawnEnemyCoroutine()
        {
            while (HasAlivePlayer())
            {
                yield return _wait;
                var enemyData = new SpaceShipData();

                var positionX = Random.Range(_minPositionX, _maxPositionX);

                enemyData.Positions = new[] { positionX, _positionY };
                enemyData.Health = _maxHealth;
                enemyData.Id = Guid.NewGuid();

                GameContext.CurrentGameData.EnemiesData.Add(enemyData);
                _ = SpawnEnemy(enemyData);
            }
        }

        public Mimic SpawnEnemy(SpaceShipData enemyData)
        {
            var position = new Vector3(enemyData.Positions[0], enemyData.Positions[1], 0);
            var enemyView = CreateEnemyView(position);
            enemyView.SetTargets(players);

            enemyView.SetHealth(enemyData.Health);
            enemyView.SetId(enemyData.Id);

            return enemyView;
        }

        private MimicEnemy CreateEnemyView(Vector3 position)
        {
            var enemyShip = Instantiate(_enemyView, position, Quaternion.identity);

            _enemyRepository.Add(enemyShip);

            enemyShip.OnDestroyed += OnDestroyed;
            return enemyShip;

            void OnDestroyed()
            {
                enemyShip.OnDestroyed -= OnDestroyed;
                _enemyRepository.Remove(enemyShip);
            }
        }
    }
}