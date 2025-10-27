using System;
using PurrNet;
using Unity.Cinemachine;
using UnityEngine;

namespace Script.Object
{
    public class Objects : NetworkBehaviour
    {
        private CharacterMovement player;
        private BoxCollider boxCollider;
        
        private void Start()
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        public GameObject GetObjects(CharacterMovement _Player)
        {
            Debug.Log("GetObjects");
            player = _Player;
            boxCollider.enabled = false;
            transform.localScale /= 2;
            return gameObject;
        }

        private void Update()
        {
            if (player != null)
            {
                transform.position = player.Holder.transform.position;
                
            }
        }
    }
}