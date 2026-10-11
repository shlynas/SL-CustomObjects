using System;
using DONT_TOUCH.Enums;
using DONT_TOUCH.Scripts.BlockSerialization;
using UnityEngine;

namespace DONT_TOUCH.Scripts.BlockComponents.Locker
{
    [Serializable]
    public class LockerItem
    {
        public LockerItem()
        {
        }

        public LockerItem(SerializableLockerItem serializableLockerItem)
        {
            if (Enum.TryParse(serializableLockerItem.TargetItem, out ItemType itemType))
            {
                TargetItem = itemType;
            }
        
            RemainingUses = serializableLockerItem.RemainingUses;
            ProbabilityPoints = serializableLockerItem.ProbabilityPoints;
            MinPerChamber = serializableLockerItem.MinPerChamber;
            MaxPerChamber = serializableLockerItem.MaxPerChamber;
        }
    
        [Tooltip("The ItemType of this pickup."), SearchableEnum]
        public ItemType TargetItem;

        [Min(0)]
        public int RemainingUses = 1;
    
        [Min(0)]
        public int ProbabilityPoints = 100;

        [Min(0)]
        public int MinPerChamber = 1;
    
        [Min(0)]
        public int MaxPerChamber = 10;
    }
}