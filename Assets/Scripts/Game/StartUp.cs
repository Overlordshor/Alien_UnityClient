using Cinemachine;
using MimicSpace;
using SpaceGame.SaveSystem;
using SpaceGame.SaveSystem.Dto;
using SpaceGame.ScoreSystem;
using SpaceGame.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace SpaceGame.Game
{
    public class StartUp : MonoBehaviour
    {
        [SerializeField] private EnemyFactory _enemyFactory;

        [SerializeField] private Transform _startedPosition;

        [SerializeField] private int _scorePerEnemy = 1;

        [SerializeField] private EnemyRepository _enemyRepository;

        [SerializeField] private HUD _hud;
        [SerializeField] private float _maxHealth = 10;

        [SerializeField] private int _playersNumber = 2;

        [SerializeField] private CinemachineVirtualCamera _virtualCamera;
        [SerializeField] private Mimic[] _playerShipPrefabs;
        private List<PlayerData> _playersData;

        private void Awake()
        {
            foreach (var g in GameObject.FindObjectsOfType<Leg>())
            {
                Destroy(g.gameObject);
            }
        }
        private void Start()
        {
            _enemyRepository.OnEnemyAdded += OnEnemyCountChanged;
            _enemyRepository.OnEnemyRemoved += OnEnemyCountChanged;

            var playerFactory = new PlayerFactory();

            GameContext.CurrentGameData ??= new GameData();
            _playersData = GameContext.CurrentGameData.PlayersData;
            while (_playersData.Count < _playersNumber)
            {
                var index = _playersNumber - _playersData.Count - 1;
                var player = playerFactory.CreatePlayer((PlayerIndex)index);

                var playerData = playerFactory.CreatePlayerData(player);

                playerData.Health = _maxHealth;
                playerData.Positions = new float[] { _startedPosition.position.x, _startedPosition.position.y };

                _playersData.Add(playerData);
            }

            var playerShips = new Mimic[_playersNumber];

            for (var i = 0; i < _playersNumber; i++)
            {
                CreatePlayer(i);
            }

            _enemyFactory.SetUp(playerShips, _maxHealth);
            _enemyFactory.StartSpawnEnemies();

            foreach (var enemyData in GameContext.CurrentGameData.EnemiesData)
            {
                _ = _enemyFactory.SpawnEnemy(enemyData);
            }

            void CreatePlayer(int index)
            {
                var playerData = _playersData[index];
                var player = playerFactory.CreatePlayer((PlayerIndex)index, playerData);

                var playerView = CreatePlayerShip(_playerShipPrefabs[index], player, playerData);
                _virtualCamera.Follow = playerView.transform;
                _virtualCamera.LookAt = playerView.transform;

                player.SetShipId(playerView.Guid);

                playerShips[index] = playerView;

                _enemyRepository.OnEnemyAdded += TryKillPlayers;

                void TryKillPlayers(int count)
                {
                    if (count == 10)
                    {
                        playerView.Damage(playerView.CurrentHealth);
                    }
                }
            }
        }

        private Mimic CreatePlayerShip(Mimic playerViewPrefab, Player player, PlayerData playerData)
        {
            var startedPosition = new Vector3(playerData.Positions[0], playerData.Positions[1], _startedPosition.position.z);
            var playerView = Instantiate(playerViewPrefab, startedPosition, Quaternion.identity);

            playerView.SetHealth(playerData.Health);

            playerView.OnHealthChanged += OnPlayerHealthChanged;
            OnPlayerHealthChanged(playerView.CurrentHealth);

            player.OnScoreAdded += OnPlayerScoreAdded;
            OnPlayerScoreAdded(player.GetScore());

            playerView.SetId(playerData.Id);

            playerView.OnEnemyDestroyed += OnEnemyDestroyed;
            playerView.OnDestroyed += OnDestroyed;

            return playerView;
            void OnPlayerHealthChanged(float health)
            {
                if (player.Id == PlayerIndex.Second)
                    UpdateSecondPlayerHealth(health);
                if (player.Id == PlayerIndex.First)
                    UpdateFirstPlayerHealth(health);
            }

            void OnPlayerScoreAdded(int score)
            {
                if (player.Id == PlayerIndex.Second)
                    OnSecondPlayerScoreAdded(score);
                if (player.Id == PlayerIndex.First)
                    OnFirstPlayerScoreAdded(score);
            }

            void OnEnemyDestroyed()
            {
                player.AddScore(_scorePerEnemy);
            }

            void OnDestroyed()
            {
                player.OnScoreAdded -= OnPlayerScoreAdded;
                playerView.OnHealthChanged -= OnPlayerHealthChanged;

                _ = _playersData.Remove(playerData);

                playerView.OnEnemyDestroyed -= OnEnemyDestroyed;
                playerView.OnDestroyed -= OnDestroyed;

                SceneManager.LoadScene(0);
            }
        }

        private void OnEnemyCountChanged(int count)
        {
            _hud.SetEnemyCount(count);
        }

        private void UpdateFirstPlayerHealth(float health)
        {
            _hud.Set1PlayerHealthText(health);
        }

        private void UpdateSecondPlayerHealth(float health)
        {
            _hud.Set2PlayerHealthText(health);
        }

        private void OnFirstPlayerScoreAdded(int score)
        {
            _hud.Set1PlayerScoreText(score);
        }

        private void OnSecondPlayerScoreAdded(int score)
        {
            _hud.Set2PlayerScoreText(score);
        }

        private void OnDestroy()
        {
            _enemyRepository.OnEnemyAdded -= OnEnemyCountChanged;
            _enemyRepository.OnEnemyRemoved -= OnEnemyCountChanged;
        }
    }
}