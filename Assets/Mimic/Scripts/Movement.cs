using UnityEngine;

namespace MimicSpace
{
    /// <summary>
    /// This is a very basic movement script, if you want to replace it
    /// Just don't forget to update the Mimic's velocity vector with a Vector3(x, 0, z)
    /// </summary>
    public class Movement : MonoBehaviour
    {
        [Header("Controls")]
        [Tooltip("Body Height from ground")]
        [Range(0.5f, 5f)]
        public float height = 0.8f;
        public float speed = 5f;
        private Vector3 velocity = Vector3.zero;
        public float velocityLerpCoef = 4f;
        private Mimic myMimic;
        private PlayerActionsExample playerInput;
        private Collider colider;

        private void Start()
        {
            myMimic = GetComponent<Mimic>();
            playerInput = new PlayerActionsExample();
            playerInput.Enable();
        }

        private void Update()
        {
            var movement = playerInput.Player.Move.ReadValue<Vector2>();
            if (movement == Vector2.zero)
                return;

            velocity = Vector3.Lerp(velocity, new Vector3(movement.x, 0, movement.y).normalized * speed, velocityLerpCoef * Time.deltaTime);

            // Assigning velocity to the mimic to assure great leg placement
            myMimic.velocity = velocity;

            transform.position = transform.position + (velocity * Time.deltaTime);
            var destHeight = transform.position;

            if (Physics.Raycast(transform.position + (Vector3.up * 5f), -Vector3.up, out var hit))
                destHeight = new Vector3(transform.position.x, hit.point.y + height, transform.position.z);

            transform.position = Vector3.Lerp(transform.position, destHeight, velocityLerpCoef * Time.deltaTime);
        }
    }
}