using SpaceGame.Alien;
using SpaceGame.SaveSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceGame.Game
{
    public class EnemyRepository : MonoBehaviour
    {
        public event Action<int> OnEnemyRemoved;

        public event Action<int> OnEnemyAdded;

        private List<MimicEnemy> _enemyShips = new();

        public void Add(MimicEnemy enemyShip)
        {
            _enemyShips.Add(enemyShip);

            OnEnemyAdded?.Invoke(_enemyShips.Count);
        }

        public void Remove(MimicEnemy enemyShip)
        {
            _ = _enemyShips.Remove(enemyShip);

            var enemyData = GameContext.CurrentGameData.EnemiesData.First(enemyData => enemyData.Id == enemyShip.Guid);
            _ = GameContext.CurrentGameData.EnemiesData.Remove(enemyData);

            OnEnemyRemoved?.Invoke(_enemyShips.Count);
        }
    }
}