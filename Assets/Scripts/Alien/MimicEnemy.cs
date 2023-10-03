using MimicSpace;
using System.Linq;
using UnityEngine;

namespace SpaceGame.Alien
{

    public class MimicEnemy : Mimic
    {
        private Vector3 _delta;
        private Mimic _player;
        private Mimic[] _players;

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (_player == null)
            {
                _player = FindRandomAlivePlayer(_players);
                return;
            }

            _delta = _player.transform.position - transform.position;
            _delta.Normalize();

            if (_player.transform.localScale.x > transform.localScale.x)
                _delta = new Vector3(-_delta.x, _delta.y, -_delta.z);

            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(_player.transform.position.x, 0, _player.transform.position.z)) < minLegDistance)
            {
                Fight();
            }
        }

        private void Fight()
        {
            Damage(_player.transform.localScale.x * 10f * Time.deltaTime);
            _player.Damage(transform.localScale.x * Time.deltaTime);

            if (!IsLife())
            {
                _player.SetHealth(10f);
            }
        }

        private void FixedUpdate()
        {
            if (!IsMovementReady())
                return;

            Movement();
        }

        private bool IsMovementReady()
        {
            return _player != null;
        }

        private Mimic FindRandomAlivePlayer(Mimic[] players)
        {
            var alivePlayers = players
                .Where(player => player != null)
                .ToArray();
            if (!alivePlayers.Any())
                return null;
            var playerIndex = Random.Range(0, alivePlayers.Length);
            return alivePlayers[playerIndex];
        }

        private void Movement()
        {
            transform.position = transform.position + (_delta * 0.05f);
        }

        internal void SetTargets(Mimic[] players)
        {
            _players = players;
        }
    }
}