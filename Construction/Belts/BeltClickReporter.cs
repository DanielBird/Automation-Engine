using Engine.Utilities;
using UnityEngine;

namespace Engine.Construction.Belts
{
    /// <summary>
    /// An alternative route for having belts detect player clicks
    /// This was built for Splitters, who have two different output belts
    /// And therefore need two different colliders and detectors 
    /// </summary>
    
    public enum LeftOrRight {Left, Right}
    
    public class BeltClickReporter : MonoBehaviour, IClickable
    {
        [field: SerializeField] private Belt myBelt;
        public LeftOrRight leftOrRight;

        private void Awake()
        {
            if (myBelt == null)
            {
                myBelt = GetComponentInParent<Belt>(); 
                Debug.Log("Missing parent node on the belt click reporter on " + name);
            }
        }

        public bool IsEnabled { get; }
        public bool IsSelected { get; }
        public void OnPlayerSelect()
        {
            if (myBelt is Splitter_Alternative splitter)
            {
                splitter.OnPlayerSelect(leftOrRight);
                return;
            }
            
            myBelt.OnPlayerSelect();
        }

        public void OnPlayerDeselect()
        {
            myBelt.OnPlayerDeselect();
        }
    }
}