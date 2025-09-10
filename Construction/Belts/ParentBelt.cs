using Engine.Construction.Events;
using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using Engine.Utilities.Events;
using UnityEngine;

namespace Engine.Construction.Belts
{
    /// <summary>
    /// A type of belt that manages a child belt
    /// The child belt should live on a game object that is a child of this game object
    /// </summary>
    public class ParentBelt : Belt
    {
        [Header("Parent Belt")]
        public Belt childBelt;
        protected IWorld World; 
        private Vector2Int _leftGridPosition; 
        [Tooltip("How many steps along the grid should the splitter look for its target Nodes?")]
        public int stepSize = 1;

        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
            SetupResourceMovement();
            
            World = config.World;
            if (!RegisterChildBelt(config)) return;
            FinaliseChildBelt(config);
        }
        
        private bool RegisterChildBelt(NodeConfiguration config)
        {
            if (childBelt == null)
            {
                Debug.LogWarning($"Every {name} requires a child belt. The child belt is currently missing");
                
                childBelt = GetComponentInChildren<Belt>();
                if(childBelt == null) return false;
            }
            
            _leftGridPosition = PositionByDirection.GetLeftPosition(GridCoord, Direction, stepSize);
            Vector3Int childGridCoord = new (_leftGridPosition.x, 0, _leftGridPosition.y);

            if (!config.World.TryPlaceNodeAt(childBelt, _leftGridPosition.x, _leftGridPosition.y))
            {
                childBelt.FailedPlacement(childGridCoord);
                return false;
            }
            
            childBelt.Place(childGridCoord, World);
            return true;
        }

        private void FinaliseChildBelt(NodeConfiguration config)
        {
            NodeConfiguration nodeConfig = GetChildNodeConfiguration(config);
            
            childBelt.SetParent(this);
            childBelt.Initialise(nodeConfig);
            EventBus<NodePlaced>.Raise(new NodePlaced(childBelt));
        }

        protected virtual NodeConfiguration GetChildNodeConfiguration(NodeConfiguration config)
        {
            return NodeConfiguration.Create(World, NodeType.Straight, Direction, true); 
        }
        
        public override void OnRemoval()
        {
            base.OnRemoval();
            
            // Clean up child belt
            childBelt.OnRemoval();
            World.TryRemoveNode(childBelt);
        }
    }
}