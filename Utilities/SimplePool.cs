using System.Collections.Generic;
using UnityEngine;

namespace Engine.Utilities
{
    ///   Instead of calling Instantiate(), use this:
    ///       SimplePool.Spawn(somePrefab, somePosition, someRotation);
    /// 
    ///   Instead of destroying an object, use this:
    ///       SimplePool.Despawn(myGameObject);
    /// 
    ///   If desired, you can preload the pool with a number of instances:
    ///       SimplePool.Preload(somePrefab, 20);
    /// 

    public class SimplePool
    {   
        // You can avoid resizing of the Stack's internal array by setting this to a number equal to or greater to what you expect most of your pool sizes to be.
        // Note, you can also use Preload() to set the initial size of a pool -- this can be handy if only some of your pools are going to be exceptionally large (for example, your bullets.)
        const int DEFAULT_POOL_SIZE = 5;
        
        private static void EditorLog(string message, bool logError = false)
        {
#if UNITY_EDITOR
            if(logError) Debug.LogError(message);
            else Debug.LogWarning(message);
#endif
        }
        
        /// The Pool class represents the pool for a particular prefab.
        public class Pool
        {
            // We append an id to the name of anything we instantiate. // This is purely cosmetic.
            private int _nextId = 1;

            // The structure containing our inactive objects.
            // A Stack and not a List because we'll never pluck an object from the start or middle of the array.
            private Stack<GameObject> _inactive;

            //A Hashset which contains all GetInstanceIDs from the instantiated GameObjects so we know which GameObject is a member of this pool.
            public readonly HashSet<int> MemberIDs;
            
            // The prefab that we are pooling
            GameObject prefab;

            public Pool(GameObject prefab, int initialQty)
            {
                this.prefab = prefab;
                _inactive = new Stack<GameObject>(initialQty);
                MemberIDs = new HashSet<int>();
            }
            
            public GameObject Spawn(Vector3 pos, Quaternion rot, Transform t, int defaultPoolKey)
            {
                GameObject obj = null;
                while (_inactive.Count > 0)
                {
                    obj = _inactive.Pop();
                    if (obj != null) break;
                }
    
                if (obj == null)
                {
                    if (prefab == null)
                    {
                        EditorLog("Missing prefab for spawning.", true);
                        return null;
                    }
                    
                    obj = Object.Instantiate(prefab, pos, rot, t);
                    obj.name = $"{prefab.name} ({_nextId++})";
        
                    // Ensure the object has the PoolMember component.
                    PoolMember poolMember = obj.GetComponent<PoolMember>() ?? obj.AddComponent<PoolMember>();
                    poolMember.poolKey = defaultPoolKey;
                    MemberIDs.Add(obj.GetInstanceID());
                }

                // Reset the object state
                obj.transform.position = pos;
                obj.transform.rotation = rot;
                obj.SetActive(true);
                return obj;
            }

            // Return an object to the inactive pool.
            public void ReturnToPool(GameObject obj)
            {
                if (!obj.activeInHierarchy)
                {
                    EditorLog("Trying to despawn a game object that is not currently active in the hierarchy");
                    return;
                }
                
                obj.SetActive(false);
                _inactive.Push(obj);
            }
        }

        /// Added to freshly instantiated objects, so we can link back to the correct pool on despawn.
        public class PoolMember : MonoBehaviour
        {
            public int poolKey = -1;
        }

        // All of our pools
        public static Dictionary<int, Pool> Pools;

        /// If a pool does not already exist for a given game object
        /// Create a new pool and add it to the dictionary of pools.
        private static void Init(GameObject prefab = null, int qty = DEFAULT_POOL_SIZE, int? poolKey = -1)
        {
            if (Pools == null) Pools = new Dictionary<int, Pool>();

            if (prefab != null)
            {
                int key = poolKey >= 0 ? poolKey.Value : prefab.GetInstanceID(); 
                if (!Pools.ContainsKey(key)) Pools[key] = new Pool(prefab, qty);
            }
            else
            {
                EditorLog("Missing game object");
            }
        }

        /// <summary>
        /// If you want to preload a few copies of an object at the start
        /// of a scene, you can use this. Really not needed unless you're
        /// going to go from zero instances to 10+ very quickly.
        /// Could technically be optimized more, but in practice the
        /// Spawn/Despawn sequence is going to be pretty darn quick and
        /// this avoids code duplication.
        /// </summary>
        public static void Preload(GameObject prefab, Transform t, int qty = 1, int? poolKey = -1)
        {
            Init(prefab, qty, poolKey);

            // Make an array to grab the objects we're about to pre-spawn.
            GameObject[] obs = new GameObject[qty];
            for (int i = 0; i < qty; i++)
            {
                obs[i] = Spawn(prefab, Vector3.zero, Quaternion.identity, t, poolKey);
            }

            // Now despawn them all.
            for (int i = 0; i < qty; i++)
            {
                Despawn(obs[i]);
            }
        }

        /// <summary>
        /// Spawns a copy of the specified prefab (instantiating one if required).
        /// NOTE: Remember that Awake() or Start() will only run on the very first
        /// spawn and that member variables won't get reset.  OnEnable will run
        /// after spawning -- but remember that toggling IsActive will also
        /// call that function.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform t, int? poolKey = -1)
        {
            Init(prefab, DEFAULT_POOL_SIZE, poolKey);
            
            int key = poolKey >= 0 ? poolKey.Value : prefab.GetInstanceID(); 
            GameObject go = Pools[key].Spawn(pos, rot, t, key); 
            
            if (go == null)
            {
                EditorLog("Failed to spawn new game object into pool.");
                EditorLog("Existing pool count: " + Pools.Count);
                go = Object.Instantiate(prefab, pos, rot, t);
            }

            return go; 
        }

        /// Despawn the specified game object back into its pool.
        public static void Despawn(GameObject obj)
        {
            PoolMember poolMember = obj.GetComponent<PoolMember>();
            if (poolMember != null)
            {
                int key = poolMember.poolKey;
                if (Pools.ContainsKey(key))
                {
                    Pools[key].ReturnToPool(obj);
                    return;
                }
            }
            
            EditorLog("Object '" + obj.name + "' wasn't spawned from a pool. Destroying it instead.");
            Object.Destroy(obj);
        }
        
        public static bool PoolContainsKey(int key)
        {
            return Pools.ContainsKey(key);
        }
    }
}
