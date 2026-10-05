using System;
using System.Collections.Generic;
using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class PartSpawner : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlacementManager placement;
        [SerializeField] private DraggableObject[] prefabs = Array.Empty<DraggableObject>();
        [SerializeField] private int[] partLimits = Array.Empty<int>();
        [SerializeField] private Vector2[] spawnPositions = Array.Empty<Vector2>();

        public event Action<int> PartAdded;

        private readonly List<SpawnedPart> spawnedParts = new List<SpawnedPart>();

        private bool CanEditParts => isActiveAndEnabled && gameManager != null &&
            gameManager.State == GameState.Edit && placement != null &&
            placement.CanModifyParts;

        public bool CanAddParts
        {
            get
            {
                if (!CanEditParts || prefabs == null)
                {
                    return false;
                }
                for (int index = 0; index < prefabs.Length; index++)
                {
                    if (GetRemainingCount(index) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void Configure(GameManager manager, PlacementManager placementManager,
            DraggableObject[] partPrefabs, int[] partLimits = null)
        {
            gameManager = manager;
            placement = placementManager;
            prefabs = partPrefabs != null
                ? (DraggableObject[])partPrefabs.Clone()
                : Array.Empty<DraggableObject>();
            this.partLimits = partLimits != null ? (int[])partLimits.Clone() : Array.Empty<int>();
            spawnPositions = Array.Empty<Vector2>();
        }

        public void ConfigureSpawnPositions(params Vector2[] positions)
        {
            spawnPositions = positions != null ? (Vector2[])positions.Clone() : Array.Empty<Vector2>();
        }

        public int GetRemainingCount(int index)
        {
            if (prefabs == null || index < 0 || index >= prefabs.Length || prefabs[index] == null)
            {
                return 0;
            }

            // DeleteSelected deactivates the complete assembly before its deferred
            // Destroy. Checking activeSelf makes that inventory available immediately,
            // without refunding pieces just because a parent temporarily hides them.
            spawnedParts.RemoveAll(part => part.Instance == null || !part.Instance.gameObject.activeSelf);
            int limit = partLimits != null && index < partLimits.Length ? partLimits[index] : -1;
            if (limit < 0)
            {
                return -1;
            }

            int used = 0;
            foreach (SpawnedPart part in spawnedParts)
            {
                if (part.Index == index)
                {
                    used++;
                }
            }
            return Mathf.Max(0, limit - used);
        }

        public bool CanAddPart(int index)
        {
            return CanEditParts && GetRemainingCount(index) != 0;
        }

        public DraggableObject AddPart(int index)
        {
            if (!CanAddPart(index))
            {
                return null;
            }

            DraggableObject prefab = prefabs[index];
            Vector2 spawn = index < spawnPositions.Length ? spawnPositions[index] : new Vector2(0, -1.4f);
            var position = new Vector3(spawn.x, prefab.transform.position.y, spawn.y);
            DraggableObject instance = Instantiate(prefab, position, prefab.transform.rotation);
            instance.transform.SetParent(transform, true);
            instance.name = prefab.name;
            // Reserve before selection notifications can invoke another add action.
            var spawned = new SpawnedPart { Index = index, Instance = instance };
            spawnedParts.Add(spawned);
            PhysicsObject[] physicsObjects = instance.GetComponentsInChildren<PhysicsObject>(true);
            if (!gameManager.RegisterResetObjects(physicsObjects) || !placement.SelectObject(instance))
            {
                spawnedParts.Remove(spawned);
                gameManager.UnregisterResetObjects(physicsObjects);
                instance.gameObject.SetActive(false);
                Destroy(instance.gameObject);
                return null;
            }

            PartAdded?.Invoke(index);
            return instance;
        }

        private sealed class SpawnedPart
        {
            public int Index;
            public DraggableObject Instance;
        }
    }
}
